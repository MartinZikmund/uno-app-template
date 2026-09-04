# AppTemplate API

An ASP.NET Core minimal API plus a shared contracts library and a typed Refit
client. Three projects make up the API:

| Project | SDK / TFM | Purpose |
| --- | --- | --- |
| `AppTemplate.Api` | `Microsoft.NET.Sdk.Web`, `net10.0` | Minimal API server. Exposes an example `items` resource and an OpenAPI document. |
| `AppTemplate.Api.Contracts` | `Microsoft.NET.Sdk`, `net10.0` | Request/response DTOs shared by the server and client, plus a source-generated `JsonSerializerContext`. |
| `AppTemplate.Api.Client` | `Microsoft.NET.Sdk`, `net10.0` | Typed [Refit](https://github.com/reactiveui/refit) client (`IAppTemplateApiClient`) and a DI registration helper. |

## Endpoints

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/v1/items` | Lists all items. |
| `GET` | `/api/v1/items/{id}` | Gets a single item by id (404 when missing). |
| `POST` | `/api/v1/items` | Creates a new item. |
| `GET` | `/openapi/v1.json` | OpenAPI document (enabled via `AddOpenApi` / `MapOpenApi`). |

The server uses the shared `ApiJsonSerializerContext` for trimming/AOT-friendly,
reflection-free JSON. The data lives in an in-memory `ItemStore` seeded at
startup; swap it for a real data source (database, external service, ...) as the
API grows.

## Running the server

```bash
dotnet run --project src/AppTemplate.Api/AppTemplate.Api.csproj
```

The OpenAPI document is then available at `http://localhost:5080/openapi/v1.json`.

## Consuming the client from the app

`AppTemplate.Api.Client` is a standalone library that is not referenced by the
Uno head by default. To consume the API, add a reference to
`AppTemplate.Api.Client` and register the typed client during host configuration
(for example in `App.xaml.cs` / the `IHostBuilder` setup):

```csharp
using AppTemplate.Api.Client;

services.AddAppTemplateApiClient(new Uri("https://your-api-host/"));
```

Then inject `IAppTemplateApiClient` into a service or view model:

```csharp
public sealed class ItemsService(IAppTemplateApiClient api)
{
    public Task<IReadOnlyList<ItemDto>> GetItemsAsync(CancellationToken ct = default) =>
        api.GetItemsAsync(ct);
}
```

`AddAppTemplateApiClient` returns the `IHttpClientBuilder`, so you can chain
additional configuration (auth handlers, Polly/resilience, etc.) as needed.

## JSON serialization context

`ApiJsonSerializerContext` lives in `AppTemplate.Api.Contracts` and is shared by
the server (`ConfigureHttpJsonOptions`) and the client (Refit
`SystemTextJsonContentSerializer`). Following the convention that each external
API boundary owns its own source-generated `JsonSerializerContext`, every DTO and
the collections returned by the endpoints are declared with
`[JsonSerializable(typeof(...))]`. The context is reflection-free and zero-cost at
runtime (the only cost is build-time codegen), which keeps serialization AOT- and
trimming-safe on iOS and other NativeAOT targets. When you add a contract type,
add a matching `[JsonSerializable]` attribute.
