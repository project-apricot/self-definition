using ApricotFramework.SelfDefinition.AspNetCore.Extensions;
using ApricotFramework.SelfDefinition.AspNetCore.Impl;
using ApricotFramework.SelfDefinition.AspNetCore.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApricotFramework.SelfDefinition.AspNetCore.Tests;

/// <summary>
/// Compiles and exercises the extension points the documentation tells consumers to use. If the
/// public shape of the library drifts, this fails to build — which is the point.
/// </summary>
public class DocumentedExtensionPointTests
{
    /// <summary>
    /// The subclassing sample from the ASP.NET Core documentation page: source the settings from
    /// somewhere else, keep the parsing and trimming.
    /// </summary>
    private sealed class TenantSelfDefinitionProvider : OptionsAwareSelfDefinitionProvider
    {
        /// <summary>
        /// Creates a new provider.
        /// </summary>
        /// <param name="optionsMonitor">The monitor supplying the configured settings.</param>
        public TenantSelfDefinitionProvider(IOptionsMonitor<SelfDefinitionOptions> optionsMonitor)
            : base(optionsMonitor)
        {
        }

        /// <inheritdoc />
        protected override SelfDefinitionOptions GetCurrentOptions()
        {
            return new SelfDefinitionOptions { ExternalBaseAddress = "https://tenant.example.com/" };
        }
    }

    [Fact]
    public void OverridingGetCurrentOptions_ReplacesTheSourceButKeepsTheTrimming()
    {
        var services = new ServiceCollection();

        services.AddSelfDefinition(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SelfDefinition:ExternalBaseAddress"] = "https://ignored.example.com",
                })
                .Build());

        services.AddSingleton<ISelfDefinitionProvider, TenantSelfDefinitionProvider>();

        using var serviceProvider = services.BuildServiceProvider(validateScopes: true);

        // The override supplies the address, and the base class still trims the trailing slash.
        Assert.Equal(
            "https://tenant.example.com",
            serviceProvider.GetRequiredService<ISelfDefinitionProvider>().GetExternalBaseAddress());
    }

    [Fact]
    public void ImplementingTheInterfaceDirectly_NeedsOnlyOneMember()
    {
        // The whole implementer contract. If this ever needs a second member, that is a breaking
        // change for everyone who wrote their own provider.
        Assert.Single(typeof(ISelfDefinitionProvider).GetMembers());
    }
}
