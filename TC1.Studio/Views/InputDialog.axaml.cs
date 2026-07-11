using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TC1.Studio.Views;

public partial class InputDialog : Window
{
    public string Result { get; private set; }

    public InputDialog()
    {
        InitializeComponent();
        NameBox.Focus();
        NameBox.KeyDown += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter) OnOk(null, null);
            if (e.Key == Avalonia.Input.Key.Escape) OnCancel(null, null);
        };
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        var text = NameBox.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            ErrorText.Text = "Name cannot be empty.";
            ErrorText.IsVisible = true;
            return;
        }
        if (!char.IsLetter(text[0]) && text[0] != '_')
        {
            ErrorText.Text = "Name must start with a letter or underscore.";
            ErrorText.IsVisible = true;
            return;
        }
        for (int i = 1; i < text.Length; i++)
        {
            if (!char.IsLetterOrDigit(text[i]) && text[i] != '_')
            {
                ErrorText.Text = $"Invalid character '{text[i]}'. Use letters, digits, or underscores.";
                ErrorText.IsVisible = true;
                return;
            }
        }
        Result = text;
        Close(true);
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
