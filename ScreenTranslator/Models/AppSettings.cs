namespace ScreenTranslator.Models;

public sealed class AppSettings
{
  public string ActiveProviderId { get; set; } = "mock";
  public string DefaultFrom { get; set; } = "en";
  public string DefaultTo { get; set; } = "zh-Hans";
  public string Hotkey { get; set; } = "Ctrl+Alt+T";
  public string PasteHistoryHotkey { get; set; } = "Ctrl+Shift+V";
  public bool AutoStart { get; set; } = false;

  public Dictionary<string, ProviderSettings> Providers { get; set; } = new();

  // Bubble appearance settings
  public BubbleSettings Bubble { get; set; } = new();
}

public sealed class ProviderSettings
{
  // DPAPI-protected secret (base64) for the provider.
  public string? KeyProtected { get; set; }

  // Some providers (e.g. Youdao) use an app id/key + app secret.
  public string? AppId { get; set; }
  public string? AppSecretProtected { get; set; }

  public string? Endpoint { get; set; }
  public string? Region { get; set; }
}

public sealed class BubbleSettings
{
  public string BackgroundColor { get; set; } = "#F7F7F5";
  public string TextColor { get; set; } = "#111111";
  public string BorderColor { get; set; } = "#22000000";
  public string FontFamily { get; set; } = "Segoe UI";
  public double FontSize { get; set; } = 14;
  public double CornerRadius { get; set; } = 8;
  public double Padding { get; set; } = 12;
  public double MaxWidthRatio { get; set; } = 0.45;
}
