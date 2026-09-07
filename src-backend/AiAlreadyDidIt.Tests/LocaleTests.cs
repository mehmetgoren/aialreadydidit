using AiAlreadyDidIt.Api.Services.Account;

namespace AiAlreadyDidIt.Tests;

public class LocaleTests
{
    [Theory]
    [InlineData("en-US", "en-US")]
    [InlineData("tr-TR", "tr-TR")]
    [InlineData("tr", "tr-TR")]
    [InlineData("TR-tr", "tr-TR")]
    [InlineData("pt-PT", "pt-BR")]
    [InlineData("pt", "pt-BR")]
    [InlineData("zh-TW", "zh-CN")]
    [InlineData("fr_CA", "fr-FR")]
    [InlineData("ar", "ar-SA")]
    [InlineData("ja-JP", "ja-JP")]
    [InlineData("ko", "ko-KR")]
    [InlineData("de-AT", "de-DE")]
    [InlineData("es-MX", "es-ES")]
    [InlineData("ru", "ru-RU")]
    [InlineData("nl-NL", "en-US")]
    [InlineData("", "en-US")]
    [InlineData(null, "en-US")]
    public void NormalizeLocale_maps_browser_tags_to_supported_ui_languages(string? input, string expected) =>
        Assert.Equal(expected, AccountService.NormalizeLocale(input));

    [Fact]
    public void Supported_locales_are_unique_bcp47_tags()
    {
        Assert.Equal(11, AccountService.SupportedLocales.Length);
        Assert.Equal(AccountService.SupportedLocales.Length, AccountService.SupportedLocales.Distinct().Count());
        Assert.All(AccountService.SupportedLocales, l => Assert.Matches("^[a-z]{2}-[A-Z]{2}$", l));
    }
}
