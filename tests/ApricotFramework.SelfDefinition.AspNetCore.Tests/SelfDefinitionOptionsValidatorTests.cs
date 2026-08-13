using ApricotFramework.SelfDefinition.AspNetCore.Options;
using ApricotFramework.SelfDefinition.AspNetCore.Validation;

namespace ApricotFramework.SelfDefinition.AspNetCore.Tests;

public class SelfDefinitionOptionsValidatorTests
{
    /// <summary>
    /// Validates a candidate external base address.
    /// </summary>
    /// <param name="externalBaseAddress">The address to validate.</param>
    /// <returns>The validation result.</returns>
    private static Microsoft.Extensions.Options.ValidateOptionsResult Validate(string? externalBaseAddress)
    {
        return new SelfDefinitionOptionsValidator().Validate(
            name: null,
            new SelfDefinitionOptions { ExternalBaseAddress = externalBaseAddress });
    }

    [Theory]
    [InlineData("https://api.example.com")]
    [InlineData("http://api.example.com")]
    [InlineData("https://example.com/api")]
    [InlineData("https://example.com/api/")]
    [InlineData("http://localhost:5000")]
    [InlineData("HTTPS://EXAMPLE.COM/Api")]
    [InlineData(" https://api.example.com ")]
    [InlineData("https://xn--e1afmkfd.xn--p1ai/")]
    [InlineData("https://пример.рф/путь")]
    public void Validate_WithUsableAddress_Succeeds(string address)
    {
        Assert.True(Validate(address).Succeeded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNoAddress_FailsAskingForOne(string? address)
    {
        var result = Validate(address);

        Assert.True(result.Failed);
        Assert.Contains("is not configured", result.FailureMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData("https://")]
    [InlineData("https:///")]
    [InlineData("https://example.com /api")]
    [InlineData("https://example.com\\api")]
    public void Validate_WithNonAbsoluteAddress_FailsAsNotAbsolute(string address)
    {
        var result = Validate(address);

        Assert.True(result.Failed);
        Assert.Contains("is not an absolute URL", result.FailureMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ftp://example.com", "ftp")]
    [InlineData("file:///etc/passwd", "file")]
    [InlineData("mailto:someone@example.com", "mailto")]
    // A bare path and a scheme-relative address both parse as absolute file URIs, so the scheme
    // check is the only thing standing between them and a service that builds file:// links.
    [InlineData("/api", "file")]
    [InlineData("//example.com", "file")]
    public void Validate_WithUnsupportedScheme_FailsNamingTheScheme(string address, string scheme)
    {
        var result = Validate(address);

        Assert.True(result.Failed);
        Assert.Contains($"uses the '{scheme}' scheme", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WithEmbeddedCredentials_FailsAsCredentialLeak()
    {
        var result = Validate("https://user:pass@example.com");

        Assert.True(result.Failed);
        Assert.Contains("embeds credentials", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WithQueryString_FailsAsUnusablePrefix()
    {
        var result = Validate("https://example.com/api?tenant=acme");

        Assert.True(result.Failed);
        Assert.Contains("carries a query string", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WithFragment_FailsAsUnusablePrefix()
    {
        var result = Validate("https://example.com/api#section");

        Assert.True(result.Failed);
        Assert.Contains("carries a fragment", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WithFailure_QuotesTheOffendingValue()
    {
        // An operator reading a crash log needs to see what was actually configured.
        Assert.Contains("'ftp://example.com'", Validate("ftp://example.com").FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WithNullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SelfDefinitionOptionsValidator().Validate(name: null, options: null!));
    }

    [Fact]
    public void Validate_WithVeryLongAddress_CompletesAndSucceeds()
    {
        // A persisted or configured value driving unbounded work is a recurring hazard; Uri parsing
        // is linear here, so a 64 KB address is merely long, not a stall.
        Assert.True(Validate("https://example.com/" + new string('a', 64 * 1024)).Succeeded);
    }

    [Fact]
    public void Validate_WithEmbeddedNewline_FailsAsNotAbsolute()
    {
        Assert.True(Validate("https://exa\nmple.com").Failed);
    }
}
