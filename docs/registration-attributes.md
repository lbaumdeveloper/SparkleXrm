# Registration attributes

spkl reads the registration of plugins, Custom APIs and workflow activities from `[CrmPluginRegistration]` attributes on the classes. The code is the single source of truth: what is not in an attribute is removed on the next deploy (see [`plugins`](commands.md#plugins)).

The attribute class comes from `CrmPluginRegistrationAttribute.cs`, shared through `Directory.Build.targets` ([getting started, step 3](getting-started.md#3-share-the-registration-attribute)).

## Plugin step

```csharp
[CrmPluginRegistration(
    "Update",                        // message
    "account",                       // table logical name, or "none" for messages without a table
    StageEnum.PreOperation,          // PreValidation, PreOperation, PostOperation
    ExecutionModeEnum.Synchronous,   // Synchronous, Asynchronous
    "name,address1_line1",           // filtering attributes, "" = all
    "Contoso.Plugins.AccountUpdate: Update of account", // step name, must be unique per message
    1,                               // execution order
    IsolationModeEnum.Sandbox)]      // Sandbox (required for Dataverse online), None
public class AccountUpdate : IPlugin { ... }
```

One attribute = one step. Add several attributes to the same class for several steps.

`MessageNameEnum` can be used instead of the message string: `MessageNameEnum.Update`.

### Optional named properties

| Property | Meaning |
|---|---|
| `Id` | Step id (GUID string). Without it, the step is matched by name + message, so **renaming a step creates a new one and deletes the old one**. Setting `Id` keeps the same step across renames. |
| `Description` | Step description. |
| `UnSecureConfiguration` | Unsecure configuration string passed to the plugin constructor. |
| `DeleteAsyncOperation` | Asynchronous steps: delete the system job when it succeeds. |
| `Image1Type`, `Image1Name`, `Image1Attributes` | First image. Type is `PreImage`, `PostImage` or `Both`. Attributes are comma-separated; empty means all columns. |
| `Image2Type`, `Image2Name`, `Image2Attributes` | Second image. |
| `Server`, `Offline` | Supported deployment. Default is server only. |

Accepted but **not applied** by the current code: `SecureConfiguration`, `Action`.

### Example with images

```csharp
[CrmPluginRegistration("Update", "account", StageEnum.PostOperation, ExecutionModeEnum.Synchronous,
    "name", "Contoso.Plugins.AccountUpdate: Post-Update of account", 1, IsolationModeEnum.Sandbox,
    Id = "b0a10b53-3788-e711-96d0-00155d380101",
    Image1Type = ImageTypeEnum.PreImage, Image1Name = "PreImage", Image1Attributes = "name",
    Image2Type = ImageTypeEnum.PostImage, Image2Name = "PostImage", Image2Attributes = "name")]
```

## Custom API

```csharp
[CrmPluginRegistration("contoso_CalculateDiscount")]
public class CalculateDiscount : IPlugin { ... }
```

The single argument is the Custom API's unique name. spkl sets this plugin type as the Custom API's plugin. It does **not** create the Custom API, its request parameters or response properties; create those in the solution first. If the Custom API is not found, spkl prints a warning and continues.

## Workflow activity

```csharp
[CrmPluginRegistration(
    "CalculateDiscount",         // name
    "Calculate discount",        // friendly name
    "Calculates the discount",   // description
    "Contoso",                   // group shown in the workflow designer
    IsolationModeEnum.Sandbox)]
public sealed class CalculateDiscountActivity : CodeActivity { ... }
```

Only one attribute per activity. Deploy with the `workflow` task.

## Things that are easy to miss

- **Only public classes** are found. spkl looks at the assembly's exported types that implement `IPlugin` (or inherit `CodeActivity`).
- **Classes without an attribute are skipped.** They are not registered as plugin types. An abstract `PluginBase` without an attribute is therefore fine.
- **No attribute at all = nothing happens.** If no class in the assembly has an attribute, the assembly is not registered.
- **Isolation mode** of the assembly is taken from the first attribute found.
- **Step names must be unique per class.** Two attributes with the same step name on one class stop the deploy with an error.
- **Existing projects**: run [`instrument`](commands.md#instrument) once to generate attributes from what is already registered, instead of writing them by hand.
