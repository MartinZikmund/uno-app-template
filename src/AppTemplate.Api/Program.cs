using AppTemplate.Api;
using AppTemplate.Api.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Use the shared, source-generated JSON context for AOT/trimming-friendly serialization.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiJsonSerializerContext.Default));

builder.Services.AddSingleton<ItemStore>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();

var items = app.MapGroup("/api/v1/items");

items.MapGet("/", (ItemStore store) => Results.Ok(store.GetAll()))
    .WithName("GetItems")
    .WithSummary("Gets all items.");

items.MapGet("/{id:int}", (int id, ItemStore store) =>
        store.Get(id) is { } item ? Results.Ok(item) : Results.NotFound())
    .WithName("GetItem")
    .WithSummary("Gets a single item by its identifier.");

items.MapPost("/", (CreateItemRequest request, ItemStore store) =>
    {
        ItemDto created = store.Add(request);
        return Results.Created($"/api/v1/items/{created.Id}", created);
    })
    .WithName("CreateItem")
    .WithSummary("Creates a new item.");

app.Run();
