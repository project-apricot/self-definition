namespace ApricotFramework.SelfDefinition.AspNetCore.Options;

/// <summary>
/// The facts a service states about itself, bound from configuration.
/// </summary>
public sealed class SelfDefinitionOptions
{
    /// <summary>
    /// The origin the service is reached at from outside — for example
    /// <c>https://api.example.com</c>, or <c>https://example.com/api</c> when the service is
    /// mounted under a path base.
    /// </summary>
    /// <remarks>
    /// Required. It is validated when the host starts, so an unusable value stops the host rather
    /// than failing the first request that needs to build a link. A trailing slash is accepted and
    /// trimmed off. A query string, a fragment, embedded credentials, and any scheme other than
    /// <c>http</c> or <c>https</c> are rejected.
    /// </remarks>
    public string? ExternalBaseAddress { get; set; }
}
