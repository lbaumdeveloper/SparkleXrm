# Getting started

This guide sets up spkl **once per Visual Studio solution**. Every Dynamics 365 / Dataverse project in the solution is deployed from a single `Tooling` project, and the same commands work from Visual Studio, VS Code and a terminal.

spkl runs on Windows only (it targets .NET Framework 4.6.2).

## 1. Solution layout

```
MySolution/
├── MySolution.sln
├── Directory.Build.targets      ← shares the registration attribute (step 3)
├── .vscode/tasks.json           ← VS Code commands (step 5)
├── packages/                    ← NuGet packages (packages.config projects)
├── Tooling/                     ← the only project that references spkl
│   ├── spkl.json                ← all deployment config for the solution
│   ├── spkl.ps1                 ← runs spkl.exe from the package
│   └── CrmPluginRegistrationAttribute.cs   ← added by the spkl package
├── Plugins/
├── Actions/
├── Workflows/
├── Helper/
└── Webresources/
```

Copy the files from [`templates/solution`](../templates/solution) into your solution root.

## 2. Install spkl into the Tooling project only

Create `Tooling` as a .NET Framework class library that uses `packages.config`, then:

```powershell
Install-Package spkl -ProjectName Tooling
```

The package adds `spkl.json`, `CrmPluginRegistrationAttribute.cs` and a `spkl\*.bat` folder to the project. Replace the generated `spkl.json` with the one from the template. The `.bat` files are not needed; `spkl.ps1` replaces them.

`spkl.ps1` looks for `spkl.exe` in this order and uses the highest version it finds:

1. `<solution>\packages\spkl.<version>\tools\spkl.exe` (packages.config)
2. `%NUGET_PACKAGES%\spkl\<version>\tools\spkl.exe`, or `%USERPROFILE%\.nuget\packages\spkl\<version>\tools\spkl.exe` (PackageReference)

## 3. Share the registration attribute

Plugin and workflow assemblies describe their own registration with `[CrmPluginRegistration(...)]` (see [registration attributes](registration-attributes.md)). The attribute class must be compiled into each of those assemblies, because Dataverse loads plugin assemblies on their own.

`Directory.Build.targets` links `Tooling\CrmPluginRegistrationAttribute.cs` into every project that opts in. Add this to `Plugins.csproj`, `Actions.csproj` and `Workflows.csproj`:

```xml
<PropertyGroup>
  <SpklRegistration>true</SpklRegistration>
</PropertyGroup>
```

Do **not** install the spkl package into these projects.

## 4. Configure `Tooling/spkl.json`

Replace the placeholders in the template:

| Placeholder | Example |
|---|---|
| `<SolutionUniqueName>` | `ContosoCore` |
| `<PluginsAssembly>` | `Contoso.Plugins` |
| `<ActionsAssembly>` | `Contoso.Actions` |
| `<WorkflowsAssembly>` | `Contoso.Workflows` |
| `<Namespace>` | `Contoso` |

All paths in `spkl.json` are relative to the folder that contains `spkl.json`, which is why they start with `..\`. Every option is described in the [spkl.json reference](spkl-json.md).

## 5. Run commands

Build the solution first. spkl deploys the DLLs that are already in `bin\Debug`; it does not build.

### Terminal

```powershell
.\Tooling\spkl.ps1 -Task plugins                    # plugins from all three assemblies
.\Tooling\spkl.ps1 -Task plugins -Profile actions   # only the Actions assembly
.\Tooling\spkl.ps1 -Task workflow -Profile workflows
.\Tooling\spkl.ps1 -Task webresources
.\Tooling\spkl.ps1 -Task earlybound
```

### VS Code

`Ctrl+Shift+P` → **Tasks: Run Task** → pick a `spkl: …` task.

### Visual Studio

**Tools → External Tools… → Add**, once per command:

| Field | Value |
|---|---|
| Title | `spkl: deploy plugins` |
| Command | `powershell.exe` |
| Arguments | `-NoProfile -ExecutionPolicy Bypass -File "$(SolutionDir)Tooling\spkl.ps1" -Task plugins` |
| Initial directory | `$(SolutionDir)` |
| Use Output window | unchecked (the login prompt needs a console) |

## 6. Connecting to Dataverse

Without a connection string spkl shows a console login:

1. It lists saved environments from `%APPDATA%\spkl\Connections.json` (no credentials are stored there).
2. For a new one it asks for the environment URL, e.g. `contoso.crm4.dynamics.com`, and opens the Microsoft sign-in window.
3. Tokens are cached in `%APPDATA%\spkl\TokenCache`, so later runs usually do not prompt.

To skip the prompt, set a [Dataverse connection string](https://learn.microsoft.com/power-apps/developer/data-platform/xrm-tooling/use-connection-strings-xrm-tooling-connect) for your session:

```powershell
$env:SPKL_CONNECTION = "AuthType=ClientSecret;Url=https://contoso.crm4.dynamics.com;ClientId=...;ClientSecret=..."
```

Never commit connection strings. `-Connection` on `spkl.ps1` overrides the environment variable.

Use `-Task whoami` to check which environment and user you are connected as.
