# TODO

## Developer experience
- [ ] Single install per solution: Tooling project with one `spkl.json` for plugins, workflows, actions and webresources
- [ ] Share `CrmPluginRegistrationAttribute.cs` via `Directory.Build.props` instead of the NuGet content file per project
- [ ] Deploy scripts wired into `.vscode/tasks.json` and Visual Studio External Tools
- [ ] Rewrite README for spkl only, documenting the solution layout

## Modernization
- [ ] Convert projects to SDK-style `.csproj`
- [ ] Replace `Microsoft.Xrm.Tooling.Connector` / ADAL with `Microsoft.PowerPlatform.Dataverse.Client`
- [ ] Support modern auth (client secret, certificate, interactive OAuth) via connection strings
- [ ] Move CLI to .NET 8 and ship it as a dotnet local tool (`dotnet-tools.json`, `dotnet tool restore`)
- [ ] Keep `SparkleXrm.Tasks` plugin-facing parts on .NET Framework 4.6.2 where Dataverse requires it
- [ ] CI pipeline: build and run `SparkleXrm.Tasks.Tests`
- [ ] `dotnet new` solution template (Webresources, Plugins, Actions, Workflows, Helper, Tooling)
- [ ] Update NuGet metadata (authors, project URL, icon)
