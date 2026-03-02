using worker_autscale.services;

namespace worker_autscale
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IWorkItemRepository _repository;
        private readonly IWorkItemProcessor _processor;
        private readonly IMetricsService _metricsService;
        private readonly IConfiguration _configuration;
        private readonly int _metricsPublishIntervalSeconds;
        private readonly int _processingDelayMilliseconds;

        public Worker(
            ILogger<Worker> logger,
            IWorkItemRepository repository,
            IWorkItemProcessor processor,
            IMetricsService metricsService,
            IConfiguration configuration)
        {
            _logger = logger;
            _repository = repository;
            _processor = processor;
            _metricsService = metricsService;
            _configuration = configuration;
            _metricsPublishIntervalSeconds = configuration.GetValue<int>("MetricsPublishIntervalSeconds", 60);
            _processingDelayMilliseconds = configuration.GetValue<int>("ProcessingDelayMilliseconds", 1000);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);
            _logger.LogInformation("Batch processing enabled with metrics publishing every {seconds} seconds", _metricsPublishIntervalSeconds);

            // Start metrics publishing task
            var metricsTask = PublishMetricsAsync(stoppingToken);

            // Start batch processing task
            var processingTask = ProcessBatchesAsync(stoppingToken);

            // Wait for both tasks (they run until cancellation)
            await Task.WhenAll(metricsTask, processingTask);
        }

        private async Task ProcessBatchesAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var processedCount = await _processor.ProcessBatchAsync(stoppingToken);

                    if (processedCount == 0)
                    {
                        // No items were processed, wait longer before trying again
                        await Task.Delay(_processingDelayMilliseconds * 5, stoppingToken);
                    }
                    else
                    {
                        // Items were processed, continue with normal delay
                        await Task.Delay(_processingDelayMilliseconds, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing batch");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        private async Task PublishMetricsAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var unprocessedCount = await _repository.GetUnprocessedCountAsync(stoppingToken);
                    _logger.LogInformation("Current unprocessed items count: {count}", unprocessedCount);

                    await _metricsService.PublishWorkloadMetricsAsync(unprocessedCount, stoppingToken);

                    await Task.Delay(TimeSpan.FromSeconds(_metricsPublishIntervalSeconds), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publishing metrics");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
    }
}
