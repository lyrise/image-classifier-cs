using System.Diagnostics;
using CommandLine;
using FluentAvalonia.Core;
using ImageClassifier.Windows.Main;
using Microsoft.Extensions.DependencyInjection;

namespace ImageClassifier.Shared;

public partial class Bootstrapper : IAsyncDisposable
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private ServiceProvider? _serviceProvider;

    public static Bootstrapper Instance { get; } = new Bootstrapper();

    private Bootstrapper()
    {
    }

    public class Options
    {
        [Option('c', "config")]
        public string ConfigPath { get; set; } = "config.json";

        [Option('v', "verbose")]
        public bool Verbose { get; set; } = false;
    }

    public async ValueTask BuildAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var parsedResult = CommandLine.Parser.Default.ParseArguments<Options>(Environment.GetCommandLineArgs());
            var config = await AppConfig.LoadAsync(parsedResult.Value.ConfigPath);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(config);
            serviceCollection.AddTransient<MainWindowModel>();

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }
        catch (OperationCanceledException e)
        {
            _logger.Debug(e);

            throw;
        }
        catch (Exception e)
        {
            _logger.Error(e);

            throw;
        }
    }

    public ServiceProvider GetServiceProvider()
    {
        return _serviceProvider ?? throw new NullReferenceException();
    }

    public async ValueTask DisposeAsync()
    {
    }
}
