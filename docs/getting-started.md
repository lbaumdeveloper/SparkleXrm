# Getting started

This guide sets up spkl **once per Visual Studio solution**. Every Dynamics 365 / Dataverse project in the solution is deployed from a single `Tooling` project, and the same commands work from Visual Studio, VS Code and a terminal.

spkl runs on Windows with .NET Framework 4.8 and supports Dynamics 365 v9 on-premises (AD, IFD) and online. Your plugin and workflow projects stay on .NET Framework 4.6.2; spkl reads them without loading them.

## Packages

| Package | Install into | Content |
|---|---|---|
| `Spkl.OnPrem` | `Tooling` only | `spkl.exe`; depends on `Microsoft.CrmSdk.CoreTools` (CrmSvcUtil, SolutionPackager) |
| `Spkl.OnPrem.Attributes` | `Plugins`, `Actions`, `Workflows` | `CrmPluginRegistrationAttribute.cs` as source. Development dependency: nothing is added to the plugin's dependencies. |
| `Spkl.OnPrem.Templates` | `dotnet new install` | The `spkl-solution` template |

The ids differ from the original `spkl` package on nuget.org on purpose, so restore never picks the original instead of this fork.

## 1. Create the solution

```powershell
dotnet new install Spkl.OnPrem.Templates
dotnet new spkl-solution -n Contoso --SolutionUniqueName ContosoCore
cd Contoso
.\Tooling\New-SigningKey.ps1
```

`-n` names the folder, the `.sln` and the assemblies (`Contoso.Plugins.dll`, …). `--SolutionUniqueName` is the Dynamics 365 solution spkl deploys into; it is required, because spkl never deploys to the default solution.

```
Contoso/
├── Contoso.sln
├── Directory.Build.props        ← output to bin\Debug, signs with Tooling\key.snk
├── .vscode/tasks.json           ← VS Code commands (step 3)
├── Tooling/                     ← the only project that references Spkl.OnPrem
│   ├── spkl.json                ← all deployment config for the solution
│   ├── spkl.ps1                 ← runs spkl.exe from the package
│   ├── New-SigningKey.ps1       ← creates key.snk once
│   └── key.snk                  ← commit it
├── Plugins/                     ← Contoso.Plugins
├── Actions/                     ← Contoso.Actions (Custom APIs)
├── Workflows/                   ← Contoso.Workflows (custom workflow activities)
├── Helper/                      ← shared code and early-bound types
└── Webresources/                ← web resource files, path = name
```

- **Signing**: Dynamics 365 only registers strong-named assemblies. `New-SigningKey.ps1` creates `Tooling\key.snk` (same format as `sn.exe -k`); `Directory.Build.props` signs every project with it once it exists. Commit the key so every developer builds the same assembly identity.
- **Helper**: `Plugins`, `Actions` and `Workflows` compile the `.cs` files of `Helper` into their own assembly, because a sandboxed plugin cannot load a second DLL. `Helper` itself is a normal class library, e.g. for unit tests.
- **Webresources** and **Tooling** compile nothing ([NoTargets](https://github.com/microsoft/MSBuildSdks/tree/main/src/NoTargets) projects). They exist so the files show up in Visual Studio.

## 2. Configure `Tooling/spkl.json`

The template fills in the solution and assembly names. Adjust profiles, web resource settings and `earlyboundtypes.entities` as needed.

All paths in `spkl.json` are relative to the folder that contains `spkl.json`, which is why they start with `..\`. The `$schema` line gives you completion and validation in VS Code and Visual Studio. Every option is described in the [spkl.json reference](spkl-json.md).

`spkl.ps1` looks for `spkl.exe` in this order and uses the highest version it finds:

1. `<solution>\packages\Spkl.OnPrem.<version>\tools\spkl.exe` (packages.config)
2. `%NUGET_PACKAGES%\spkl.onprem\<version>\tools\spkl.exe`, or `%USERPROFILE%\.nuget\packages\spkl.onprem\<version>\tools\spkl.exe` (PackageReference, used by the template)

### Existing solution

Without the template:

1. Copy `Tooling\spkl.json`, `Tooling\spkl.ps1`, `Tooling\New-SigningKey.ps1` and `.vscode\tasks.json` from [`templates/solution`](../templates/solution) and replace `Contoso` and `<SolutionUniqueName>`.
2. Install `Spkl.OnPrem` into one project only, e.g. `Tooling`.
3. Install `Spkl.OnPrem.Attributes` into every plugin and workflow project. With `packages.config` the file is added to the project; with PackageReference it is compiled in without appearing in the project.
4. Make sure the plugin assemblies are signed and `assemblypath` in `spkl.json` points to the built DLLs.

## 3. Run commands

Build the solution first. spkl deploys the DLLs that are already in `bin\Debug`; it does not build.

`spkl.ps1` needs a PowerShell execution policy that allows local scripts, e.g. `RemoteSigned`.

### Terminal

```powershell
.\Tooling\spkl.ps1 -Task plugins                    # plugins from all three assemblies
.\Tooling\spkl.ps1 -Task plugins -Profile actions   # only the Actions assembly
.\Tooling\spkl.ps1 -Task workflow -Profile workflows
.\Tooling\spkl.ps1 -Task webresources
.\Tooling\spkl.ps1 -Task earlybound
.\Tooling\spkl.ps1 -Task plugins -DryRun            # only show what would change
```

### VS Code

`Ctrl+Shift+P` → **Tasks: Run Task** → pick a `spkl: …` task.

### Visual Studio

**Tools → External Tools… → Add**, once per command:

| Field | Value |
|---|---|
| Title | `spkl: deploy plugins` |
| Command | `powershell.exe` |
| Arguments | `& '$(SolutionDir)Tooling\spkl.ps1' -Task plugins` |
| Initial directory | `$(SolutionDir)` |
| Use Output window | unchecked (the login prompt needs a console) |

## 4. Connecting to Dataverse

Without a connection string spkl shows a console login:

1. It lists saved environments from `%APPDATA%\spkl\Connections.json` (no credentials are stored there).
2. For a new one it asks for the environment URL, e.g. `contoso.crm4.dynamics.com`, and opens the Microsoft sign-in window.
3. Tokens are cached in `%APPDATA%\spkl\TokenCache`, so later runs usually do not prompt.

To skip the prompt, set a [Dataverse connection string](https://learn.microsoft.com/power-apps/developer/data-platform/xrm-tooling/use-connection-strings-xrm-tooling-connect) for your session:

```powershell
# On-premises with the logged-in Windows user
$env:SPKL_CONNECTION = "AuthType=AD;Url=https://crm.contoso.local/Contoso"

# On-premises IFD
$env:SPKL_CONNECTION = "AuthType=IFD;Url=https://contoso.crm.contoso.com;Domain=CONTOSO;Username=jsmith;Password=***"

# Online
$env:SPKL_CONNECTION = "AuthType=ClientSecret;Url=https://contoso.crm4.dynamics.com;ClientId=...;ClientSecret=..."
```

`Password=***` makes spkl ask for the password, so it never sits in the variable. Without a connection string, `spkl <task> /l` shows the on-premises console login instead of the online one.

Never commit connection strings. `-Connection` on `spkl.ps1` overrides the environment variable.

Use `-Task whoami` to check which environment and user you are connected as.
