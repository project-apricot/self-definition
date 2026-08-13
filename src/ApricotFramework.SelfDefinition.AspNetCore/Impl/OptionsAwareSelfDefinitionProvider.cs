using ApricotFramework.SelfDefinition.AspNetCore.Options;
using Microsoft.Extensions.Options;

namespace ApricotFramework.SelfDefinition.AspNetCore.Impl;

/// <summary>
/// A self-definition provider that reads its settings from the options system on every call, so a
/// configuration reload takes effect without restarting the host.
/// </summary>
public class OptionsAwareSelfDefinitionProvider : ISelfDefinitionProvider
{
    /// <summary>
    /// The monitor supplying the current settings.
    /// </summary>
    private readonly IOptionsMonitor<SelfDefinitionOptions> optionsMonitor;

    /// <summary>
    /// Creates a new provider.
    /// </summary>
    /// <param name="optionsMonitor">The monitor supplying the current settings.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="optionsMonitor"/> is null.
    /// </exception>
    public OptionsAwareSelfDefinitionProvider(IOptionsMonitor<SelfDefinitionOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        this.optionsMonitor = optionsMonitor;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The value is returned as configured apart from the trimming: the case is not lowered and an
    /// international host is not punycoded, so what an operator wrote is what links contain.
    /// </remarks>
    public virtual string GetExternalBaseAddress()
    {
        var configured = this.GetCurrentOptions().ExternalBaseAddress?.Trim().TrimEnd('/');

        // Unreachable when registered through AddSelfDefinition, which validates at startup. It
        // stands for a provider constructed by hand around settings nothing has validated.
        if (string.IsNullOrEmpty(configured))
        {
            throw new InvalidOperationException(
                $"No {nameof(SelfDefinitionOptions.ExternalBaseAddress)} is configured, so the external base address cannot be determined.");
        }

        return configured;
    }

    /// <summary>
    /// Gets the settings in force for this call.
    /// </summary>
    /// <returns>The current settings.</returns>
    /// <remarks>
    /// Override this to source the settings from somewhere other than the options monitor.
    /// </remarks>
    protected virtual SelfDefinitionOptions GetCurrentOptions()
    {
        return this.optionsMonitor.CurrentValue;
    }
}
