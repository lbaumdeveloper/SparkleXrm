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

**Search**: spkl looks for `spkl.json` in the `path` you pass **and every sub-folder**, and runs the task once per file it finds. Files under a `packages\spkl…` folder are ignored. Keep a single `spkl.json` in `Tooling\` and always pass that folder.

### Profiles

Every entry has an optional `profile`: a comma-separated list of names, without spaces.

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
| `assemblypath` | string | – | DLL(s) to register. See [assembly paths](#assembly-paths). |
| `solution` | string | **required** | Unique name of the solution the assembly, types and steps are added to. spkl stops with an error if it is missing, so nothing is deployed to the default solution. |
| `excludePluginSteps` | bool | `false` | Register assembly and types only, no steps. Same as `/e`. |
| `classRegex` | string | see below | `instrument` only: regex that finds plugin and workflow classes in `.cs` files. Change it if your plugins inherit from a base class not named `PluginBase` or `Plugin`. |

### Assembly paths

How `assemblypath` is matched depends on how it starts:

| Starts with | Matched as | Example |
|---|---|---|
| `..`, `**` or `\\` | a glob relative to the spkl.json folder, exactly as written | `..\\Plugins\\bin\\Debug\\Contoso.Plugins.dll` |
| anything else | `**\\` + the value: **any** matching file in any sub-folder of the spkl.json folder | `bin\\Debug\\Contoso.Plugins.dll` |

If the value has no file extension, `\\*.dll` is appended, so a folder means all its DLLs. `System.*.dll`, the Dataverse SDK DLLs and Newtonsoft.Json are always skipped.

Use the exact DLL path starting with `..\\`. It is the only form that cannot pick up a copy of the DLL from another `bin` folder.

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
| `entities` | string | – | Comma-separated logical names: `"account,contact"`. |
| `entityCollection` | string[] | – | Same as `entities`, as an array. Used only if `entities` is empty. |
| `actions` | string | – | Comma-separated custom action / Custom API names. |
| `actionCollection` | string[] | – | Same as `actions`, as an array. |
| `generateOptionsetEnums` | bool | `false` | Generate enums for choice columns. |
| `generateGlobalOptionsets` | bool | `false` | Generate enums for global choices. |
| `generateStateEnums` | bool | `false` | Generate enums for `statecode`. |
| `filename` | string | – | Output file, relative to spkl.json. |
| `classNamespace` | string | – | Namespace of the generated classes. |
| `serviceContextName` | string | – | Name of the generated `OrganizationServiceContext`. |
| `oneTypePerFile` | bool | `false` | Split the output into one file per type, next to `filename`. |

If the section is missing, spkl generates `Entities.cs` for `account,contact` in namespace `Xrm`.

## `solutions`

Used by `unpack`, `unpacksolution`, `pack`, `import`, `export` and `download-webresources`.

| Field | Type | Default | Meaning |
|---|---|---|---|
| `profile` | string | – | See [profiles](#profiles). Note: in this section a space after the comma breaks matching. |
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
