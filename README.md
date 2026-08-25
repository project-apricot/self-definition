# ApricotFramework.SelfDefinition.AspNetCore

[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.SelfDefinition.AspNetCore.svg?label=ApricotFramework.SelfDefinition.AspNetCore)](https://www.nuget.org/packages/ApricotFramework.SelfDefinition.AspNetCore/)
[![CI](https://github.com/project-apricot/self-definition/actions/workflows/ci.yml/badge.svg)](https://github.com/project-apricot/self-definition/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/project-apricot/self-definition/blob/main/LICENSE)

Lets an ASP.NET Core service state facts about itself instead of inferring them from whatever
request happens to arrive. Chiefly its **external base address** — the origin it is reached at from
outside — so code building absolute links does not have to guess.

## Install

```bash
dotnet add package ApricotFramework.SelfDefinition.AspNetCore
```

## Usage

```csharp
using ApricotFramework.SelfDefinition.AspNetCore.Extensions;

builder.Services.AddSelfDefinition(builder.Configuration);
```

```json
{
  "SelfDefinition": {
    "ExternalBaseAddress": "https://api.example.com/gateway"
  }
}
```

```csharp
using ApricotFramework.SelfDefinition.AspNetCore;

app.MapGet("/link", (ISelfDefinitionProvider selfDefinition) =>
{
    // Never ends in a slash, so one separator is always correct.
    var baseAddress = selfDefinition.GetExternalBaseAddress();

    return Results.Ok($"{baseAddress}/account/reset?token=abc123");
});
```

`ExternalBaseAddress` is **required and validated when the host starts**, so a missing or unusable
value stops the host rather than producing wrong links later. Rejected: anything that is not an
absolute `http`/`https` URL, embedded credentials, a query string, and a fragment. Note that a bare
path such as `/api` is rejected too — it parses as an absolute `file:` URI, so it fails the scheme
check rather than the absolute-URL one.

Full documentation: <https://projectapricot.dev/docs/self-definition>
