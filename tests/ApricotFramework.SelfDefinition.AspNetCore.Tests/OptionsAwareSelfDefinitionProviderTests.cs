using ApricotFramework.SelfDefinition.AspNetCore.Impl;
using ApricotFramework.SelfDefinition.AspNetCore.Options;
using Microsoft.Extensions.Options;

namespace ApricotFramework.SelfDefinition.AspNetCore.Tests;

public class OptionsAwareSelfDefinitionProviderTests
{
    /// <summary>
    /// An options monitor whose current value can be swapped, standing in for a configuration
    /// reload.
    /// </summary>
    private sealed class MutableOptionsMonitor : IOptionsMonitor<SelfDefinitionOptions>
    {
        /// <summary>
        /// Creates a monitor over an initial address.
        /// </summary>
        /// <param name="externalBaseAddress">The initial address.</param>
        public MutableOptionsMonitor(string? externalBaseAddress)
        {
            this.CurrentValue = new SelfDefinitionOptions { ExternalBaseAddress = externalBaseAddress };
        }

        /// <inheritdoc />
        public SelfDefinitionOptions CurrentValue { get; set; }

        /// <inheritdoc />
        public SelfDefinitionOptions Get(string? name)
        {
            return this.CurrentValue;
        }

        /// <inheritdoc />
        public IDisposable OnChange(Action<SelfDefinitionOptions, string?> listener)
        {
            return new NoopSubscription();
        }

        /// <summary>
        /// A change subscription that does nothing, since no test listens for changes.
        /// </summary>
        private sealed class NoopSubscription : IDisposable
        {
            /// <inheritdoc />
            public void Dispose()
            {
                // Nothing to release.
            }
        }
    }

    /// <summary>
    /// Creates a provider over a fixed address.
    /// </summary>
    /// <param name="externalBaseAddress">The configured address.</param>
    /// <returns>The provider.</returns>
    private static OptionsAwareSelfDefinitionProvider CreateProvider(string? externalBaseAddress)
    {
        return new OptionsAwareSelfDefinitionProvider(new MutableOptionsMonitor(externalBaseAddress));
    }

    [Theory]
    [InlineData("https://api.example.com", "https://api.example.com")]
    [InlineData("https://api.example.com/", "https://api.example.com")]
    [InlineData("https://api.example.com///", "https://api.example.com")]
    [InlineData("https://example.com/api", "https://example.com/api")]
    [InlineData("https://example.com/api/", "https://example.com/api")]
    [InlineData("http://localhost:5000/", "http://localhost:5000")]
    [InlineData("  https://api.example.com/  ", "https://api.example.com")]
    public void GetExternalBaseAddress_WithConfiguredAddress_TrimsWhitespaceAndTrailingSlashes(string configured, string expected)
    {
        Assert.Equal(expected, CreateProvider(configured).GetExternalBaseAddress());
    }

    [Fact]
    public void GetExternalBaseAddress_WithPathBase_KeepsThePathBase()
    {
        // Losing the path base is the classic way a service behind a gateway builds broken links.
        Assert.Equal("https://example.com/tenant/api", CreateProvider("https://example.com/tenant/api/").GetExternalBaseAddress());
    }

    [Fact]
    public void GetExternalBaseAddress_WithMixedCaseAddress_ReturnsItAsConfigured()
    {
        // Not lower-cased: what an operator wrote is what links contain.
        Assert.Equal("HTTPS://EXAMPLE.COM/Api", CreateProvider("HTTPS://EXAMPLE.COM/Api").GetExternalBaseAddress());
    }

    [Fact]
    public void GetExternalBaseAddress_WithInternationalHost_ReturnsItUnpunycoded()
    {
        Assert.Equal("https://пример.рф", CreateProvider("https://пример.рф/").GetExternalBaseAddress());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/")]
    [InlineData("///")]
    public void GetExternalBaseAddress_WithNothingUsableConfigured_Throws(string? configured)
    {
        // The backstop for a provider built by hand around unvalidated settings. Registered through
        // AddSelfDefinition, the options validator rejects all of these first.
        var exception = Assert.Throws<InvalidOperationException>(() => CreateProvider(configured).GetExternalBaseAddress());

        Assert.Contains(nameof(SelfDefinitionOptions.ExternalBaseAddress), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetExternalBaseAddress_AfterOptionsChange_ReflectsTheNewAddress()
    {
        var monitor = new MutableOptionsMonitor("https://old.example.com");
        var provider = new OptionsAwareSelfDefinitionProvider(monitor);

        Assert.Equal("https://old.example.com", provider.GetExternalBaseAddress());

        monitor.CurrentValue = new SelfDefinitionOptions { ExternalBaseAddress = "https://new.example.com/" };

        Assert.Equal("https://new.example.com", provider.GetExternalBaseAddress());
    }

    [Fact]
    public void Constructor_WithNullMonitor_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new OptionsAwareSelfDefinitionProvider(null!));
    }

    [Fact]
    public void GetExternalBaseAddress_WhenComposedWithARelativePath_ProducesASingleSeparator()
    {
        // The contract the trimming exists to serve. Every documented usage looks like this.
        var address = CreateProvider("https://example.com/api/").GetExternalBaseAddress();

        Assert.Equal("https://example.com/api/things/42", $"{address}/things/42");
    }
}
