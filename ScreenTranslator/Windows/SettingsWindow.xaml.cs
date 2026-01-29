using System.Windows;
using System.Windows.Controls;
using ScreenTranslator.Models;
using ScreenTranslator.Services;
using WpfColor = System.Windows.Media.Color;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfColorConverter = System.Windows.Media.ColorConverter;

namespace ScreenTranslator.Windows;

public partial class SettingsWindow : Window
{
  private readonly SettingsService _settings;
  private readonly AutoStartService _autoStart = new();
  private readonly Func<string, string?>? _applyHotkey;
  private readonly Func<string, string?>? _applyPasteHistoryHotkey;
  private bool _clearKeyRequested;
  private bool _clearYoudaoSecretRequested;
  private string? _existingKeyProtected;
  private string? _existingYoudaoSecretProtected;

  private readonly List<LanguageChoice> _fromLanguages =
  [
    new("auto", "自动检测"),
    new("en", "英语"),
    new("zh-Hans", "简体中文"),
    new("zh-Hant", "繁体中文"),
    new("ja", "日语"),
    new("ko", "韩语"),
    new("ru", "俄语"),
    new("th", "泰语"),
    new("vi", "越南语"),
  ];

  private readonly List<LanguageChoice> _toLanguages =
  [
    new("en", "英语"),
    new("zh-Hans", "简体中文"),
    new("zh-Hant", "繁体中文"),
    new("ja", "日语"),
    new("ko", "韩语"),
    new("ru", "俄语"),
    new("th", "泰语"),
    new("vi", "越南语"),
  ];

  private readonly List<ProviderChoice> _providers =
  [
    new("mock", "模拟 (无网络)"),
    new("youdao", "有道翻译"),
    new("deepl", "DeepL"),
    new("azure", "微软翻译"),
    new("google", "谷歌翻译"),
    new("libretranslate", "LibreTranslate (自托管)"),
  ];

  private readonly List<string> _fontFamilies =
  [
    "Segoe UI",
    "Microsoft YaHei",
    "Microsoft YaHei UI",
    "SimSun",
    "SimHei",
    "KaiTi",
    "FangSong",
    "Arial",
    "Consolas",
    "Courier New",
  ];

  public SettingsWindow(
    SettingsService settings,
    Func<string, string?>? applyHotkey = null,
    Func<string, string?>? applyPasteHistoryHotkey = null)
  {
    InitializeComponent();
    _settings = settings;
    _applyHotkey = applyHotkey;
    _applyPasteHistoryHotkey = applyPasteHistoryHotkey;

    InitializeProviderControls();
    InitializeLanguageControls();
    InitializeBubbleControls();
    InitializeEventHandlers();

    LoadFromSettings();
  }

  private void InitializeProviderControls()
  {
    ProviderCombo.ItemsSource = _providers;
    ProviderCombo.DisplayMemberPath = nameof(ProviderChoice.Name);
    ProviderCombo.SelectedValuePath = nameof(ProviderChoice.Id);
    ProviderCombo.SelectionChanged += (_, _) => LoadProviderFields();
  }

  private void InitializeLanguageControls()
  {
    FromLangCombo.ItemsSource = _fromLanguages;
    FromLangCombo.DisplayMemberPath = nameof(LanguageChoice.Name);
    FromLangCombo.SelectedValuePath = nameof(LanguageChoice.Id);

    ToLangCombo.ItemsSource = _toLanguages;
    ToLangCombo.DisplayMemberPath = nameof(LanguageChoice.Name);
    ToLangCombo.SelectedValuePath = nameof(LanguageChoice.Id);
  }

  private void InitializeBubbleControls()
  {
    // Font family combo
    FontFamilyCombo.ItemsSource = _fontFamilies;

    // Slider value changed handlers
    FontSizeSlider.ValueChanged += (_, _) => UpdateBubblePreview();
    CornerRadiusSlider.ValueChanged += (_, _) => UpdateBubblePreview();
    PaddingSlider.ValueChanged += (_, _) => UpdateBubblePreview();
    MaxWidthSlider.ValueChanged += (_, _) => UpdateBubblePreview();

    // Color text changed handlers
    BgColorText.TextChanged += (_, _) => UpdateColorPreview(BgColorText, BgColorPreview);
    TextColorText.TextChanged += (_, _) => UpdateColorPreview(TextColorText, TextColorPreview);
    BorderColorText.TextChanged += (_, _) => UpdateColorPreview(BorderColorText, BorderColorPreview);

    BgColorText.LostFocus += (_, _) => UpdateBubblePreview();
    TextColorText.LostFocus += (_, _) => UpdateBubblePreview();
    BorderColorText.LostFocus += (_, _) => UpdateBubblePreview();
    FontFamilyCombo.SelectionChanged += (_, _) => UpdateBubblePreview();

    // Color preview click to focus the text box
    BgColorPreview.MouseLeftButtonDown += (_, _) => BgColorText.Focus();
    TextColorPreview.MouseLeftButtonDown += (_, _) => TextColorText.Focus();
    BorderColorPreview.MouseLeftButtonDown += (_, _) => BorderColorText.Focus();
  }

  private void InitializeEventHandlers()
  {
    // API Key handlers
    ShowKeyCheck.Checked += (_, _) => SetKeyVisibility(true);
    ShowKeyCheck.Unchecked += (_, _) => SetKeyVisibility(false);
    KeyPassword.PasswordChanged += (_, _) => _clearKeyRequested = false;
    KeyText.TextChanged += (_, _) => _clearKeyRequested = false;
    ClearKeyButton.Click += (_, _) =>
    {
      KeyPassword.Password = string.Empty;
      KeyText.Text = string.Empty;
      _clearKeyRequested = true;
    };

    // Youdao secret handlers
    YoudaoShowSecretCheck.Checked += (_, _) => SetYoudaoSecretVisibility(true);
    YoudaoShowSecretCheck.Unchecked += (_, _) => SetYoudaoSecretVisibility(false);
    YoudaoSecretPassword.PasswordChanged += (_, _) => _clearYoudaoSecretRequested = false;
    YoudaoSecretText.TextChanged += (_, _) => _clearYoudaoSecretRequested = false;
    YoudaoClearSecretButton.Click += (_, _) =>
    {
      YoudaoSecretPassword.Password = string.Empty;
      YoudaoSecretText.Text = string.Empty;
      _clearYoudaoSecretRequested = true;
      YoudaoSecretHint.Visibility = Visibility.Collapsed;
    };

    // Button handlers
    SaveButton.Click += (_, _) => Save();
    ResetBubbleButton.Click += (_, _) => ResetBubbleSettings();
  }

  private void LoadFromSettings()
  {
    // General settings
    AutoStartCheck.IsChecked = _autoStart.IsEnabled();
    HotkeyText.Text = string.IsNullOrWhiteSpace(_settings.Settings.Hotkey) ? "Ctrl+Alt+T" : _settings.Settings.Hotkey;
    PasteHistoryHotkeyText.Text = string.IsNullOrWhiteSpace(_settings.Settings.PasteHistoryHotkey)
      ? "Ctrl+Shift+V"
      : _settings.Settings.PasteHistoryHotkey;
    FromLangCombo.SelectedValue = string.IsNullOrWhiteSpace(_settings.Settings.DefaultFrom) ? "auto" : _settings.Settings.DefaultFrom;
    ToLangCombo.SelectedValue = string.IsNullOrWhiteSpace(_settings.Settings.DefaultTo) ? "zh-Hans" : _settings.Settings.DefaultTo;

    // Provider settings
    ProviderCombo.SelectedValue = _settings.Settings.ActiveProviderId;
    LoadProviderFields();

    // Bubble settings
    LoadBubbleSettings();
  }

  private void LoadBubbleSettings()
  {
    var bubble = _settings.Settings.Bubble ?? new BubbleSettings();

    BgColorText.Text = bubble.BackgroundColor;
    TextColorText.Text = bubble.TextColor;
    BorderColorText.Text = bubble.BorderColor;

    var fontIndex = _fontFamilies.FindIndex(f => f.Equals(bubble.FontFamily, StringComparison.OrdinalIgnoreCase));
    FontFamilyCombo.SelectedIndex = fontIndex >= 0 ? fontIndex : 0;

    FontSizeSlider.Value = bubble.FontSize;
    CornerRadiusSlider.Value = bubble.CornerRadius;
    PaddingSlider.Value = bubble.Padding;
    MaxWidthSlider.Value = bubble.MaxWidthRatio;

    UpdateColorPreview(BgColorText, BgColorPreview);
    UpdateColorPreview(TextColorText, TextColorPreview);
    UpdateColorPreview(BorderColorText, BorderColorPreview);
    UpdateBubblePreview();
  }

  private void UpdateColorPreview(System.Windows.Controls.TextBox textBox, System.Windows.Controls.Border preview)
  {
    try
    {
      var color = (WpfColor)WpfColorConverter.ConvertFromString(textBox.Text);
      preview.Background = new System.Windows.Media.SolidColorBrush(color);
    }
    catch
    {
      preview.Background = WpfBrushes.Transparent;
    }
  }

  private void UpdateBubblePreview()
  {
    // Update labels
    FontSizeLabel.Text = $"{FontSizeSlider.Value:F0} px";
    CornerRadiusLabel.Text = $"{CornerRadiusSlider.Value:F0} px";
    PaddingLabel.Text = $"{PaddingSlider.Value:F0} px";
    MaxWidthLabel.Text = $"{MaxWidthSlider.Value:P0}";

    // Update preview
    try
    {
      var bgColor = (WpfColor)WpfColorConverter.ConvertFromString(BgColorText.Text);
      var textColor = (WpfColor)WpfColorConverter.ConvertFromString(TextColorText.Text);
      var borderColor = (WpfColor)WpfColorConverter.ConvertFromString(BorderColorText.Text);

      BubblePreview.Background = new System.Windows.Media.SolidColorBrush(bgColor);
      BubblePreview.BorderBrush = new System.Windows.Media.SolidColorBrush(borderColor);
      BubblePreview.BorderThickness = new Thickness(1);
      BubblePreview.CornerRadius = new CornerRadius(CornerRadiusSlider.Value);
      BubblePreview.Padding = new Thickness(PaddingSlider.Value);

      BubblePreviewText.Foreground = new System.Windows.Media.SolidColorBrush(textColor);
      BubblePreviewText.FontSize = FontSizeSlider.Value;

      if (FontFamilyCombo.SelectedItem is string fontFamily)
      {
        BubblePreviewText.FontFamily = new WpfFontFamily(fontFamily);
      }
    }
    catch
    {
      // Ignore invalid color values
    }
  }

  private void ResetBubbleSettings()
  {
    var defaults = new BubbleSettings();
    BgColorText.Text = defaults.BackgroundColor;
    TextColorText.Text = defaults.TextColor;
    BorderColorText.Text = defaults.BorderColor;

    var fontIndex = _fontFamilies.FindIndex(f => f.Equals(defaults.FontFamily, StringComparison.OrdinalIgnoreCase));
    FontFamilyCombo.SelectedIndex = fontIndex >= 0 ? fontIndex : 0;

    FontSizeSlider.Value = defaults.FontSize;
    CornerRadiusSlider.Value = defaults.CornerRadius;
    PaddingSlider.Value = defaults.Padding;
    MaxWidthSlider.Value = defaults.MaxWidthRatio;

    UpdateColorPreview(BgColorText, BgColorPreview);
    UpdateColorPreview(TextColorText, TextColorPreview);
    UpdateColorPreview(BorderColorText, BorderColorPreview);
    UpdateBubblePreview();
  }

  private void LoadProviderFields()
  {
    var providerId = (ProviderCombo.SelectedValue as string) ?? _settings.Settings.ActiveProviderId;
    if (!_settings.Settings.Providers.TryGetValue(providerId, out var ps))
      ps = new ProviderSettings();

    EndpointText.Text = ps.Endpoint ?? string.Empty;
    RegionText.Text = ps.Region ?? string.Empty;

    YoudaoAppIdText.Text = ps.AppId ?? string.Empty;

    // Do not auto-fill the key; user can paste/update.
    KeyPassword.Password = string.Empty;
    KeyText.Text = string.Empty;

    YoudaoSecretPassword.Password = string.Empty;
    YoudaoSecretText.Text = string.Empty;

    _existingKeyProtected = ps.KeyProtected;
    _existingYoudaoSecretProtected = ps.AppSecretProtected;
    YoudaoSecretHint.Visibility = string.IsNullOrWhiteSpace(ps.AppSecretProtected) ? Visibility.Collapsed : Visibility.Visible;

    ApplyProviderVisibility(providerId);
  }

  private void Save()
  {
    // Validate hotkey
    var hotkeyValue = (HotkeyText.Text ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(hotkeyValue))
      hotkeyValue = "Ctrl+Alt+T";

    var pasteHotkeyValue = (PasteHistoryHotkeyText.Text ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(pasteHotkeyValue))
      pasteHotkeyValue = "Ctrl+Shift+V";

    if (string.Equals(hotkeyValue, pasteHotkeyValue, StringComparison.OrdinalIgnoreCase))
    {
      System.Windows.MessageBox.Show("快捷键冲突: 截图翻译 与 历史粘贴 不能相同。", "ScreenTranslator", MessageBoxButton.OK, MessageBoxImage.Warning);
      return;
    }

    if (_applyHotkey is not null)
    {
      var error = _applyHotkey(hotkeyValue);
      if (!string.IsNullOrWhiteSpace(error))
      {
        System.Windows.MessageBox.Show($"快捷键无效: {error}", "ScreenTranslator", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
    }

    if (_applyPasteHistoryHotkey is not null)
    {
      var error = _applyPasteHistoryHotkey(pasteHotkeyValue);
      if (!string.IsNullOrWhiteSpace(error))
      {
        System.Windows.MessageBox.Show($"历史粘贴快捷键无效: {error}", "ScreenTranslator", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
    }

    // Save general settings
    _settings.Settings.Hotkey = hotkeyValue;
    _settings.Settings.PasteHistoryHotkey = pasteHotkeyValue;
    _settings.Settings.DefaultFrom = (FromLangCombo.SelectedValue as string) ?? "auto";
    _settings.Settings.DefaultTo = (ToLangCombo.SelectedValue as string) ?? "zh-Hans";

    // Handle auto start
    try
    {
      var exePath = Environment.ProcessPath;
      if (!string.IsNullOrWhiteSpace(exePath))
      {
        if (AutoStartCheck.IsChecked == true && !_autoStart.IsEnabled())
          _autoStart.Enable(exePath);
        else if (AutoStartCheck.IsChecked != true && _autoStart.IsEnabled())
          _autoStart.Disable();
      }
    }
    catch (Exception ex)
    {
      System.Windows.MessageBox.Show($"更新开机启动失败: {ex.Message}", "ScreenTranslator", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    // Save provider settings
    var providerId = (ProviderCombo.SelectedValue as string) ?? "mock";
    _settings.Settings.ActiveProviderId = providerId;

    if (!_settings.Settings.Providers.TryGetValue(providerId, out var ps))
    {
      ps = new ProviderSettings();
      _settings.Settings.Providers[providerId] = ps;
    }

    ps.Endpoint = string.IsNullOrWhiteSpace(EndpointText.Text) ? null : EndpointText.Text.Trim();
    ps.Region = string.IsNullOrWhiteSpace(RegionText.Text) ? null : RegionText.Text.Trim();

    if (string.Equals(providerId, "youdao", StringComparison.OrdinalIgnoreCase))
    {
      ps.AppId = string.IsNullOrWhiteSpace(YoudaoAppIdText.Text) ? null : YoudaoAppIdText.Text.Trim();

      var secretPlain = YoudaoShowSecretCheck.IsChecked == true ? YoudaoSecretText.Text : YoudaoSecretPassword.Password;
      secretPlain = secretPlain?.Trim() ?? string.Empty;
      if (_clearYoudaoSecretRequested)
      {
        ps.AppSecretProtected = null;
      }
      else if (!string.IsNullOrWhiteSpace(secretPlain))
      {
        ps.AppSecretProtected = SecretProtector.ProtectString(secretPlain);
      }
      else if (!string.IsNullOrWhiteSpace(_existingYoudaoSecretProtected))
      {
        ps.AppSecretProtected = _existingYoudaoSecretProtected;
      }

      ps.KeyProtected = null;
    }
    else
    {
      ps.AppId = null;
      ps.AppSecretProtected = null;

      var keyPlain = ShowKeyCheck.IsChecked == true ? KeyText.Text : KeyPassword.Password;
      keyPlain = keyPlain?.Trim() ?? string.Empty;
      if (_clearKeyRequested)
      {
        ps.KeyProtected = null;
      }
      else if (!string.IsNullOrWhiteSpace(keyPlain))
      {
        ps.KeyProtected = SecretProtector.ProtectString(keyPlain);
      }
      else if (!string.IsNullOrWhiteSpace(_existingKeyProtected))
      {
        ps.KeyProtected = _existingKeyProtected;
      }
    }

    // Save bubble settings
    _settings.Settings.Bubble ??= new BubbleSettings();
    _settings.Settings.Bubble.BackgroundColor = BgColorText.Text?.Trim() ?? "#F7F7F5";
    _settings.Settings.Bubble.TextColor = TextColorText.Text?.Trim() ?? "#111111";
    _settings.Settings.Bubble.BorderColor = BorderColorText.Text?.Trim() ?? "#22000000";
    _settings.Settings.Bubble.FontFamily = (FontFamilyCombo.SelectedItem as string) ?? "Segoe UI";
    _settings.Settings.Bubble.FontSize = FontSizeSlider.Value;
    _settings.Settings.Bubble.CornerRadius = CornerRadiusSlider.Value;
    _settings.Settings.Bubble.Padding = PaddingSlider.Value;
    _settings.Settings.Bubble.MaxWidthRatio = MaxWidthSlider.Value;

    _settings.Save();

    _clearKeyRequested = false;
    _clearYoudaoSecretRequested = false;
    _existingKeyProtected = ps.KeyProtected;
    _existingYoudaoSecretProtected = ps.AppSecretProtected;
    YoudaoSecretHint.Visibility = string.IsNullOrWhiteSpace(ps.AppSecretProtected) ? Visibility.Collapsed : Visibility.Visible;

    System.Windows.MessageBox.Show("设置已保存。", "ScreenTranslator", MessageBoxButton.OK, MessageBoxImage.Information);
  }

  private void SetKeyVisibility(bool visible)
  {
    if (visible)
    {
      KeyText.Text = KeyPassword.Password;
      KeyText.Visibility = Visibility.Visible;
      KeyPassword.Visibility = Visibility.Collapsed;
    }
    else
    {
      KeyPassword.Password = KeyText.Text;
      KeyPassword.Visibility = Visibility.Visible;
      KeyText.Visibility = Visibility.Collapsed;
    }
  }

  private void SetYoudaoSecretVisibility(bool visible)
  {
    if (visible)
    {
      YoudaoSecretText.Text = YoudaoSecretPassword.Password;
      YoudaoSecretText.Visibility = Visibility.Visible;
      YoudaoSecretPassword.Visibility = Visibility.Collapsed;
    }
    else
    {
      YoudaoSecretPassword.Password = YoudaoSecretText.Text;
      YoudaoSecretPassword.Visibility = Visibility.Visible;
      YoudaoSecretText.Visibility = Visibility.Collapsed;
    }
  }

  private void ApplyProviderVisibility(string providerId)
  {
    var isYoudao = string.Equals(providerId, "youdao", StringComparison.OrdinalIgnoreCase);

    KeyRow.Visibility = isYoudao ? Visibility.Collapsed : Visibility.Visible;
    YoudaoAppIdRow.Visibility = isYoudao ? Visibility.Visible : Visibility.Collapsed;
    YoudaoSecretRow.Visibility = isYoudao ? Visibility.Visible : Visibility.Collapsed;
  }

  private sealed record ProviderChoice(string Id, string Name);
  private sealed record LanguageChoice(string Id, string Name);
}
