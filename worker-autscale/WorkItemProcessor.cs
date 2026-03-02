namespace worker_autscale
{
    public class WorkItemProcessor : IWorkItemProcessor
    {
        private readonly IWorkItemRepository _repository;
        private readonly ILogger<WorkItemProcessor> _logger;
        private readonly IConfiguration _configuration;
        private readonly int _batchSize;
        private readonly Random _random = new();

        public WorkItemProcessor(
            IWorkItemRepository repository,
            ILogger<WorkItemProcessor> logger,
            IConfiguration configuration)
        {
            _repository = repository;
            _logger = logger;
            _configuration = configuration;
            _batchSize = configuration.GetValue<int>("BatchSize", 100);
        }

        public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                var items = await _repository.GetBatchAsync(_batchSize, cancellationToken);

                if (items.Count == 0)
                {
                    _logger.LogInformation("No items to process, waiting...");
                    return 0;
                }

                _logger.LogInformation("Processing batch of {count} items", items.Count);

                var processedIds = new List<int>();
                foreach (var item in items)
                {
                    await ProcessItemAsync(item, cancellationToken);
                    processedIds.Add(item.Id);
                }

                await _repository.MarkAsProcessedAsync(processedIds, cancellationToken);

                _logger.LogInformation("Successfully processed {count} items", processedIds.Count);
                return processedIds.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch");
                return 0;
            }
        }

        private async Task ProcessItemAsync(WorkItem item, CancellationToken cancellationToken)
        {
            var processingTime = _random.Next(500, 2001);
            await Task.Delay(processingTime, cancellationToken);
            _logger.LogDebug("Processed item {itemId}: {data}", item.Id, item.Data);
        }
    }
}
