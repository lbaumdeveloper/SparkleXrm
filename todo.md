# TODO

## Done
- [x] Trim repository to spkl only
- [x] Single install per solution: Tooling project with one `spkl.json`
- [x] Share `CrmPluginRegistrationAttribute.cs` via `Directory.Build.targets`
- [x] `spkl.ps1` wired into VS Code tasks and Visual Studio External Tools
- [x] Documentation written from the source: getting started, commands, spkl.json, attributes

## Reduce magic (code changes)
- [ ] Stop the implicit `**\` prefix on `assemblypath`; resolve paths relative to spkl.json only
- [ ] Stop the recursive `spkl.json` search; use exactly the given file or folder
- [ ] `get-webresources`: scan `root` instead of the spkl.json folder
- [ ] `download-webresources`: use `webresources[].root` and `solution` instead of requiring a `solutions[].map`
- [ ] Apply `SecureConfiguration` and `Action`, or remove them from the attribute
- [ ] `compare`: implement or remove
- [ ] Allow spaces in `profile` lists of the `solutions` section (other sections already trim)
- [ ] Add a `--dry-run` that lists what would be created, updated and deleted
- [ ] Find `CrmSvcUtil.exe` / `SolutionPackager.exe` without scanning the whole NuGet cache
- [ ] JSON schema for `spkl.json` for editor completion

## Modernization
- [ ] Convert projects to SDK-style `.csproj`
- [ ] Replace `Microsoft.Xrm.Tooling.Connector` / ADAL with `Microsoft.PowerPlatform.Dataverse.Client`
- [ ] Move CLI to .NET 8 and ship it as a dotnet local tool (`dotnet-tools.json`, `dotnet tool restore`)
- [ ] Ship `CrmPluginRegistrationAttribute` as a source-only NuGet package
- [ ] CI pipeline: build and run `SparkleXrm.Tasks.Tests`
- [ ] `dotnet new` solution template (Webresources, Plugins, Actions, Workflows, Helper, Tooling)
- [ ] Update NuGet metadata (authors, project URL, icon)
