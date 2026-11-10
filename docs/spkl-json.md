# spkl.json reference

`spkl.json` has five optional sections. Each section is a list, so you can have several entries (for example one per assembly).

```json
{
  "plugins": [ ... ],
  "webresources": [ ... ],
  "earlyboundtypes": [ ... ],
  "solutions": [ ... ]
}
```

## Rules that apply everywhere

**Paths** are relative to the folder that contains `spkl.json`. Use `\\` in JSON strings: `"..\\Plugins\\bin\\Debug\\Contoso.Plugins.dll"`.

**Comments** (`/* ... */`) are allowed; spkl uses Json.NET, which accepts them.

**Location**: spkl loads exactly one `spkl.json`: the one in the `path` you pass, or the file itself if `path` ends in `spkl.json`. Sub-folders are not searched.

**Editor support**: add `"$schema": "https://raw.githubusercontent.com/lbaumdeveloper/SparkleXrm/master/docs/spkl.schema.json"` as the first property to get completion, descriptions and validation in VS Code and Visual Studio. The [template](../templates/solution/Tooling/spkl.json) already has it.

### Profiles

Every entry has an optional `profile`: a comma-separated list of names. Spaces are ignored.

| You run | Entries used |
|---|---|
| no `/p:` | entries with no `profile`, an empty one, or one that contains `default` |
| `/p:actions` | only entries whose `profile` contains `actions` |
| `/p:default` | same as no `/p:` |

So `"profile": "default,actions"` runs both in a normal deploy and with `/p:actions`.

---

## `plugins`

Used by `plugins`, `workflow` and `instrument`.

| Field | Type | Default | Meaning |
|---|---|---|---|
| `profile` | string | – | See [profiles](#profiles). |
| `assemblypath` | string | **required** | DLL(s) to register. See [assembly paths](#assembly-paths). |
| `solution` | string | **required** | Unique name of the solution the assembly, types and steps are added to. spkl stops with an error if it is missing, so nothing is deployed to the default solution. |
| `excludePluginSteps` | bool | `false` | Register assembly and types only, no steps. Same as `/e`. |
| `classRegex` | string | see below | `instrument` only: regex that finds plugin and workflow classes in `.cs` files. Change it if your plugins inherit from a base class not named `PluginBase` or `Plugin`. |

### Assembly paths

`assemblypath` is a glob relative to the spkl.json folder, matched exactly as written:

| Value | Matches |
|---|---|
| `..\\Plugins\\bin\\Debug\\Contoso.Plugins.dll` | that one file |
| `..\\Plugins\\bin\\Debug` | every `*.dll` in that folder (no extension → `\\*.dll` is appended) |
| `..\\Plugins\\bin\\**\\Contoso.Plugins.dll` | the file in any sub-folder of `bin`, e.g. both `Debug` and `Release` |

`System.*.dll`, the Dataverse SDK DLLs and Newtonsoft.Json are always skipped. Prefer the exact DLL path, so dependencies and other build configurations are not picked up.

Default `classRegex`:

```
((public( sealed)? class (?'class'[\w]*)[\W]*?)((?'plugin':[\W]*?((IPlugin)|(PluginBase)|(Plugin)))|(?'wf':[\W]*?CodeActivity)))
```

## `webresources`

Used by `webresources`, `download-webresources` and `get-webresources`.

| Field | Type | Default | Meaning |
|---|---|---|---|
| `profile` | string | – | See [profiles](#profiles). |
| `root` | string | spkl.json folder | Folder the web resource files live in. |
| `solution` | string | **required** | Unique name of the solution the web resources are added to. spkl stops with an error if it is missing. |
| `autodetect` | `yes` / `no` | `no` | Deploy every file under `root`; the path below `root` becomes the unique name. |
| `deleteaction` | `no` / `remove` / `delete` | `no` | With `autodetect`: what to do with web resources in `solution` that no longer exist on disk. |
| `files` | list | – | Files to deploy when `autodetect` is off. |

`files` entries:

| Field | Required | Meaning |
|---|---|---|
| `file` | yes | Path relative to `root`. |
| `uniquename` | no | Name in Dataverse. Defaults to `file` with `/` separators. Must include the publisher prefix, e.g. `new_/js/account.js`. |
| `displayname` | no | Defaults to `uniquename`. |
| `description` | no | |
| `languagecode` | no | LCID, e.g. `1031`. |

Recommended: use `autodetect` and organise `root` the way the names should look:

```
Webresources/
└── new_/
    ├── js/account.js        → new_/js/account.js
    └── html/dialog.html     → new_/html/dialog.html
```

## `earlyboundtypes`

Used by `earlybound`.

| Field | Type | Default | Meaning |
|---|---|---|---|
| `profile` | string | – | See [profiles](#profiles). |
| `entities` | string[] | – | Table logical names: `["account", "contact"]`. |
| `actions` | string[] | – | Custom action / Custom API unique names: `["contoso_CalculateDiscount"]`. |
| `generateOptionsetEnums` | bool | `false` | Generate enums for choice columns. |
| `generateGlobalOptionsets` | bool | `false` | Generate enums for global choices. |
| `generateStateEnums` | bool | `false` | Generate enums for `statecode`. |
| `filename` | string | – | Output file, relative to spkl.json. |
| `classNamespace` | string | – | Namespace of the generated classes. |
| `serviceContextName` | string | – | Name of the generated `OrganizationServiceContext`. |
| `oneTypePerFile` | bool | `false` | Split the output into one file per type, next to `filename`. |

If the section is missing, spkl generates `Entities.cs` for `account` and `contact` in namespace `Xrm`.

Older files with a comma-separated string (`"entities": "account,contact"`) are still read. `entityCollection` and `actionCollection` are no longer supported; rename them to `entities` and `actions`.

## `solutions`

Used by `unpack`, `unpacksolution`, `pack`, `import`, `export` and `download-webresources`.

| Field | Type | Default | Meaning |
|---|---|---|---|
| `profile` | string | – | See [profiles](#profiles). |
| `solution_uniquename` | string | – | Unique name of the solution in Dataverse. |
| `packagepath` | string | – | Folder the unpacked solution lives in. |
| `solutionpath` | string | `solution.zip` | Zip file path. `{0}`–`{3}` are replaced by the four version parts: `"Solutions\\Contoso_{0}_{1}_{2}_{3}.zip"`. |
| `packagetype` | enum | `unmanaged` | `unmanaged`, `managed`, `both_unmanaged_import`, `both_managed_import`. The `both_*` values unpack/pack both; the suffix decides which zip `import` uses. |
| `increment_on_import` | bool | `false` | `import` increases the last version part in `Other\Solution.xml` first. |
| `map` | list | – | SolutionPackager mappings, see below. |
| `clobber` | bool | `true` | Unpack: overwrite read-only files (`/clobber`). |
| `allowwrite` | bool | `true` | Unpack: write files (`/allowWrite`). |
| `allowdelete` | bool | `true` | Unpack: delete files no longer in the solution (`/allowDelete`). |

`map` entries become a SolutionPackager [mapping file](https://learn.microsoft.com/power-platform/alm/solution-packager-tool#use-the-map-command-argument):

| Field | Meaning |
|---|---|
| `map` | `file` → `<FileToFile>`, `path` → `<FileToPath>`, `folder` → `<Folder>` |
| `from` | Path inside the solution package, e.g. `WebResources\\**\\*.*` |
| `to` | Path on disk the file is taken from when packing |

`download-webresources` needs a `map` whose `from` starts with `WebResources` to know where to save files.
