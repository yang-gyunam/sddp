using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;
using Sddp.Api.Authorization;

namespace Sddp.Api.Controllers;

/// <summary>
/// 음성 전사(Speech-to-Text) 컨트롤러
/// </summary>
[Route("api/speech")]
[Authorize]
public class SpeechController : BaseApiController
{
    private const long MaxAudioBytes = 10 * 1024 * 1024; // 10MB (60초 webm/opus ≈ ~1MB)

    private static readonly string[] AllowedContentTypes =
    [
        "audio/webm",
        "audio/ogg",
        "audio/mp4",
        "audio/mpeg",
        "audio/wav",
    ];

    private readonly ISpeechToTextService _speechToText;
    private readonly ILogger<SpeechController> _logger;
    private readonly bool _isSpeechEnabled;

    public SpeechController(
        ISpeechToTextService speechToText,
        IConfiguration configuration,
        ILogger<SpeechController> logger)
    {
        _speechToText = speechToText;
        _logger = logger;
        _isSpeechEnabled = configuration.GetValue("FeatureFlags:Speech:Enabled", false);
    }

    /// <summary>
    /// STT 기능 활성화 상태 조회
    /// </summary>
    [HttpGet("status")]
        [ResourceScopeExempt("Feature flag probe that reads build configuration only and touches no tenant or project owned data.")]
    public IActionResult GetStatus()
    {
        return Ok(ApiResponse<SpeechStatusDto>.Ok(new SpeechStatusDto
        {
            Enabled = _isSpeechEnabled,
        }));
    }

    /// <summary>
    /// 음성 전사 (multipart/form-data, 필드명 audio, 최대 10MB)
    /// </summary>
    [HttpPost("transcriptions")]
        [ResourceScopeExempt("Stateless transcription of an uploaded audio stream; nothing is persisted and no resource id appears in the route.")]
    [MutationAuthorizationExempt("Speech transcription does not persist application data.", allowsAutomatedCaller: true)]
    [RequestSizeLimit(MaxAudioBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxAudioBytes)]
    public async Task<IActionResult> Transcribe(
        IFormFile? audio,
        CancellationToken cancellationToken)
    {
        if (!_isSpeechEnabled)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.Fail("SPEECH_DISABLED", "Speech-to-text is not enabled."));
        }

        if (audio is null || audio.Length == 0)
        {
            return BadRequest(
                ApiResponse<object>.Fail("SPEECH_FILE_REQUIRED", "Audio file is required."));
        }

        // 브라우저는 "audio/webm;codecs=opus" 형태로 전송하므로 codecs 접미사를 제거하고 비교
        var baseContentType = audio.ContentType?.Split(';')[0].Trim().ToLowerInvariant();
        if (baseContentType is null || !AllowedContentTypes.Contains(baseContentType))
        {
            return BadRequest(ApiResponse<object>.Fail(
                "SPEECH_UNSUPPORTED_FORMAT",
                $"Unsupported audio format: {audio.ContentType}"));
        }

        try
        {
            await using var stream = audio.OpenReadStream();
            var result = await _speechToText.TranscribeAsync(
                stream,
                audio.FileName,
                baseContentType,
                cancellationToken);
            return Ok(ApiResponse<TranscriptionResultDto>.Ok(result));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Speech transcription failed");
            return StatusCode(
                StatusCodes.Status502BadGateway,
                ApiResponse<object>.Fail(
                    "SPEECH_TRANSCRIPTION_FAILED",
                    "Transcription service is unavailable."));
        }
    }
}
