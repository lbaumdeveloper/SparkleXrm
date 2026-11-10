# spkl

Deployment task runner for Dynamics 365 / Dataverse. spkl deploys plugins, Custom APIs, workflow activities and web resources, generates early-bound classes, and packs/unpacks solutions, all from one `spkl.json` and the `[CrmPluginRegistration]` attributes in your code.

This repository contains only spkl. The original SparkleXrm framework, UI components and samples were removed.

## Documentation

| Guide | Read it when |
|---|---|
| [Getting started](docs/getting-started.md) | Setting up a solution: install spkl once in a Tooling project, run it from VS Code, Visual Studio or a terminal. |
| [Commands](docs/commands.md) | You want to know what a task does, what it reads and what it changes or deletes. |
| [spkl.json reference](docs/spkl-json.md) | Writing or debugging `spkl.json`: every option, paths and profiles. |
| [Registration attributes](docs/registration-attributes.md) | Registering plugin steps, images, Custom APIs and workflow activities in code. |

A ready-to-copy solution setup is in [`templates/solution`](templates/solution).

## Quick start

```powershell
Install-Package spkl -ProjectName Tooling      # once, only in the Tooling project
.\Tooling\spkl.ps1 -Task plugins               # deploy plugins
.\Tooling\spkl.ps1 -Task webresources          # deploy web resources
```

## Requirements

- Windows (spkl targets .NET Framework 4.6.2)
- A Dataverse / Dynamics 365 environment

## Repository layout

| Folder | Content |
|---|---|
| `spkl/` | Source: CLI (`spkl`), tasks (`SparkleXrm.Tasks`), tests and test projects |
| `docs/` | Documentation |
| `templates/solution/` | Files to copy into your own solution |

Planned work is tracked in [`todo.md`](todo.md).

License: [MIT](MIT-License.txt)
