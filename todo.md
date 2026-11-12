# TODO

## Done
- [x] Trim repository to spkl only
- [x] Single install per solution: Tooling project with one `spkl.json`
- [x] Share `CrmPluginRegistrationAttribute.cs` via `Directory.Build.targets`
- [x] `spkl.ps1` wired into VS Code tasks and Visual Studio External Tools
- [x] Documentation written from the source: getting started, commands, spkl.json, attributes

## Reduce magic (code changes)
- [x] Stop the implicit `**\` prefix on `assemblypath`; resolve paths relative to spkl.json only
- [x] Stop the recursive `spkl.json` search; use exactly the given file or folder
- [x] `get-webresources`: scan `root` instead of the spkl.json folder, and fix its multi-extension search that matched nothing
- [x] `download-webresources`: use `webresources[].root` and `solution` instead of requiring a `solutions[].map`
- [x] Remove `SecureConfiguration` and `Action` from the attribute (never applied)
- [x] Replace the `.bat` scripts in the NuGet package with `spkl.ps1`
- [x] `compare`: removed
- [x] Allow spaces in `profile` lists of the `solutions` section
- [x] Add `/dry-run` that lists what would be created, updated and deleted
- [x] Find `CrmSvcUtil.exe` / `SolutionPackager.exe` without scanning the whole NuGet cache
- [x] JSON schema for `spkl.json` for editor completion

## Modernization
- [x] Convert projects to SDK-style `.csproj`
- [x] Target .NET Framework 4.8 (spkl targets mostly Dynamics 365 v9 on-premises, so .NET 8 and `Microsoft.PowerPlatform.Dataverse.Client` are out)
- [x] Update `Microsoft.CrmSdk.XrmTooling.CoreAssembly` to 9.1.1.103
- [x] Read plugin assemblies with `MetadataLoadContext` instead of `Assembly.ReflectionOnlyLoad`
- [x] Replace the `CmdLine` package with a built-in argument parser
- [x] Ship `CrmPluginRegistrationAttribute` as a source-only NuGet package (`Spkl.OnPrem.Attributes`)
- [x] CI pipeline: build and run the `SparkleXrm.Tasks.Tests` unit tests
- [x] `dotnet new` solution template (Webresources, Plugins, Actions, Workflows, Helper, Tooling)
- [x] Update NuGet metadata (ids `Spkl.OnPrem*`, authors, project URL, MIT license expression)
- [x] Package icon
- [x] `build/Pack.ps1` packs all three packages; `.gitlab-ci.yml` publishes them on the private GitLab (never from GitHub)
- [ ] Mirror the repo to the private GitLab, register a Windows runner and publish the first version
