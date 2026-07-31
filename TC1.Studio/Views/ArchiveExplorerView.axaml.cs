using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TC1.Studio.ViewModels;

namespace TC1.Studio.Views;

public partial class ArchiveExplorerView : UserControl
{
    private int _lastOpenIndex = -1;

    public ArchiveExplorerView()
    {
        InitializeComponent();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ArchiveExplorerViewModel vm) return;
        if (vm.SelectedEntry == null || vm.SelectedEntry.Index == _lastOpenIndex) return;
        _lastOpenIndex = vm.SelectedEntry.Index;
        vm.OpenSelectedEntryCommand.Execute(null);
    }

    private async void OnRenameEntry(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ArchiveExplorerViewModel vm) return;

        var dialog = new InputDialog();
        dialog.Topmost = true;
        var result = await dialog.ShowDialog<bool?>((Window)VisualRoot);
        if (result == true && !string.IsNullOrEmpty(dialog.Result))
            vm.RenameEntry(dialog.Result);
    }
}
