using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace ImageClassifier.Windows.Main;

public partial class MainWindow : Window
{
    public MainWindow()
        : base()
    {
        this.InitializeComponent();

#if DEBUG
        this.AttachDevTools();
#endif

        this.Closed += new EventHandler((_, _) => this.OnClosed());
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (this.DataContext is MainWindowModelBase windowModel)
        {
            if (e.Key == Key.Up || e.Key == Key.K)
            {
                windowModel.UndoCommand.Execute();
            }
            else if (e.Key == Key.Right || e.Key == Key.L)
            {
                windowModel.RightCommand.Execute();
            }
            else if (e.Key == Key.Left || e.Key == Key.H)
            {
                windowModel.LeftCommand.Execute();
            }
            else if (e.Key == Key.Down || e.Key == Key.J)
            {
                windowModel.DownCommand.Execute();
            }
        }
    }

    private async void OnClosed()
    {
        if (this.DataContext is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }
}
