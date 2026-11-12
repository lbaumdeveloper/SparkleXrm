# Releasing

spkl ships as three NuGet packages with the same version:

| Package | Used by |
|---|---|
| `Spkl.OnPrem` | `Tooling` project: `spkl.exe` |
| `Spkl.OnPrem.Attributes` | `Plugins`, `Actions`, `Workflows`: `CrmPluginRegistrationAttribute.cs` |
| `Spkl.OnPrem.Templates` | `dotnet new spkl-solution` |

They are **never published from GitHub**. GitHub only builds and tests them. Publishing runs on the private GitLab server through `.gitlab-ci.yml`, into that GitLab project's NuGet package registry.

## First-time setup

### 1. Put the repository on GitLab

Create an empty project on the GitLab server, e.g. `tooling/spkl`, and push this repository to it as a second remote:

```powershell
git remote add gitlab https://<gitlab>/tooling/spkl.git
git push gitlab master
```

Repeat `git push gitlab master` (and the tags, see below) whenever GitHub has changes you want to release. Alternatively set up repository mirroring in GitLab, if your GitLab edition offers it.

Make sure the project's package registry is enabled (in the project settings under visibility / project features; the exact menu names depend on the GitLab version).

### 2. Register a Windows runner

The pipeline needs Windows, because spkl targets .NET Framework 4.8.

On a Windows build machine:

1. Install the .NET 8 SDK and the .NET Framework 4.8 Developer Pack.
2. Install GitLab Runner and register it for the project (Settings → CI/CD → Runners → New project runner) with the **shell** executor and a tag, e.g. `windows`.
3. In the runner's `config.toml`, set `shell = "pwsh"` (or `"powershell"`) for this runner.

If the tag is not `windows`, set the CI/CD variable `SPKL_RUNNER_TAG` (Settings → CI/CD → Variables) to it.

Optional: `SPKL_FEED_URL` pushes to another GitLab NuGet registry instead of the project's own, e.g. a group-level project. The default is `<gitlab>/api/v4/projects/<project id>/packages/nuget/index.json`, and the job pushes with its own job token, so no secret is needed.

### 3. Publish the first version

The repository is at version `1.0.0` (`<Version>` in `Directory.Build.props`).

```powershell
git tag v1.0.0
git push gitlab v1.0.0
```

The tag pipeline:

1. `build`: builds the test projects, runs the unit tests and runs `build/Pack.ps1`, which packs the three packages.
2. `publish`: checks that the tag equals `v<Version>`, then pushes the packages to the registry. Pushing a version that already exists is skipped.

Check the project's package registry (**Deploy → Package registry** in recent GitLab versions): `Spkl.OnPrem`, `Spkl.OnPrem.Attributes` and `Spkl.OnPrem.Templates` should be listed with `1.0.0`.

### 4. Set up each developer machine

Every developer adds the registry once, with a GitLab personal access token that has the `read_api` scope:

```powershell
dotnet nuget add source "https://<gitlab>/api/v4/projects/<project id>/packages/nuget/index.json" `
  --name spkl --username <gitlab user> --password <token>
```

Without `--store-password-in-clear-text`, `dotnet nuget add source` stores the password encrypted, which only works on Windows. That's fine here, because the team is on Windows.

Then install the template:

```powershell
dotnet new install Spkl.OnPrem.Templates
```

Visual Studio uses the same NuGet sources, so restoring `Tooling` and the plugin projects works there too.

## Releasing an update

### 1. Raise the version

Set the new version in **all** of these places. `build/Pack.ps1` stops the build if they differ:

| File | What |
|---|---|
| `Directory.Build.props` | `<Version>` |
| `templates/solution/Tooling/Tooling.csproj` | `Spkl.OnPrem` version |
| `templates/solution/Plugins/Contoso.Plugins.csproj` | `Spkl.OnPrem.Attributes` version |
| `templates/solution/Actions/Contoso.Actions.csproj` | `Spkl.OnPrem.Attributes` version |
| `templates/solution/Workflows/Contoso.Workflows.csproj` | `Spkl.OnPrem.Attributes` version |

Use [semantic versioning](https://semver.org): patch for fixes (`1.0.1`), minor for new features (`1.1.0`), major for breaking changes such as removed attribute properties or spkl.json options (`2.0.0`).

Merge the change on GitHub as usual. GitHub CI builds a solution from the template against the new packages, so a mismatch shows up there first.

### 2. Tag and push to GitLab

```powershell
git checkout master
git pull
git tag v1.0.1
git push gitlab master v1.0.1
```

A version can be published only once. To fix a broken release, raise the version again; don't move the tag.

### 3. Update existing solutions

The template only affects new solutions. In an existing solution:

1. Raise `Spkl.OnPrem` in `Tooling\Tooling.csproj` and `Spkl.OnPrem.Attributes` in the plugin, action and workflow projects, e.g. with **Manage NuGet Packages for Solution → Updates** in Visual Studio.
2. Build. `spkl.ps1` always runs the highest restored `spkl.exe` version.
3. Files that came from the template, such as `Tooling\spkl.ps1`, `.vscode\tasks.json` or `Directory.Build.props`, are not updated by the packages. Check `git diff v<old>..v<new> -- templates/solution` and copy changed files by hand.

Developers update the template itself with:

```powershell
dotnet new update
```

## Packing without GitLab

```powershell
.\build\Pack.ps1 -OutputDirectory .\artifacts
```

This writes the three `.nupkg` files to `artifacts\`, e.g. to test a change locally with `dotnet nuget add source .\artifacts --name local`.
