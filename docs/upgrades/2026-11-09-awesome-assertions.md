---
title: Replace FluentAssertions with AwesomeAssertions in the tests
impact: recommended
features: [tests]
platforms: []
---

## Why

FluentAssertions 8 and later need a paid license for commercial use. AwesomeAssertions is a community fork under the
Apache 2.0 license, with the same `Should()` API, so the template's tests now use it.

## Who is affected

Apps with `tests/AppTemplate.Core.Tests`. Skip this if your tests already reference `AwesomeAssertions`, or if you hold a
FluentAssertions license and want to keep using it.

## Steps

1. In `src/Directory.Packages.props`, replace the package version, if you list it there:

   ```xml
   <!-- Before -->
   <PackageVersion Include="FluentAssertions" Version="8.7.1" />
   <!-- After -->
   <PackageVersion Include="AwesomeAssertions" Version="9.6.0" />
   ```

2. Do the same for the reference in `tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj`:

   ```xml
   <!-- Before -->
   <PackageReference Include="FluentAssertions" Version="8.7.1" />
   <!-- After -->
   <PackageReference Include="AwesomeAssertions" Version="9.6.0" />
   ```

3. In every test file, replace `using FluentAssertions;` with `using AwesomeAssertions;`, including any nested namespaces
   such as `FluentAssertions.Execution`. Your own tests may use more of the API than the template's do, so let the
   compiler tell you if anything else needs changing.

## Verify

```bash
git grep -n "FluentAssertions"
dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj
```

The search should find nothing, and the tests should pass.
