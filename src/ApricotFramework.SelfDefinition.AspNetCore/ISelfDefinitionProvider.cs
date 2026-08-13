namespace ApricotFramework.SelfDefinition.AspNetCore;

/// <summary>
/// Supplies the facts a service states about itself, rather than inferring them from whatever
/// happens to reach it.
/// </summary>
public interface ISelfDefinitionProvider
{
    /// <summary>
    /// Gets the origin the service is reached at from outside: scheme, host, optional port and
    /// optional path base, with no trailing slash.
    /// </summary>
    /// <returns>
    /// The external base address, with any trailing slash removed, so it can be prefixed onto a
    /// relative path with a single separator between them.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no external base address is configured.
    /// </exception>
    string GetExternalBaseAddress();
}
