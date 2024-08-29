using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ImageClassifier.Shared;
using ImageClassifier.Windows.Main;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageClassifier;

public class App : Application
{
    private readonly ILogger _logger;

    private FileStream? _lockFileStream;

    public App()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .AddDebug();
        });
        _logger = loggerFactory.CreateLogger<App>();
    }

    public override void Initialize()
    {
        if (!this.IsDesignMode)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((_, e) => _logger.LogError(e.ExceptionObject as Exception, "Unhandled Exception"));
            this.ApplicationLifetime!.Exit += (_, _) => this.Exit();
        }

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        this.Startup();

        base.OnFrameworkInitializationCompleted();
    }

    public static new App? Current => Application.Current as App;

    public new IClassicDesktopStyleApplicationLifetime? ApplicationLifetime => base.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

    public MainWindow? MainWindow => this.ApplicationLifetime?.MainWindow as MainWindow;

    public bool IsDesignMode
    {
        get
        {
#if DESIGN
            return true;
#else
            return Design.IsDesignMode;
#endif
        }
    }

    private async void Startup()
    {
        try
        {
            _lockFileStream = new FileStream("./lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);

            _logger.LogInformation("Starting...");
            _logger.LogInformation("AssemblyInformationalVersion: {0}", Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

            var mainWindow = new MainWindow();

            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                lifetime.MainWindow = mainWindow;
            }

            await Bootstrapper.Instance.BuildAsync();

            var serviceProvider = Bootstrapper.Instance.GetServiceProvider();
            mainWindow.DataContext = serviceProvider.GetRequiredService<MainWindowModel>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected Exception");
        }
    }

    private async void Exit()
    {
        await Bootstrapper.Instance.DisposeAsync();

        _logger.LogInformation("Stopping...");

        _lockFileStream?.Dispose();
    }
}
