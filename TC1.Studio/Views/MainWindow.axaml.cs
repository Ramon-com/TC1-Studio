using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using TC1.Studio.ViewModels;

namespace TC1.Studio.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, System.EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.RequestOpenFile = async () =>
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Open TC1 Archive",
                    AllowMultiple = false,
                };
                dialog.Filters.Add(new FileDialogFilter
                {
                    Name = "FAT files",
                    Extensions = { "fat" }
                });

                var result = await dialog.ShowAsync(this);
                return result?.Length > 0 ? result[0] : null;
            };

            vm.RequestSaveFile = async () =>
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Save TC1 Archive",
                    DefaultExtension = "fat",
                };
                dialog.Filters.Add(new FileDialogFilter
                {
                    Name = "FAT files",
                    Extensions = { "fat" }
                });

                return await dialog.ShowAsync(this);
            };
        }
    }
}
