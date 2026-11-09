---
title: Store complex preferences through source-generated JSON
impact: recommended
features: []
platforms: []
---

## Why

`Preferences.GetComplex` and `SetComplex` used reflection-based `System.Text.Json`, which is off in a trimmed build. Saving
a theme or any other complex value could fail at runtime on a trimmed head, while Debug builds worked fine.

## Who is affected

Every app with `src/AppTemplate/Services/Settings/Preferences.cs`. Skip this if its `SetComplex` already passes a
`JsonTypeInfo`.

## Steps

`mzuno-app upgrade --patch` delivers steps 1 and 2. Step 3 is yours, because only you know what your app stores.

1. Create `src/AppTemplate/Services/Settings/PreferencesJsonContext.cs`:

   ```csharp
   using System.Text.Json.Serialization;

   namespace AppTemplate.Services.Settings;

   [JsonSerializable(typeof(ElementTheme))]
   internal sealed partial class PreferencesJsonContext : JsonSerializerContext
   {
   }
   ```

2. In `Preferences.cs`, add these usings and members:

   ```csharp
   using System.Text.Json.Serialization.Metadata;
   using AppTemplate.Core.Models;

   // Trimmed builds have reflection-based JSON off, so complex values need source-generated metadata.
   private static readonly JsonSerializerOptions JsonOptions = new()
   {
       TypeInfoResolver = JsonTypeInfoResolver.Combine(PreferencesJsonContext.Default, AppTemplateJsonContext.Default),
   };

   private static JsonTypeInfo<T> GetTypeInfo<T>() => (JsonTypeInfo<T>)JsonOptions.GetTypeInfo(typeof(T));
   ```

   Then change the two calls:

   ```diff
   -                if (JsonSerializer.Deserialize<T>(json) is { } result)
   +                if (JsonSerializer.Deserialize(json, GetTypeInfo<T>()) is { } result)
   ```

   ```diff
   -    public void SetComplex<T>(string key, T? value) => _container.Values[key] = JsonSerializer.Serialize(value);
   +    public void SetComplex<T>(string key, T? value) => _container.Values[key] = JsonSerializer.Serialize(value, GetTypeInfo<T?>());
   ```

3. Find every type your app passes to `GetComplex` or `SetComplex`, and register each one: app models in
   `AppTemplateJsonContext`, head-only types such as WinUI enums in `PreferencesJsonContext`. An unregistered type throws
   `NotSupportedException` when saved. See
   [json-aot-serialization.md](https://github.com/MartinZikmund/uno-app-template/blob/main/docs/json-aot-serialization.md#preferences).

## Verify

```bash
git grep -n "GetComplex\|SetComplex" -- src
```

Check that each type it lists is registered. Then run a Release build of a trimmed head, change a stored setting such as
the theme, restart, and confirm it stuck.
