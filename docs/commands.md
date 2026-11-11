# Command reference

```
spkl <task> [path] [connection-string] [/p:<profile>] [switches]
```

| Argument | Meaning |
|---|---|
| `task` | One of the tasks below. |
| `path` | The folder that contains `spkl.json`, or the path of the `spkl.json` file itself. Sub-folders are not searched. Defaults to the current directory. |
| `connection-string` | Optional. Without it spkl shows the console login (see [getting started](getting-started.md#6-connecting-to-dataverse)). Pass `""` for tasks that need no connection. A `Password=***` placeholder makes spkl prompt for the password. |
| `/p:<profile>` | Only use `spkl.json` entries whose `profile` contains this name. Without it, entries with no profile or with `default` in their profile are used. See [profiles](spkl-json.md#profiles). |

Switches:

| Switch | Meaning |
|---|---|
| `/e` | `plugins` only: register assemblies and types but not steps. |
| `/o` | `download-webresources` only: overwrite existing local files. |
| `/s:<prefix>` | `get-webresources` only: publisher prefix to strip when matching files, e.g. `/s:new`. |
| `/w` | Wait for a key press before exiting. |
| `/?` | Show the help. |

With `Tooling\spkl.ps1` you only pass `-Task`, `-Profile` and optionally `-Connection` or `-Path`; it fills in the rest.

---

## Deployment

### `plugins`

Registers plugins from the assemblies listed in `plugins[].assemblypath`.

For each assembly that contains `IPlugin` classes:

1. **Assembly** – created or updated by name, content uploaded. It is skipped if no public plugin class has a `[CrmPluginRegistration]` attribute.
2. **Plugin types** – one per public `IPlugin` class that has an attribute.
3. **Steps and images** – one per `[CrmPluginRegistration]` attribute. Existing steps are matched by `Id` if set, otherwise by step name + message.
4. **Custom API** – an attribute with only a message name binds the plugin type to that Custom API. The Custom API must already exist.
5. Everything is added to `plugins[].solution`. Every entry must have a `solution`; otherwise spkl stops before changing anything.

> ⚠️ **spkl deletes what is not in code.** Plugin types that no longer exist in the assembly are deleted with their steps. Steps on a type that have no matching attribute are deleted. Images not in the attribute are deleted. Steps added manually in the Plugin Registration Tool will be removed on the next deploy.

### `workflow`

Registers custom workflow activities (`CodeActivity` classes) from the same `plugins[].assemblypath` entries. Each class needs one `[CrmPluginRegistration(name, friendlyName, description, groupName, isolationMode)]` attribute. Use a profile so plugin and workflow deploys don't scan each other's assemblies.

### `webresources`

Uploads files from `webresources[].root`, adds them to `webresources[].solution` (required) and publishes them. Unchanged files are skipped.

- With a `files` list: only the listed files are deployed.
- With `"autodetect": "yes"`: every file under `root` is deployed, and its path below `root` becomes its unique name. `root\new_\js\account.js` → `new_/js/account.js`.
- `deleteaction` decides what happens to web resources that are in the solution but no longer on disk: `no` (default), `remove` (from the solution) or `delete` (from the environment).

Web resource type is taken from the file extension: `htm/html, js, css, png, gif, jpg/jpeg, ico, xml, xsl/xslt, xap, svg, resx`.

---

## Download / scaffolding

### `download-webresources`

Downloads web resources of a solution to disk. Needs **both**:

- a `solutions` entry (its `solution_uniquename` decides which solution to download from), and
- a `map` in that entry with a `from` starting with `WebResources`, which decides the local folder.

Existing files are kept unless `/o` is passed.

### `get-webresources`

Scans `webresources[].root` (and its sub-folders) for `*.js, *.htm, *.html, *.css, *.xap, *.png, *.jpeg, *.jpg, *.gif, *.ico, *.xml, *.svg, *.resx, *.xsl, *.xslt` files, matches them to web resources in the environment by name, and writes the matches into the `files` list of `spkl.json`. Requires `/s:<prefix>`. A backup of `spkl.json` is created first.

### `instrument`

Reads the registrations from the environment and writes matching `[CrmPluginRegistration]` attributes into your `.cs` files. Use it once to move an existing, manually registered project to spkl. It scans `*.cs` files under `path`, so run it against the plugin project:

```powershell
.\Tooling\spkl.ps1 -Task instrument -Path .\Plugins
```

A `.bak` copy of every changed file is written next to it.

### `earlybound`

Generates early-bound classes with `CrmSvcUtil.exe` for the entities and actions in `earlyboundtypes`. `CrmSvcUtil.exe` comes from the `Microsoft.CrmSdk.CoreTools` package, which the spkl package depends on.

---

## Solutions

All of these use the `solutions` section and `SolutionPackager.exe` from `Microsoft.CrmSdk.CoreTools`.

| Task | What it does |
|---|---|
| `unpack` | Exports `solution_uniquename` from the environment and extracts it into `packagepath`. |
| `unpacksolution` | Extracts the local zip at `solutionpath` into `packagepath`. No connection needed. |
| `pack` | Packs `packagepath` into a zip at `solutionpath`, applying `map`. No connection needed. |
| `import` | `pack`, then imports the zip and publishes all. With `increment_on_import` the last part of the version in `Other\Solution.xml` is increased by one first. |
| `export` | Exports managed and/or unmanaged zips (per `packagetype`) into the spkl.json folder. |

---

## Other

| Task | What it does |
|---|---|
| `whoami` | Prints the organization id and user id you are connected as. |
