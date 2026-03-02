namespace worker_autscale
{
    public interface IWorkItemProcessor
    {
        Task<int> ProcessBatchAsync(CancellationToken cancellationToken);
    }
}
