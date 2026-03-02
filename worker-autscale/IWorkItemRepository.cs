namespace worker_autscale
{
    public interface IWorkItemRepository
    {
        Task<int> GetUnprocessedCountAsync(CancellationToken cancellationToken);
        Task<List<WorkItem>> GetBatchAsync(int batchSize, CancellationToken cancellationToken);
        Task MarkAsProcessedAsync(List<int> itemIds, CancellationToken cancellationToken);
    }
}
