using N8.Shared.Messaging;

namespace N8.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMqPublisher _publisher;

    public Worker(ILogger<Worker> logger, RabbitMqPublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = $"Worker running at: {DateTimeOffset.Now}";

                if (!_publisher.IsConnected)
                {
                    _publisher.Initialize();
                }

                _publisher.PublishMessage(message);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(message);
                }

                await Task.Delay(3000, stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An error occurred while publishing a message.");
            }
        }
    }

    public override void Dispose()
    {
        _publisher.Dispose();
        base.Dispose();
    }
}
