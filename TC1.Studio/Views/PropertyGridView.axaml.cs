using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TC1.Studio.ViewModels;
using TC1.Studio.Views;

namespace TC1.Studio.Views;

public partial class PropertyGridView : UserControl
{
    public PropertyGridView()
    {
        InitializeComponent();
    }

    private void OnApplyClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is FieldViewModel field)
        {
            if (DataContext is PropertyGridViewModel vm)
                vm.ApplyFieldEditCommand.Execute(field);
        }
    }

    private async void OnRenameField(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi)
        {
            var field = mi.DataContext as FieldViewModel;
            if (field == null && mi.Parent is ContextMenu cm)
                field = cm.PlacementTarget?.DataContext as FieldViewModel;

            if (field == null || DataContext is not PropertyGridViewModel vm) return;

            var dialog = new InputDialog();
            dialog.Topmost = true;
            var result = await dialog.ShowDialog<bool?>((Window)VisualRoot);
            if (result == true && !string.IsNullOrEmpty(dialog.Result))
            {
                vm.RenameFieldHash(field, dialog.Result);
            }
        }
    }
}
