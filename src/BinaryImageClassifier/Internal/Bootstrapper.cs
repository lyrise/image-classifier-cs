using Microsoft.Extensions.DependencyInjection;
using BinaryImageClassifier.Windows.Main;

namespace BinaryImageClassifier.Internal;

public partial class Bootstrapper : IAsyncDisposable
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private ServiceProvider? _serviceProvider;

    public static Bootstrapper Instance { get; } = new Bootstrapper();

    private const string UI_STATUS_FILE_NAME = "ui_status.json";

    private Bootstrapper()
    {
    }

    public async ValueTask BuildAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var serviceCollection = new ServiceCollection();

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
