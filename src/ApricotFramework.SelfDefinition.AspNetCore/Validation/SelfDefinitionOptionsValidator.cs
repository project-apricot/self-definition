using ApricotFramework.SelfDefinition.AspNetCore.Options;
using Microsoft.Extensions.Options;

namespace ApricotFramework.SelfDefinition.AspNetCore.Validation;

/// <summary>
/// Rejects an external base address that cannot serve as the prefix of an absolute link.
/// </summary>
/// <remarks>
/// Every rejection here is something that would otherwise surface much later: either as a request
/// that cannot build a link, or as a link that is built and is wrong.
/// </remarks>
public sealed class SelfDefinitionOptionsValidator : IValidateOptions<SelfDefinitionOptions>
{
    /// <summary>
    /// An example of a well-formed value, quoted in the failure messages so an operator seeing one
    /// in a crash log knows what is wanted.
    /// </summary>
    private const string ExampleAddress = "https://api.example.com";

    /// <summary>
    /// The only schemes an external base address may use. Anything else is either not reachable by
    /// an HTTP client or not an origin at all.
    /// </summary>
    private static readonly string[] AllowedSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps];

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, SelfDefinitionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Surrounding whitespace is trimmed rather than rejected, because Uri tolerates it and
        // would otherwise let " https://x " validate here and be returned with the spaces intact.
        var configured = options.ExternalBaseAddress?.Trim();

        if (string.IsNullOrEmpty(configured))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(SelfDefinitionOptions.ExternalBaseAddress)} is not configured. Set it to the origin this service is reached at from outside, for example '{ExampleAddress}'.");
        }

        if (!Uri.TryCreate(configured, UriKind.Absolute, out var address))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(SelfDefinitionOptions.ExternalBaseAddress)} '{configured}' is not an absolute URL. Expected something of the form '{ExampleAddress}'.");
        }

        // Uri lower-cases the scheme it parsed, so an ordinal comparison is right here and a
        // culture-sensitive one would never be, this being a protocol token.
        //
        // This check carries more weight than it looks: a bare path such as "/api", and a
        // scheme-relative "//example.com", both parse as *absolute* URIs under the file scheme, so
        // the check above passes them and only this one catches them.
        if (!AllowedSchemes.Contains(address.Scheme, StringComparer.Ordinal))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(SelfDefinitionOptions.ExternalBaseAddress)} '{configured}' uses the '{address.Scheme}' scheme, but only {string.Join(" and ", AllowedSchemes)} are supported. Include the scheme explicitly, for example '{ExampleAddress}'.");
        }

        if (!string.IsNullOrEmpty(address.UserInfo))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(SelfDefinitionOptions.ExternalBaseAddress)} '{configured}' embeds credentials, which would be copied into every link built from it. Remove the user information before the host.");
        }

        if (!string.IsNullOrEmpty(address.Query))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(SelfDefinitionOptions.ExternalBaseAddress)} '{configured}' carries a query string. A base address is a prefix, so anything appended to it would land after the query and be ignored.");
        }

        if (!string.IsNullOrEmpty(address.Fragment))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(SelfDefinitionOptions.ExternalBaseAddress)} '{configured}' carries a fragment. A base address is a prefix, so anything appended to it would land inside the fragment.");
        }

        return ValidateOptionsResult.Success;
    }
}
