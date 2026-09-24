# Migrating VL node exposure to C#

Use this guide when a task asks to replace legacy VL-authored process nodes, node factories, or type forwards with the C# import system. It describes the migration path demonstrated by `VL.Video`; it is not a requirement to rewrite unrelated packages.

## Start with the existing contract

Before editing, inspect the package's C# project, `.vl` document, initialization code, type-forward definitions, and any help patches that consume the nodes. Record the externally visible contract:

- Node names, categories, summaries, remarks, tags, pin names, order, types, defaults, and optional-pin visibility.
- Return values and status outputs, including any conversions performed by VL patches (for example, `Path` to `string`).
- State creation, update behavior, disposal, enum defaults, and whether a type is intentionally hidden or advanced.

Check `git status` first. Preserve existing user changes, including recent `[Smell]` annotations; do not treat dirty files as disposable migration scaffolding.

## Import public C# types

For a package whose public API lives under a dedicated namespace, add an assembly-level import in `src/Properties/AssemblyInfo.cs`:

```csharp
[assembly: ImportAsIs(Namespace = "VL.Video", Category = "Video")]
```

`Namespace` is the prefix stripped when constructing VL categories; `Category` supplies the root category. Use the narrowest namespace that imports the intended public types.

### Choose import granularity

The main assembly-level selectors in `VL.Core.Import` are:

| Attribute | Use |
| --- | --- |
| `ImportAsIs` | Import public types and members matching an optional namespace prefix; one selector per assembly. `Namespace` and `Category` set the import scope and category mapping. |
| `ImportNamespace` | Import public types and members under a named namespace; multiple selectors are allowed. The namespace is stripped when deriving categories, with optional `Category` as the root. |
| `ImportType` | Import one exact type and its public members; multiple selectors are allowed. Supports `Name`, `Category`, and `NamespacePrefixToStrip`. |
| `IncludeForeign` | Compatibility fallback: expose types not imported by `ImportAsIs` as foreign VL types. Use only when that fallback is intentional. |
| `ImportAttribute` | Abstract base for the selectors above; do not apply it directly. |

Prefer the narrowest selector that exposes the intended API. For example, replace a VL `<ForwardRecordDefinition>` or other single-type forward with an assembly-level `[ImportType(typeof(MyNamespace.MyType), Category = "...")]` when the actual CLR type is available. Use `[ImportNamespace(...)]` for one or more namespaces; retain `ImportAsIs` when broad namespace import is desired. Verify that dependent pins resolve to the intended CLR type after removing a forward.

Other `VL.Core.Import` attributes control how imported symbols appear or form nodes:

| Attribute | Use |
| --- | --- |
| `ProcessNode` | Define a process from an attributed class. Supports `Name`, `Category`, `Summary`, `Remarks`, `Tags`, fragment selection, `HasStateOutput`, and `StateOutputNotVisibleByDefault`. |
| `ProcessNodeFactory` / `ProcessNodeFactory` base class | Generate process-node definitions from a factory, including path-specific definitions. `ImportClass` controls whether the factory class/members are also imported. Retain this for genuinely dynamic node sets. |
| `Fragment` / `FragmentSelection` | Select constructor, method, and property fragments; fragments support order, hidden, and default-moment settings. |
| `Pin` | Control pin name, visibility, exposition, and supported pin-group settings. `IsState` is internal-only. |
| `Name` / `Category` | Override the name or category of an individual symbol. |
| `SkipCategory` | Keep a static helper/extension container out of the category browser. |
| `Smell` | Mark symbol presentation, such as hidden or advanced. |

`VL.Video/src/Properties/AssemblyInfo.cs` is the `ImportAsIs` example. `ImportAsIs`, `ImportNamespace`, and `ImportType` make public C# types/members directly available; add `[ProcessNode]` where a type should define a VL process node.

## Define process nodes with attributes

Use `[ProcessNode]` metadata for the node's public name/category, summary, remarks, tags, and state-output behavior. `HasStateOutput = true` exposes the process state as an output and also makes the class members available as individual nodes; use `StateOutputNotVisibleByDefault` when that state output should start hidden.

`FragmentSelection.Implicit` selects public methods and properties by default; `[Fragment(IsHidden = true)]` can exclude an implicit fragment. `FragmentSelection.Explicit` selects only members marked `[Fragment]`. Use `Fragment.Order` when declaration order is insufficient and `Fragment.IsDefault` only when a fragment must target the default moment.

The constructor is required for a process definition. With explicit fragment selection, mark it `[Fragment]` too; an implicit, unmarked parameterless constructor is not selected. Keep the constructor public. If it accepts `NodeContext` only for internal node behavior, mark that pin hidden with `[Pin(Visibility = PinVisibility.Hidden)]`.

Use a dedicated adapter class when the VL node is a composition or adapts inputs/outputs. Annotate an existing runtime class directly when that class already is the node's state and its behavior maps cleanly. The disk `VideoPlayer` adapter and URL `VideoPlayer` node in `VL.Video` demonstrate both approaches.

A common shape is:

```csharp
[ProcessNode(Name = "Example", Category = "Video",
	FragmentSelection = FragmentSelection.Explicit,
	Summary = "Describe the node")]
public sealed class ExampleNode
{
	[Fragment]
	public ExampleNode() { }

	[Fragment]
	[return: Pin(Name = "Output")]
	public ExampleNode Update(
		[Pin(Name = "Rate"), DefaultValue(1f)] float rate,
		[Pin(Name = "Playing")] out bool playing)
	{
		// Update state and assign every out parameter.
		playing = false;
		return this;
	}
}
```

Document every VL-facing type, process node, and pin. Add XML `<summary>` documentation to visible types/nodes and public members, `<param>` documentation for every method input and `out` pin, and `<returns>` documentation for each return-value pin. These comments supply VL summaries and pin help; the `[ProcessNode]` `Summary` describes the node but does not replace per-pin documentation. Keep each `<param name>` identical to its C# parameter name. Use `[return: Pin(Name = "Output")]` and named `out` pins to preserve visible output names; add `[Pin(Name = "...")]` wherever automatic C#-to-pin naming could change legacy spelling, spacing, or acronyms.

## Preserve defaults and optional pins

Do not rely on guessed defaults. Carry over each old default explicitly using C# optional parameter defaults or `System.ComponentModel.DefaultValueAttribute`; for non-constant structs, use the type/string form, for example `[DefaultValue(typeof(Int2), "1920, 1080")]`.

Use `[Pin(Visibility = PinVisibility.Optional)]` for pins that were optional in VL. A nullable type and an optional-visible pin are separate decisions; preserve both the input type and its visibility. For nullable or `Optional<T>` inputs, confirm the disconnected/default behavior matches the old node. Add `out` parameters for outputs that were previously assembled from getter nodes.

If the old node's pins were generated from an enum, compare the complete enum values with the existing pin set before replacing the dynamic schema with a fixed method signature. Keep the original order and display names. Only use enum values as array indexes when their values are verified to be contiguous and zero-based; `VL.Video`'s camera and video property enums currently satisfy that requirement. For hot update paths, use direct indexed writes such as `Properties[(int)property]` rather than a per-update LINQ search by name.

## Check whether the node set is static

Replacing a node-factory-based system with fixed `[ProcessNode]` declarations works only when the node set is static and fully known at compile time: each node can then be represented by an attributed C# type. If a factory creates different nodes according to runtime data or the requested path, do not silently freeze that set or drop nodes. Keep or migrate that behavior to `ProcessNodeFactory`/`ProcessNodeFactoryAttribute`, whose factory can supply nodes for a path.

## Preserve type discoverability with `Smell`

Use `[Smell(SymbolSmell.Hidden)]` for implementation-only types that should not be shown as ordinary VL symbols, and `[Smell(SymbolSmell.Advanced)]` for public types that should remain available but be marked advanced. Use the narrowest appropriate symbol (type/member) and do not hide a type required by a visible node's pins.

Current examples in `VL.Video`:

- `VideoCaptureDeviceEnum` is hidden; it supplies the dynamic enum implementation.
- `VideoCaptureDeviceEnumEntry`, `ErrorState`, `NetworkState`, and `ReadyState` are advanced public types.

`SymbolSmell` is a flags enum; do not infer visibility policy from C# accessibility alone. The annotation controls VL symbol presentation, not CLR access.

## Reduce the `.vl` document to the assembly forward

Once all functionality is represented by imported C# types/process nodes, remove obsolete node-factory registrations, patched node definitions, VL type-forward records, and their `NodeFactoryDependency` entries. If the package `.vl` file is only the import forward, retain its document metadata and one dependency to the packaged assembly:

```xml
<PlatformDependency Location="./lib/net10.0/YourPackage.dll" IsForward="true" />
```

Use the actual package DLL path and preserve the document's existing IDs/version metadata. Keep `NugetDependency` or other dependencies only when remaining VL-authored content requires them. Check the package nuspec still includes both the DLL and `.vl` document. Help patches may continue to refer to the same node names; update them only if the public contract intentionally changes.

## Verification checklist

1. Build the target C# project (for example, `dotnet build VL.Video\src\VL.Video.csproj`) and run existing targeted tests if present.
2. Confirm every intended `[ProcessNode]` has a selected public constructor and the intended `[Fragment]` members.
3. Compare generated node names, categories, pins, defaults, optionality, output types, descriptions, and `Smell` metadata against the original contract; ensure every VL-facing type/node and every pin has XML documentation.
4. Search the package for obsolete `RegisterNodeFactory`, `NewNodeDescription`, forward-record, and `NodeFactoryDependency` references.
5. Validate the final `.vl` XML and ensure it forwards to the packaged C# DLL.

Do not retain a factory solely to imitate a static pin list. Conversely, if a node's pin schema is truly runtime-dependent and cannot be represented by fixed C# process-node members, document that limitation and choose an appropriate dynamic mechanism rather than silently dropping pins.
