using Amazon.CloudWatch.EMF.Logger;
using Amazon.CloudWatch.EMF.Model;

namespace worker_autscale
{
    public interface IMetricsService
    {
        Task PublishWorkloadMetricsAsync(int unprocessedCount, CancellationToken cancellationToken);
    }

    public class CloudWatchEMFMetricsService : IMetricsService
    {
        private readonly ILogger<CloudWatchEMFMetricsService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _namespace;
        private readonly string _serviceName;

        public CloudWatchEMFMetricsService(
            ILogger<CloudWatchEMFMetricsService> logger, 
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _namespace = configuration.GetValue<string>("CloudWatch:Namespace") ?? "WorkerAutoscale";
            _serviceName = configuration.GetValue<string>("CloudWatch:ServiceName") ?? "WorkerService";
        }

        public async Task PublishWorkloadMetricsAsync(int unprocessedCount, CancellationToken cancellationToken)
        {
            try
            {
                var metricsLogger = new MetricsLogger();

                metricsLogger.SetNamespace(_namespace);

                var dimensions = new DimensionSet();
                dimensions.AddDimension("Service", _serviceName);
                dimensions.AddDimension("Environment", GetEnvironment());
                metricsLogger.SetDimensions(dimensions);

                metricsLogger.PutMetric("UnprocessedItemsCount", unprocessedCount, Unit.COUNT);
                metricsLogger.PutMetric("WorkerHealthy", 1, Unit.COUNT);

                metricsLogger.PutProperty("Timestamp", DateTimeOffset.UtcNow.ToString("o"));
                metricsLogger.PutProperty("ServiceVersion", "1.0.0");

                metricsLogger.Flush();

                _logger.LogDebug("Published EMF metrics - UnprocessedItemsCount: {unprocessedCount}", unprocessedCount);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish EMF metrics");
            }
        }

        private string GetEnvironment()
        {
            return _configuration.GetValue<string>("Environment") 
                   ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                   ?? "Production";
        }
    }
}
