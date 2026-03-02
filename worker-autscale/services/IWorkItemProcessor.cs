namespace worker_autscale.services
{
    public interface IWorkItemProcessor
    {
        Task<int> ProcessBatchAsync(CancellationToken cancellationToken);
    }
}
