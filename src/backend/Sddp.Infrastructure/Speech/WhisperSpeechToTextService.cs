using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Interfaces;

namespace Sddp.Infrastructure.Speech;

/// <summary>
/// Whisper(OpenAI 호환 /v1/audio/transcriptions) 기반 음성 전사 서비스
/// </summary>
public class WhisperSpeechToTextService : ISpeechToTextService
{
    private const string TranscriptionsPath = "/v1/audio/transcriptions";
    private const string HealthPath = "/health";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _httpClient;
    private readonly SpeechToTextSettings _settings;
    private readonly ILogger<WhisperSpeechToTextService> _logger;

    public WhisperSpeechToTextService(
        HttpClient httpClient,
        IOptions<SpeechToTextSettings> settings,
        ILogger<WhisperSpeechToTextService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<TranscriptionResultDto> TranscribeAsync(
        Stream audio,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(audio);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(_settings.Model), "model");
        form.Add(new StringContent("json"), "response_format");
        if (!string.IsNullOrWhiteSpace(_settings.Language))
        {
            form.Add(new StringContent(_settings.Language), "language");
        }

        using var response = await (_httpClient.PostAsync(TranscriptionsPath, form, cancellationToken))
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await (response.Content.ReadAsStringAsync(cancellationToken)).ConfigureAwait(false);
            _logger.LogError(
                "Whisper transcription failed: {StatusCode} {Body}",
                (int)response.StatusCode,
                body);
            throw new HttpRequestException(
                $"Whisper transcription failed with status {(int)response.StatusCode}");
        }

        var payload = await (response.Content
            .ReadFromJsonAsync<WhisperTranscriptionResponse>(JsonOptions, cancellationToken))
            .ConfigureAwait(false);

        return new TranscriptionResultDto { Text = (payload?.Text ?? string.Empty).Trim() };
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await (_httpClient.GetAsync(HealthPath, cancellationToken))
                .ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Whisper health check failed");
            return false;
        }
    }

    private sealed record WhisperTranscriptionResponse(
        [property: JsonPropertyName("text")] string? Text);
}
