# Quick Start Guide

## Local Development

1. **Clone and Run**:
   ```bash
   dotnet run --project worker-autscale
   ```

2. **What You'll See**:
   - Worker starts and begins processing batches
   - Every 60 seconds, it reports the unprocessed items count (random 0-10,000)
   - Logs show batch processing activity

3. **Expected Output**:
   ```
   info: worker_autscale.Worker[0]
         Worker started at: 2024-01-15 10:30:00 +00:00
   info: worker_autscale.Worker[0]
         Batch processing enabled with metrics publishing every 60 seconds
   info: worker_autscale.MockWorkItemRepository[0]
         Retrieved batch of 45 items from database
   info: worker_autscale.WorkItemProcessor[0]
         Processing batch of 45 items
   info: worker_autscale.Worker[0]
         Current unprocessed items count: 5432
   info: worker_autscale.CloudWatchEMFMetricsService[0]
         Published EMF metrics - UnprocessedItemsCount: 5432
   ```

## Configuration Changes

### Change Batch Size
In `appsettings.json`:
```json
{
  "BatchSize": 200  // Process 200 items per batch instead of 100
}
```

### Change Metrics Publishing Frequency
```json
{
  "MetricsPublishIntervalSeconds": 30  // Publish every 30 seconds
}
```

### Change Processing Delay
```json
{
  "ProcessingDelayMilliseconds": 5000  // Wait 5 seconds between batches
}
```

## Understanding the Scaling Metrics

The service publishes `UnprocessedItemsCount` which represents pending database work:

| Unprocessed Count | Expected Behavior |
|------------------|-------------------|
| 0 - 500 | Scale IN - Reduce containers |
| 500 - 1000 | Stable - Maintain current capacity |
| 1000+ | Scale OUT - Add more containers |

## Docker Testing

1. **Build Image**:
   ```bash
   docker build -t worker-autoscale .
   ```

2. **Run Container**:
   ```bash
   docker run -e ASPNETCORE_ENVIRONMENT=Development worker-autoscale
   ```

3. **Run with Custom Config**:
   ```bash
   docker run \
     -e ASPNETCORE_ENVIRONMENT=Production \
     -e BatchSize=150 \
     -e MetricsPublishIntervalSeconds=30 \
     worker-autoscale
   ```

## Simulating High Load

The mock repository generates random counts. To test scaling behavior in a real scenario:

1. Increase the batch size to process items faster
2. Decrease processing delay to speed up cycles
3. Watch CloudWatch metrics to see the scaling triggers

## Monitoring in CloudWatch

After deploying to ECS:

1. **View Logs**: 
   - Go to CloudWatch → Log Groups → `/ecs/worker-autoscale`

2. **View Metrics**:
   - Go to CloudWatch → Metrics → Custom Namespaces → `WorkerAutoscale`
   - Select `UnprocessedItemsCount` metric

3. **View Alarms**:
   - Go to CloudWatch → Alarms
   - Check status of `worker-autoscale-high-workload` and `worker-autoscale-low-workload`

## Troubleshooting

### No logs appearing locally
- EMF logs only work when running in AWS environment
- Locally, you'll see standard console logs

### Want to see EMF format locally?
The EMF library outputs to stdout in JSON format when running on ECS. Locally, it's intercepted by the logger.

### Adjust random count range
In `MockWorkItemRepository.cs`, modify:
```csharp
var count = _random.Next(0, 10001);  // Change range here
```

## Next Steps

1. Deploy to AWS ECS following `DEPLOYMENT.md`
2. Monitor CloudWatch metrics
3. Observe auto-scaling in action
4. Replace mock repository with real database implementation

## Converting to Production

Replace `MockWorkItemRepository` with a real implementation:

1. Add database connection package (e.g., Entity Framework Core)
2. Create a new repository class implementing `IWorkItemRepository`
3. Update `Program.cs` registration:
   ```csharp
   builder.Services.AddSingleton<IWorkItemRepository, SqlWorkItemRepository>();
   ```
4. Add connection string configuration
5. Deploy with database credentials

See `README.md` for full production implementation example.
