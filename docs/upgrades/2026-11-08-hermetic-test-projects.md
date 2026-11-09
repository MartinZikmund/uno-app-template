---
title: Stop test projects inheriting MSBuild files from above the repository
impact: recommended
features: [tests]
platforms: []
---

## Why

Test projects sit outside `src/`, so MSBuild kept searching up the disk for shared build files. If any folder above
the repository has a `Directory.Packages.props`, as a home folder sometimes does, restore fails with `NU1008`:

```text
error NU1008: The following PackageReference items cannot define a value for Version
```

The same search could also pull in an unrelated `Directory.Build.props`.

## Who is affected

Every app with a `tests/` folder, though you only see the failure on a machine with one of those files above the
repository. Skip this if `tests/Directory.Build.props` already exists.

## Steps

1. Add `tests/Directory.Build.props`:

   ```xml
   <Project>
     <PropertyGroup>
       <ImportDirectoryPackagesProps>false</ImportDirectoryPackagesProps>
     </PropertyGroup>
   </Project>
   ```

2. Add an empty `tests/Directory.Build.targets`:

   ```xml
   <Project>
   </Project>
   ```

3. If your tests look for the repository root by checking for `AGENTS.md`, check for `version.json` instead. Only
   `version.json` is present in every app created from the template.

## Verify

```bash
dotnet test tests/AppTemplate.Core.Tests/AppTemplate.Core.Tests.csproj
```
