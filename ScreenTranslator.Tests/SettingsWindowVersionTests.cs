using ScreenTranslator.Windows;
using Xunit;

namespace ScreenTranslator.Tests;

public sealed class SettingsWindowVersionTests
{
  [Theory]
  [InlineData("1.0.0", "1.0.0")]
  [InlineData("1.0.0.0", "1.0.0")]
  [InlineData("1.0.0+abc123", "1.0.0")]
  [InlineData("1.0.0-beta.1", "1.0.0")]
  [InlineData(null, "1.0.0")]
  [InlineData("", "1.0.0")]
  public void NormalizeDisplayVersion_ReturnsMajorMinorPatch(string? rawVersion, string expected)
  {
    var actual = SettingsWindow.NormalizeDisplayVersion(rawVersion);

    Assert.Equal(expected, actual);
  }
}
