using ApricotFramework.SelfDefinition.AspNetCore.Impl;
using ApricotFramework.SelfDefinition.AspNetCore.Options;
using ApricotFramework.SelfDefinition.AspNetCore.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApricotFramework.SelfDefinition.AspNetCore.Extensions;

/// <summary>
/// Registers self-definition in a service collection.
/// </summary>
public static class SelfDefinitionServiceCollectionExtensions
{
    /// <summary>
    /// The configuration section bound when no other is named.
    /// </summary>
    public const string ConfigurationSectionName = "SelfDefinition";

    /// <summary>
    /// Adds self-definition, binding its settings from the
    /// <see cref="ConfigurationSectionName"/> configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to bind from.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddSelfDefinition(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddSelfDefinition(configuration, ConfigurationSectionName);
    }

    /// <summary>
    /// Adds self-definition, binding its settings from a named configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to bind from.</param>
    /// <param name="sectionName">The section to bind.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddSelfDefinition(this IServiceCollection services, IConfiguration configuration, string sectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddOptions<SelfDefinitionOptions>().Bind(configuration.GetSection(sectionName));

        return AddSelfDefinitionCore(services);
    }

    /// <summary>
    /// Adds self-definition, configuring its settings in code.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the settings.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddSelfDefinition(this IServiceCollection services, Action<SelfDefinitionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<SelfDefinitionOptions>().Configure(configure);

        return AddSelfDefinitionCore(services);
    }

    /// <summary>
    /// Registers the validator and the provider itself.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    private static IServiceCollection AddSelfDefinitionCore(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SelfDefinitionOptions>, SelfDefinitionOptionsValidator>());

        // Surface a missing or unusable base address when the host starts rather than on the first
        // request that needs to build a link. In a bare ServiceProvider there is nothing to run the
        // startup hook, so the validator still runs on first access to the options.
        services.AddOptions<SelfDefinitionOptions>().ValidateOnStart();

        services.TryAddSingleton<ISelfDefinitionProvider, OptionsAwareSelfDefinitionProvider>();

        return services;
    }
}
