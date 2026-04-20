namespace ScreenTranslator.Services;

public static class GifRecordingDefaults
{
  public const int FrameIntervalMs = 125;
  public const int MinimumDistinctFrameDelayMs = 250;
  public const int MaxDurationSeconds = 30;
  public const int MaxCaptureAttempts = (MaxDurationSeconds * 1000) / FrameIntervalMs;
  public const int MaxConsecutiveCaptureFailures = 3;
}
