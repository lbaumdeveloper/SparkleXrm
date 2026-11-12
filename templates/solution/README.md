# Contoso

Dynamics 365 solution deployed with [spkl](https://github.com/lbaumdeveloper/SparkleXrm).

| Project | Purpose |
|---|---|
| `Plugins`, `Actions`, `Workflows` | Plugin and workflow assemblies. Register steps with `[CrmPluginRegistration]`. |
| `Helper` | Shared code and early-bound types, compiled into each of the three assemblies above. |
| `Webresources` | Web resources. The path below this folder is the name, e.g. `new_/js/account.js`. |
| `Tooling` | `spkl.json` for the whole solution and `spkl.ps1`, which runs spkl. |

Build first, then deploy:

```powershell
.\Tooling\spkl.ps1 -Task plugins
.\Tooling\spkl.ps1 -Task workflow -Profile workflows
.\Tooling\spkl.ps1 -Task webresources
.\Tooling\spkl.ps1 -Task plugins -DryRun
```

In VS Code use **Tasks: Run Task → spkl: …**. Set `$env:SPKL_CONNECTION` to skip the login prompt.
