using ScreenTranslator.Services;
using ScreenTranslator.Translation;
using ScreenTranslator.Models;
using Xunit;

namespace ScreenTranslator.Tests;

public sealed class TranslationServiceTests
{
  [Fact]
  public async Task TranslateAsync_RetriesWithSplitCompoundWord_WhenProviderReturnsOriginalText()
  {
    var settings = new SettingsService();
    var provider = new StubTranslationProvider(new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["ScreenTranslator"] = "ScreenTranslator",
      ["Screen Translator"] = "屏幕翻译器",
    });
    var service = new TranslationService(settings, () => provider);

    var translated = await service.TranslateAsync("ScreenTranslator", "auto", "zh-Hans", CancellationToken.None);

    Assert.Equal("屏幕翻译器", translated);
  }

  [Fact]
  public async Task TranslateAsync_DoesNotRetry_WhenProviderAlreadyTranslatedText()
  {
    var settings = new SettingsService();
    var provider = new StubTranslationProvider(new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["screenshot"] = "截图",
    });
    var service = new TranslationService(settings, () => provider);

    var translated = await service.TranslateAsync("screenshot", "auto", "zh-Hans", CancellationToken.None);

    Assert.Equal("截图", translated);
    Assert.Equal(new[] { "screenshot" }, provider.Requests);
  }

  [Fact]
  public async Task TranslateAsync_PreservesOriginalLineBreaks_WhenTextIsMultiline()
  {
    var settings = new SettingsService();
    var provider = new StubTranslationProvider(new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["first line"] = "第一行",
      ["second line"] = "第二行",
    });
    var service = new TranslationService(settings, () => provider);

    var translated = await service.TranslateAsync("first line\r\n\r\nsecond line", "auto", "zh-Hans", CancellationToken.None);

    Assert.Equal("第一行\r\n\r\n第二行", translated);
    Assert.Equal(new[] { "first line", "second line" }, provider.Requests);
  }

  [Fact]
  public async Task TranslateAsync_PreservesCommandLines_AndTranslatesProse()
  {
    var settings = new SettingsService();
    var provider = new StubTranslationProvider(new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["Build and run"] = "构建并运行",
      ["Requires Windows 11 and .NET 8 SDK."] = "需要 Windows 11 和 .NET 8 SDK。",
    });
    var service = new TranslationService(settings, () => provider);

    var source = "Build and run\r\n\r\nRequires Windows 11 and .NET 8 SDK.\r\n\r\ndotnet run --project .\\ScreenTranslator\\ScreenTranslator.csproj";
    var translated = await service.TranslateAsync(source, "auto", "zh-Hans", CancellationToken.None);

    Assert.Equal("构建并运行\r\n\r\n需要 Windows 11 和 .NET 8 SDK。\r\n\r\ndotnet run --project .\\ScreenTranslator\\ScreenTranslator.csproj", translated);
    Assert.Equal(new[] { "Build and run", "Requires Windows 11 and .NET 8 SDK." }, provider.Requests);
  }

  [Fact]
  public async Task TranslateAsync_TranslatesProseContainingIoTerm()
  {
    var settings = new SettingsService();
    var provider = new StubTranslationProvider(new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["We could use TRNSTOOLSKEEP0TOKEN, HTTP, and database tools."] = "我们可以使用 TRNSTOOLSKEEP0TOKEN、HTTP 和数据库工具。",
    });
    var service = new TranslationService(settings, () => provider);

    var translated = await service.TranslateAsync("We could use I/O, HTTP, and database tools.", "auto", "zh-Hans", CancellationToken.None);

    Assert.Equal("我们可以使用 I/O、HTTP 和数据库工具。", translated);
    Assert.Equal(new[] { "We could use TRNSTOOLSKEEP0TOKEN, HTTP, and database tools." }, provider.Requests);
  }

  [Fact]
  public async Task TranslateAsync_RestoresIoTerm_WhenProviderReturnsProtectedPlaceholder()
  {
    var settings = new SettingsService();
    var provider = new StubTranslationProvider(new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["TRNSTOOLSKEEP0TOKEN keeps the shell useful."] = "TRNSTOOLSKEEP0TOKEN 让 shell 保持有用。",
    });
    var service = new TranslationService(settings, () => provider);

    var translated = await service.TranslateAsync("I/O keeps the shell useful.", "auto", "zh-Hans", CancellationToken.None);

    Assert.Equal("I/O 让 shell 保持有用。", translated);
  }

  [Fact]
  public async Task TranslateAsync_TranslatesProseContainingDoubleDash()
  {
    var settings = new SettingsService();
    var provider = new StubTranslationProvider(new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["is the most primitive form of agent delegation -- no shared memory,"] = "是最原始的 agent 委托形式 -- 没有共享内存，",
    });
    var service = new TranslationService(settings, () => provider);

    var translated = await service.TranslateAsync("is the most primitive form of agent delegation -- no shared memory,", "auto", "zh-Hans", CancellationToken.None);

    Assert.Equal("是最原始的 agent 委托形式 -- 没有共享内存，", translated);
    Assert.Equal(new[] { "is the most primitive form of agent delegation -- no shared memory," }, provider.Requests);
  }

  [Fact]
  public async Task TranslateAsync_PreservesCommandLineOptions()
  {
    var settings = new SettingsService();
    var provider = new StubTranslationProvider(new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["dotnet run --project .\\ScreenTranslator\\ScreenTranslator.csproj"] = "translated command",
    });
    var service = new TranslationService(settings, () => provider);

    var translated = await service.TranslateAsync("dotnet run --project .\\ScreenTranslator\\ScreenTranslator.csproj", "auto", "zh-Hans", CancellationToken.None);

    Assert.Equal("dotnet run --project .\\ScreenTranslator\\ScreenTranslator.csproj", translated);
    Assert.Empty(provider.Requests);
  }

  [Fact]
  public void CreateProvider_CarriesYoudaoDomainSettings_IntoProvider()
  {
    var settings = new SettingsService();
    settings.Settings.ActiveProviderId = "youdao";
    settings.Settings.Providers["youdao"] = new ProviderSettings
    {
      AppId = "app-id",
      Domain = "computers",
      RejectFallback = true,
    };
    var service = new TranslationService(settings);

    var provider = Assert.IsType<YoudaoTranslationProvider>(service.CreateProvider());
    var domainField = typeof(YoudaoTranslationProvider).GetField("_domain", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    var rejectFallbackField = typeof(YoudaoTranslationProvider).GetField("_rejectFallback", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

    Assert.NotNull(domainField);
    Assert.NotNull(rejectFallbackField);
    Assert.Equal("computers", domainField!.GetValue(provider));
    Assert.Equal(true, rejectFallbackField!.GetValue(provider));
  }

  private sealed class StubTranslationProvider : ITranslationProvider
  {
    private readonly Dictionary<string, string> _responses;

    public StubTranslationProvider(Dictionary<string, string> responses)
    {
      _responses = responses;
    }

    public string Id => "stub";
    public string DisplayName => "Stub";
    public List<string> Requests { get; } = [];

    public Task<string> TranslateAsync(string text, string from, string to, CancellationToken ct)
    {
      ct.ThrowIfCancellationRequested();
      Requests.Add(text);
      return Task.FromResult(_responses.TryGetValue(text, out var result) ? result : text);
    }
  }
}
