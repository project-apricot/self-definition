using ApricotFramework.SelfDefinition.AspNetCore.Extensions;
using ApricotFramework.SelfDefinition.AspNetCore.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApricotFramework.SelfDefinition.AspNetCore.Tests;

/// <summary>
/// Pins the reload behaviour the documentation promises: the address is re-read, and re-validated,
/// without restarting the host.
/// </summary>
public class SelfDefinitionReloadTests
{
    [Fact]
    public void GetExternalBaseAddress_AfterConfigurationReload_ReturnsTheNewAddress()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SelfDefinition:ExternalBaseAddress"] = "https://first.example.com/",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSelfDefinition(configuration);

        using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
        var selfDefinition = serviceProvider.GetRequiredService<ISelfDefinitionProvider>();

        Assert.Equal("https://first.example.com", selfDefinition.GetExternalBaseAddress());

        configuration["SelfDefinition:ExternalBaseAddress"] = "https://second.example.com/";
        configuration.Reload();

        Assert.Equal("https://second.example.com", selfDefinition.GetExternalBaseAddress());
    }

    [Fact]
    public void GetExternalBaseAddress_AfterReloadingABrokenAddress_ThrowsAndThenRecovers()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SelfDefinition:ExternalBaseAddress"] = "https://first.example.com/",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSelfDefinition(configuration);

        using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
        var selfDefinition = serviceProvider.GetRequiredService<ISelfDefinitionProvider>();

        Assert.Equal("https://first.example.com", selfDefinition.GetExternalBaseAddress());

        // A reload is not a second chance to skip validation: a broken edit is refused rather than
        // silently adopted, so links are never built from a value that failed the same checks the
        // host applied at startup.
        //
        // Where the refusal surfaces depends on which thread fires the change token, so this asserts
        // that it happens rather than exactly where. OptionsMonitor re-creates the options eagerly in
        // its change callback: an explicit Reload() runs that inline and the validation failure comes
        // straight back out of Reload(), while a file-watcher reload runs it on a background thread
        // and the failure lands on the next call instead.
        var failure = Record.Exception(() =>
        {
            configuration["SelfDefinition:ExternalBaseAddress"] = "ftp://broken.example.com";
            configuration.Reload();

            selfDefinition.GetExternalBaseAddress();
        });

        Assert.NotNull(failure);
        Assert.Contains("uses the 'ftp' scheme", failure.ToString(), StringComparison.Ordinal);

        // And repairing the configuration recovers without a restart.
        configuration["SelfDefinition:ExternalBaseAddress"] = "https://repaired.example.com/";
        configuration.Reload();

        Assert.Equal("https://repaired.example.com", selfDefinition.GetExternalBaseAddress());
    }

    [Fact]
    public void GetExternalBaseAddress_AfterReload_StillComesFromTheSameSingleton()
    {
        // The reload story only holds because the provider re-reads the monitor per call rather than
        // capturing the address once in its constructor.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SelfDefinition:ExternalBaseAddress"] = "https://first.example.com",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSelfDefinition(configuration);

        using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
        var before = serviceProvider.GetRequiredService<ISelfDefinitionProvider>();

        configuration["SelfDefinition:ExternalBaseAddress"] = "https://second.example.com";
        configuration.Reload();

        var after = serviceProvider.GetRequiredService<ISelfDefinitionProvider>();

        Assert.Same(before, after);
        Assert.Equal("https://second.example.com", after.GetExternalBaseAddress());
    }

    [Fact]
    public void SelfDefinitionOptions_HasOnlyTheExternalBaseAddress()
    {
        // Guards the frozen configuration schema: a property added here is a public API change and a
        // new binding contract, so it should be a deliberate decision rather than a drive-by edit.
        Assert.Equal(
            [nameof(SelfDefinitionOptions.ExternalBaseAddress)],
            typeof(SelfDefinitionOptions).GetProperties().Select(property => property.Name));
    }
}
