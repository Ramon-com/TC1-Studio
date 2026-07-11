using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TC1.Studio.ViewModels;
using TC1.Studio.Views;

namespace TC1.Studio.Views;

public partial class ObjectTreeView : UserControl
{
    private object _contextItem;

    public ObjectTreeView()
    {
        InitializeComponent();
    }

    private void OnItemPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (sender is StackPanel panel && panel.DataContext is ObjectNodeViewModel node)
        {
            _contextItem = node;
            if (DataContext is ObjectTreeViewModel vm)
                vm.SelectNodeCommand.Execute(node);
        }
    }

    private async void OnRenameNode(object sender, RoutedEventArgs e)
    {
        if (_contextItem is not ObjectNodeViewModel node) return;
        if (DataContext is not ObjectTreeViewModel vm) return;

        var dialog = new InputDialog();
        dialog.Topmost = true;
        var result = await dialog.ShowDialog<bool?>((Window)VisualRoot);
        if (result == true && !string.IsNullOrEmpty(dialog.Result))
        {
            vm.RenameHash($"0x{node.NameHash:X8}", dialog.Result);
        }
    }
}
