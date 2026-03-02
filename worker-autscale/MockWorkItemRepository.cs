namespace worker_autscale
{
    /// <summary>
    /// Mock repository that simulates database operations for the blog demo.
    /// In a real scenario, this would connect to an actual database (SQL Server, PostgreSQL, etc.)
    /// </summary>
    public class MockWorkItemRepository : IWorkItemRepository
    {
        private readonly ILogger<MockWorkItemRepository> _logger;
        private readonly Random _random = new();
        private int _itemIdCounter = 1000;

        public MockWorkItemRepository(ILogger<MockWorkItemRepository> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Returns a random count between 0 and 10000 to simulate unprocessed items in the database
        /// </summary>
        public Task<int> GetUnprocessedCountAsync(CancellationToken cancellationToken)
        {
            // For demo purposes, return a random count between 0 and 10000
            var count = _random.Next(0, 10001);
            _logger.LogDebug("Unprocessed count: {count}", count);
            return Task.FromResult(count);
        }

        /// <summary>
        /// Simulates retrieving a batch of items from the database
        /// </summary>
        public Task<List<WorkItem>> GetBatchAsync(int batchSize, CancellationToken cancellationToken)
        {
            // Simulate fetching items from database
            // In reality, this would be something like:
            // SELECT TOP 100 * FROM WorkItems WHERE IsProcessed = 0 ORDER BY CreatedAt
            
            var items = new List<WorkItem>();
            
            // Generate random number of items (0 to batchSize) to simulate varying workload
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

        /// <summary>
        /// Simulates marking items as processed in the database
        /// </summary>
        public Task MarkAsProcessedAsync(List<int> itemIds, CancellationToken cancellationToken)
        {
            // In reality, this would be something like:
            // UPDATE WorkItems SET IsProcessed = 1, ProcessedAt = GETUTCDATE() WHERE Id IN (@ids)
            
            _logger.LogInformation("Marked {count} items as processed", itemIds.Count);
            return Task.CompletedTask;
        }
    }
}
