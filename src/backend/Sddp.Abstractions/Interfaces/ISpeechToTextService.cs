using Sddp.Abstractions.DTOs;

namespace Sddp.Abstractions.Interfaces;

/// <summary>
/// 음성 전사(Speech-to-Text) 서비스 인터페이스
/// </summary>
public interface ISpeechToTextService
{
    /// <summary>
    /// 오디오 스트림을 텍스트로 전사 (언어 자동 감지)
    /// </summary>
    Task<TranscriptionResultDto> TranscribeAsync(
        Stream audio,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// STT 서버 상태 확인
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
