using System.Diagnostics;
using CommandLine;
using FluentAvalonia.Core;
using ImageClassifier.Windows.Main;
using Microsoft.Extensions.DependencyInjection;

namespace ImageClassifier.Shared;

public partial class Bootstrapper : IAsyncDisposable
{
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
        AppConfig config;
        try
        {
            var parsedResult = CommandLine.Parser.Default.ParseArguments<Options>(Environment.GetCommandLineArgs());
            config = await AppConfig.LoadAsync(parsedResult.Value.ConfigPath);
        }
        catch (FileNotFoundException)
        {
            config = new AppConfig();
        }

        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSingleton(config);
        serviceCollection.AddTransient<MainWindowModel>();

        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    public ServiceProvider GetServiceProvider()
    {
        return _serviceProvider ?? throw new NullReferenceException();
    }

    public async ValueTask DisposeAsync()
    {
    }
}
