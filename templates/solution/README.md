# Solution template

Copy these files into the root of your Visual Studio solution:

| File | Purpose |
|---|---|
| `Directory.Build.targets` | Links `Tooling\CrmPluginRegistrationAttribute.cs` into projects with `<SpklRegistration>true</SpklRegistration>` |
| `Tooling/spkl.json` | Deployment config for all projects; replace the `<...>` placeholders |
| `Tooling/spkl.ps1` | Finds `spkl.exe` in the spkl NuGet package and runs a task against `Tooling/spkl.json` |
| `.vscode/tasks.json` | VS Code tasks for the common commands |

Step-by-step setup: [docs/getting-started.md](../../docs/getting-started.md).
