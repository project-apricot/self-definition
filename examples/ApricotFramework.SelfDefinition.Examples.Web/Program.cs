using ApricotFramework.SelfDefinition.AspNetCore;
using ApricotFramework.SelfDefinition.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// One call binds the "SelfDefinition" configuration section, registers the validator and registers
// ISelfDefinitionProvider. Try setting ExternalBaseAddress to "example.com" or deleting it from
// appsettings.json to watch the host refuse to boot instead of failing a later request.
builder.Services.AddSelfDefinition(builder.Configuration);

var app = builder.Build();

// What the service says its own external origin is.
app.MapGet("/self", (ISelfDefinitionProvider selfDefinition) => Results.Ok(new
{
    ExternalBaseAddress = selfDefinition.GetExternalBaseAddress(),
}));

// The reason the library exists: a link that is correct when read outside the process, rather than
// one built from whatever host header happened to arrive.
app.MapGet("/link", (ISelfDefinitionProvider selfDefinition) =>
{
    var externalBaseAddress = selfDefinition.GetExternalBaseAddress();

    return Results.Ok(new
    {
        ExternalBaseAddress = externalBaseAddress,

        // The address never ends in a slash, so one separator is always right.
        ThingUrl = $"{externalBaseAddress}/api/things/42",
        PasswordResetUrl = $"{externalBaseAddress}/account/reset?token=abc123",
    });
});

// The contrast worth seeing: what the request itself claims, next to what the service is configured
// to be. These differ behind a proxy, a gateway or a tunnel — and the request side is attacker
// controlled, which is why links are built from the configured value.
app.MapGet("/compare", (HttpRequest request, ISelfDefinitionProvider selfDefinition) => Results.Ok(new
{
    Configured = selfDefinition.GetExternalBaseAddress(),
    FromThisRequest = $"{request.Scheme}://{request.Host}{request.PathBase}",
}));

app.Run();
