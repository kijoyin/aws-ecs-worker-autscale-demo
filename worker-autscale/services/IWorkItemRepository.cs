using worker_autscale.models;

namespace worker_autscale.services
{
    public interface IWorkItemRepository
    {
        Task<int> GetUnprocessedCountAsync(CancellationToken cancellationToken);
        Task<List<WorkItem>> GetBatchAsync(int batchSize, CancellationToken cancellationToken);
        Task MarkAsProcessedAsync(List<int> itemIds, CancellationToken cancellationToken);
    }
}
