using Avalonia.Controls;
using TC1.Core.BinaryObject;

namespace TC1.Studio.Plugins;

public interface IEditorPlugin
{
    string Name { get; }
    bool CanHandle(Node node);
    Control CreateEditor(Node node);
}
