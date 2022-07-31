using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using BinaryImageClassifier.Internal;
using BinaryImageClassifier.Windows.Main;

namespace BinaryImageClassifier;

public class App : Application
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private FileStream? _lockFileStream;

    public override void Initialize()
    {
        if (!this.IsDesignMode)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((_, e) => _logger.Error(e));
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

    public MainWindow? MainWindow
    {
        get => this.ApplicationLifetime?.MainWindow as MainWindow;
        set
        {
            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifeTime)
            {
                lifeTime.MainWindow = value;
            }
        }
    }

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
            SetLogsDirectory("./logs");

            _lockFileStream = new FileStream("./lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);

            _logger.Info("Starting...");
            _logger.Info("AssemblyInformationalVersion: {0}", Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

            this.MainWindow = new MainWindow();

            await Bootstrapper.Instance.BuildAsync();

            var serviceProvider = Bootstrapper.Instance.GetServiceProvider();
            var viewModel = serviceProvider.GetRequiredService<MainWindowModel>();

            this.MainWindow!.DataContext = viewModel;
        }
        catch (Exception e)
        {
            _logger.Error(e, "Unexpected Exception");
        }
    }

    private void SetLogsDirectory(string logsDirectoryPath)
    {
        var target = (NLog.Targets.FileTarget)NLog.LogManager.Configuration.FindTargetByName("log_file");
        target.FileName = $"{Path.GetFullPath(logsDirectoryPath)}/${{date:format=yyyy-MM-dd}}.log";
        target.ArchiveFileName = $"{Path.GetFullPath(logsDirectoryPath)}/logs/archive.{{#}}.log";
        NLog.LogManager.ReconfigExistingLoggers();
    }

    private void ChangeLogLevel(NLog.LogLevel minLevel)
    {
        _logger.Debug("Log level changed: {0}", minLevel);

        var rootLoggingRule = NLog.LogManager.Configuration.LoggingRules.First(n => n.NameMatches("*"));
        rootLoggingRule.EnableLoggingForLevels(minLevel, NLog.LogLevel.Fatal);
        NLog.LogManager.ReconfigExistingLoggers();
    }
}
