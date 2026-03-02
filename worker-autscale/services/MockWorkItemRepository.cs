using worker_autscale.models;

namespace worker_autscale.services
{
    public class MockWorkItemRepository : IWorkItemRepository
    {
        private readonly ILogger<MockWorkItemRepository> _logger;
        private readonly Random _random = new();
        private int _itemIdCounter = 1000;

        public MockWorkItemRepository(ILogger<MockWorkItemRepository> logger)
        {
            _logger = logger;
        }

        public Task<int> GetUnprocessedCountAsync(CancellationToken cancellationToken)
        {
            var count = _random.Next(0, 10001);
            _logger.LogDebug("Unprocessed count: {count}", count);
            return Task.FromResult(count);
        }

        public Task<List<WorkItem>> GetBatchAsync(int batchSize, CancellationToken cancellationToken)
        {
            var items = new List<WorkItem>();
            var itemCount = _random.Next(0, batchSize + 1);
            
            for (int i = 0; i < itemCount; i++)
            {
                items.Add(new WorkItem
                {
                    Id = Interlocked.Increment(ref _itemIdCounter),
                    Data = $"WorkItem-{_itemIdCounter}-{Guid.NewGuid()}",
                    IsProcessed = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _logger.LogInformation("Retrieved batch of {count} items from database", items.Count);
            return Task.FromResult(items);
        }

        public Task MarkAsProcessedAsync(List<int> itemIds, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Marked {count} items as processed", itemIds.Count);
            return Task.CompletedTask;
        }
    }
}
