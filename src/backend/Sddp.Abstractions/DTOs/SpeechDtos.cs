namespace Sddp.Abstractions.DTOs;

/// <summary>
/// 음성 전사 결과 DTO
/// </summary>
public class TranscriptionResultDto
{
    /// <summary>
    /// 전사된 텍스트
    /// </summary>
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// STT 기능 상태 DTO
/// </summary>
public class SpeechStatusDto
{
    /// <summary>
    /// STT 기능 활성화 여부 (FeatureFlags:Speech:Enabled)
    /// </summary>
    public bool Enabled { get; set; }
}
