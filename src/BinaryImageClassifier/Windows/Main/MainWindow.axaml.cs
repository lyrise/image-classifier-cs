using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace BinaryImageClassifier.Windows.Main;

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
        if (this.DataContext is IMainWindowModel windowModel)
        {
            if (e.Key.HasFlag(Key.Right))
            {
                windowModel.RightCommand.Execute();
            }
            else if (e.Key.HasFlag(Key.Left))
            {
                windowModel.LeftCommand.Execute();
            }
            else if (e.Key.HasFlag(Key.Up))
            {
                windowModel.UndoCommand.Execute();
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
