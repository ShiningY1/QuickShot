using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using QuickShot.Services;

namespace QuickShot.Windows;

// Scrollable, branded application messages; OS file/folder pickers remain native.
public sealed class MessageWindow : Window
{
    private MessageWindow(string text, string title)
    {
        Title = title;
        Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/QuickShot.ico"));
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.CanResizeWithGrip;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        WindowLayout.Apply(this, 36);
        var grid = new Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.Children.Add(new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }
        });
        var close = new System.Windows.Controls.Button { Content = "确定", IsDefault = true, IsCancel = true,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new Thickness(0,12,0,0) };
        close.Click += (_, _) => Close();
        Grid.SetRow(close, 1);
        grid.Children.Add(close);
        Content = grid;
    }
    public static void Show(string text, string title, Window? owner = null)
    {
        var window = new MessageWindow(text, title);
        owner ??= System.Windows.Application.Current.Windows.Cast<Window>().FirstOrDefault(w => w.IsActive);
        if (owner is { IsVisible: true }) window.Owner = owner;
        else window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        window.ShowDialog();
    }
}
