namespace worker_autscale
{
    public class WorkItem
    {
        public int Id { get; set; }
        public string Data { get; set; } = string.Empty;
        public bool IsProcessed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
