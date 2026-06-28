namespace Sddp.Infrastructure.Speech;

/// <summary>
/// 음성 전사(STT) 서비스 설정
/// </summary>
public class SpeechToTextSettings
{
    public const string SectionName = "SpeechToText";

    /// <summary>
    /// STT 서버 URL (OpenAI 호환, 기본값: http://localhost:9020)
    /// </summary>
    public string Url { get; set; } = "http://localhost:9020";

    /// <summary>
    /// 전사 모델 (기본값: Systran/faster-whisper-small)
    /// </summary>
    public string Model { get; set; } = "Systran/faster-whisper-small";

    /// <summary>
    /// 전사 언어 (ISO 639-1). 빈 값이면 자동 감지
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// 요청 타임아웃 (초)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;
}
