# plugin api

the `IEditorPlugin` interface lets you replace the generic property grid with a custom editor for specific node types. basically if you know what a certain node does (like weather params), you can make a proper UI for it instead of editing raw numbers.

## interface

```csharp
namespace TC1.Studio.Plugins;

public interface IEditorPlugin
{
    string Name { get; }
    bool CanHandle(Node node);
    Control CreateEditor(Node node);
}
```

| member | description |
|---|---|
| `Name` | display name for the plugin |
| `CanHandle(Node)` | return `true` if this plugin knows how to edit this node |
| `CreateEditor(Node)` | return an avalonia `Control` as the custom editor ui |

## contract

- plugins are **read-only inspectors by default** — undo/redo and save are bypassed. call `BinaryObjectService.MarkModified()` if you actually change data.
- `CreateEditor` gets called when a user selects a node whose root hash matches the plugin. the returned control replaces the property grid.
- don't modify the node tree outside of your editing scope. clone first if you need to.

## default plugin

the built-in `PropertyGridPlugin` handles everything by default with the generic field editor (type detection, text boxes, apply button). your custom plugin only kicks in for specific node types.

## dynamic discovery (future)

eventually plugins will be auto-discovered by scanning a `Plugins/` directory for assemblies implementing `IEditorPlugin`. right now they're registered manually in code.

## example: weather plugin concept

```csharp
public class WeatherPlugin : IEditorPlugin
{
    public string Name => "Weather Editor";

    public bool CanHandle(Node node)
    {
        return node.NameHash == 0x2002CFD9; // RainPrecipitationAmount
    }

    public Control CreateEditor(Node node)
    {
        // return a custom avalonia UserControl with sliders, toggles, etc
        var panel = new StackPanel();
        // ... bind to node fields ...
        return panel;
    }
}
```

## registration

plugins are registered in `MainWindowViewModel`:

```csharp
private readonly List<IEditorPlugin> _plugins = new()
{
    new PropertyGridPlugin(boService, undo, hashes),
    // new WeatherPlugin(),
};
```
