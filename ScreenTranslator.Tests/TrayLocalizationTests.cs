using System.IO;
using Xunit;

namespace ScreenTranslator.Tests;

public sealed class TrayLocalizationTests
{
  [Theory]
  [InlineData("Strings.en.xaml")]
  [InlineData("Strings.zh-CN.xaml")]
  public void ResourceFile_ContainsDisableHotkeysTrayMenuString(string fileName)
  {
    var resourcePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ScreenTranslator", "Resources", fileName);
    var content = File.ReadAllText(resourcePath);

    Assert.Contains("x:Key=\"TrayMenu_DisableHotkeys\"", content);
  }
}
