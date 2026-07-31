using System;
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

    private async void OnAddField(object sender, RoutedEventArgs e)
    {
        if (_contextItem is not ObjectNodeViewModel node) return;
        if (VisualRoot is not Window win || win.DataContext is not MainWindowViewModel mw) return;

        var hashDialog = new InputDialog
        {
            Title = "Add Field",
            PromptText = "Field hash in hex (up to 8 digits, e.g. A1B2C3D4):",
            AllowFreeText = true,
        };
        hashDialog.Topmost = true;
        var hashResult = await hashDialog.ShowDialog<bool?>((Window)VisualRoot);
        if (hashResult != true || string.IsNullOrEmpty(hashDialog.Result)) return;
        if (!uint.TryParse(hashDialog.Result.Trim(), System.Globalization.NumberStyles.HexNumber, null, out var hash)) return;

        var valueDialog = new InputDialog
        {
            Title = "Add Field",
            PromptText = "Value in hex bytes (e.g. 00 00 80 3F):",
            AllowFreeText = true,
        };
        valueDialog.Topmost = true;
        var valueResult = await valueDialog.ShowDialog<bool?>((Window)VisualRoot);
        if (valueResult != true || string.IsNullOrEmpty(valueDialog.Result)) return;

        byte[] value;
        try
        {
            value = PropertyGridViewModel.ParseHex(valueDialog.Result);
        }
        catch (Exception ex)
        {
            mw.StatusText = $"Add field error: {ex.Message}";
            return;
        }

        mw.PropertyGrid.AddFieldToNode(node, hash, value);
        mw.StatusText = $"Added field 0x{hash:X8} to {node.ResolvedName}";
    }
}
