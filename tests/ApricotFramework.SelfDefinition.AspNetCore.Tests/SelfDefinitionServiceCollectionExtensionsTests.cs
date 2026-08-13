using ApricotFramework.SelfDefinition.AspNetCore.Extensions;
using ApricotFramework.SelfDefinition.AspNetCore.Impl;
using ApricotFramework.SelfDefinition.AspNetCore.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApricotFramework.SelfDefinition.AspNetCore.Tests;

public class SelfDefinitionServiceCollectionExtensionsTests
{
    /// <summary>
    /// A provider a consumer might register in place of the built-in one.
    /// </summary>
    private sealed class FixedSelfDefinitionProvider : ISelfDefinitionProvider
    {
        /// <inheritdoc />
        public string GetExternalBaseAddress()
        {
            return "https://replaced.example.com";
        }
    }

    /// <summary>
    /// Builds a configuration from in-memory values.
    /// </summary>
    /// <param name="values">The configuration entries.</param>
    /// <returns>The configuration.</returns>
    private static IConfiguration CreateConfiguration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();
    }

    /// <summary>
    /// Builds a provider with scope validation on, so a lifetime mistake fails the test.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service provider.</returns>
    private static ServiceProvider Build(IServiceCollection services)
    {
        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public void ConfigurationSectionName_IsTheDocumentedSection()
    {
        // The configuration contract every consumer's appsettings.json depends on.
        Assert.Equal("SelfDefinition", SelfDefinitionServiceCollectionExtensions.ConfigurationSectionName);
    }

    [Fact]
    public void AddSelfDefinition_WithConfiguration_ResolvesTheOptionsAwareProvider()
    {
        var services = new ServiceCollection();

        services.AddSelfDefinition(CreateConfiguration(("SelfDefinition:ExternalBaseAddress", "https://api.example.com")));

        using var provider = Build(services);

        Assert.IsType<OptionsAwareSelfDefinitionProvider>(provider.GetRequiredService<ISelfDefinitionProvider>());
    }

    [Fact]
    public void AddSelfDefinition_WithConfiguration_BindsTheExternalBaseAddress()
    {
        var services = new ServiceCollection();

        services.AddSelfDefinition(CreateConfiguration(("SelfDefinition:ExternalBaseAddress", "https://api.example.com/")));

        using var provider = Build(services);

        Assert.Equal("https://api.example.com", provider.GetRequiredService<ISelfDefinitionProvider>().GetExternalBaseAddress());
    }

    [Fact]
    public void AddSelfDefinition_Always_RegistersTheProviderAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddSelfDefinition(CreateConfiguration(("SelfDefinition:ExternalBaseAddress", "https://api.example.com")));

        using var provider = Build(services);

        Assert.Same(
            provider.GetRequiredService<ISelfDefinitionProvider>(),
            provider.GetRequiredService<ISelfDefinitionProvider>());
    }

    [Fact]
    public void AddSelfDefinition_WithCustomSectionName_BindsThatSection()
    {
        var services = new ServiceCollection();

        services.AddSelfDefinition(
            CreateConfiguration(("Service:Self:ExternalBaseAddress", "https://custom.example.com")),
            "Service:Self");

        using var provider = Build(services);

        Assert.Equal("https://custom.example.com", provider.GetRequiredService<ISelfDefinitionProvider>().GetExternalBaseAddress());
    }

    [Fact]
    public void AddSelfDefinition_WithConfigureAction_UsesTheConfiguredAddress()
    {
        var services = new ServiceCollection();

        services.AddSelfDefinition(options => options.ExternalBaseAddress = "https://in-code.example.com/");

        using var provider = Build(services);

        Assert.Equal("https://in-code.example.com", provider.GetRequiredService<ISelfDefinitionProvider>().GetExternalBaseAddress());
    }

    [Fact]
    public void AddSelfDefinition_WithMissingAddress_FailsValidationOnFirstUse()
    {
        var services = new ServiceCollection();

        services.AddSelfDefinition(CreateConfiguration());

        using var provider = Build(services);

        // Under a host, ValidateOnStart turns this into a failure to boot. In a bare provider there
        // is no startup hook, so the same validator runs on first access to the options instead.
        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<ISelfDefinitionProvider>().GetExternalBaseAddress());

        Assert.Contains("is not configured", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddSelfDefinition_WithUnusableAddress_FailsValidationOnFirstUse()
    {
        var services = new ServiceCollection();

        services.AddSelfDefinition(CreateConfiguration(("SelfDefinition:ExternalBaseAddress", "ftp://example.com")));

        using var provider = Build(services);

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<ISelfDefinitionProvider>().GetExternalBaseAddress());

        Assert.Contains("uses the 'ftp' scheme", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddSelfDefinition_CalledTwice_RegistersOneValidatorAndOneProvider()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(("SelfDefinition:ExternalBaseAddress", "https://api.example.com"));

        services.AddSelfDefinition(configuration);
        services.AddSelfDefinition(configuration);

        using var provider = Build(services);

        Assert.Single(provider.GetServices<ISelfDefinitionProvider>());
        Assert.Single(provider.GetServices<IValidateOptions<SelfDefinitionOptions>>());
    }

    [Fact]
    public void AddSelfDefinition_WithProviderRegisteredFirst_KeepsTheConsumerProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ISelfDefinitionProvider, FixedSelfDefinitionProvider>();
        services.AddSelfDefinition(CreateConfiguration(("SelfDefinition:ExternalBaseAddress", "https://api.example.com")));

        using var provider = Build(services);

        Assert.Equal("https://replaced.example.com", provider.GetRequiredService<ISelfDefinitionProvider>().GetExternalBaseAddress());
    }

    [Fact]
    public void AddSelfDefinition_WithProviderRegisteredAfter_KeepsTheConsumerProvider()
    {
        var services = new ServiceCollection();

        services.AddSelfDefinition(CreateConfiguration(("SelfDefinition:ExternalBaseAddress", "https://api.example.com")));
        services.AddSingleton<ISelfDefinitionProvider, FixedSelfDefinitionProvider>();

        using var provider = Build(services);

        Assert.Equal("https://replaced.example.com", provider.GetRequiredService<ISelfDefinitionProvider>().GetExternalBaseAddress());
    }

    [Fact]
    public void AddSelfDefinition_WithNullServices_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => ((IServiceCollection)null!).AddSelfDefinition(CreateConfiguration()));

        Assert.Throws<ArgumentNullException>(
            () => ((IServiceCollection)null!).AddSelfDefinition(options => options.ExternalBaseAddress = "https://api.example.com"));
    }

    [Fact]
    public void AddSelfDefinition_WithNullConfiguration_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddSelfDefinition((IConfiguration)null!));
    }

    [Fact]
    public void AddSelfDefinition_WithNullConfigureAction_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddSelfDefinition((Action<SelfDefinitionOptions>)null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddSelfDefinition_WithBlankSectionName_Throws(string? sectionName)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => new ServiceCollection().AddSelfDefinition(CreateConfiguration(), sectionName!));
    }
}
