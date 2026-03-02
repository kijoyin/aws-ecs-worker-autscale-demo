# Worker Auto-scale - AWS ECS with EMF Custom Metrics

This project demonstrates how to build a .NET Worker Service that:
- Runs on AWS ECS (Elastic Container Service)
- Processes batches of items from a database
- Publishes custom metrics using CloudWatch EMF (Embedded Metric Format)
- Enables auto-scaling based on unprocessed items count

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        AWS ECS Cluster                          │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│  │   Worker     │  │   Worker     │  │   Worker     │         │
│  │  Container   │  │  Container   │  │  Container   │         │
│  │   (Task)     │  │   (Task)     │  │   (Task)     │         │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘         │
│         │                 │                 │                  │
│         └─────────────────┼─────────────────┘                  │
│                           │                                     │
└───────────────────────────┼─────────────────────────────────────┘
                            │
                    ┌───────▼────────┐
                    │   Database     │  ← Batch reads (100 items)
                    │  (Simulated)   │  ← GetUnprocessedCount()
                    └───────┬────────┘  ← MarkAsProcessed()
                            │
                            │
                    ┌───────▼────────┐
                    │  CloudWatch    │
                    │   EMF Metrics  │
                    │                │
                    │ UnprocessedItems│  ← Published every 60s
                    │ Count: 5,432   │
                    └───────┬────────┘
                            │
                    ┌───────▼────────┐
                    │  Auto Scaling  │
                    │     Policy     │
                    │                │
                    │  > 1000: +1    │  ← Scale OUT
                    │  < 500:  -1    │  ← Scale IN
                    └────────────────┘
```

## Project Structure

- **Worker.cs**: Main background service that coordinates batch processing and metrics publishing
- **WorkItem.cs**: Entity model representing a database work item
- **IWorkItemRepository.cs / MockWorkItemRepository.cs**: Repository pattern for database operations (mocked for demo)
- **IWorkItemProcessor.cs / WorkItemProcessor.cs**: Batch processing logic
- **CloudWatchEMFMetricsService.cs**: Publishes custom metrics to CloudWatch using EMF format
- **appsettings.json**: Configuration for CloudWatch namespace, batch size, and intervals

## How It Works

### Batch Processing
1. Worker retrieves a batch of **100 items** from the database (configurable)
2. Each item is processed with simulated work (500ms-2000ms per item)
3. Processed items are marked as complete in the database
4. Process repeats continuously

### Metrics Publishing
Every 60 seconds, the service:
1. Queries the database for unprocessed items count
2. Publishes the count to CloudWatch as an EMF metric
3. AWS uses this metric to make scaling decisions

### Auto-scaling Logic
- **Scale Out**: When `UnprocessedItemsCount` > 1000
- **Scale In**: When `UnprocessedItemsCount` < 500
- ECS will add/remove containers based on workload

## Custom Metrics

The service publishes the following metrics to CloudWatch:
- `UnprocessedItemsCount`: Number of unprocessed items in the database (used for scaling decisions)
- `WorkerHealthy`: Health indicator (1 = healthy)

## Configuration

### appsettings.json
```json
{
  "CloudWatch": {
    "Namespace": "WorkerAutoscale",
    "ServiceName": "WorkerService"
  },
  "MetricsPublishIntervalSeconds": 60,
  "BatchSize": 100,
  "ProcessingDelayMilliseconds": 1000
}
```

**Configuration Options:**
- `MetricsPublishIntervalSeconds`: How often to publish metrics (default: 60)
- `BatchSize`: Number of items to process per batch (default: 100)
- `ProcessingDelayMilliseconds`: Delay between batch processing cycles (default: 1000ms)

### Environment Variables for ECS

When deploying to ECS, ensure these environment variables are set:
- `AWS_REGION`: AWS region (e.g., us-east-1)
- `ASPNETCORE_ENVIRONMENT`: Environment name (Development, Staging, Production)

## Mock Database Behavior

For this blog demo, the `MockWorkItemRepository`:
- Returns a **random count between 0 and 10,000** for unprocessed items
- Generates random batches (0 to 100 items) to simulate varying database workload
- Simulates database operations without requiring actual database setup

**In Production**: Replace `MockWorkItemRepository` with a real implementation using:
- Entity Framework Core with SQL Server
- Dapper with PostgreSQL
- Any other database technology

Example query for unprocessed count:
```sql
SELECT COUNT(*) FROM WorkItems WHERE IsProcessed = 0
```

Example query for batch retrieval:
```sql
SELECT TOP 100 * FROM WorkItems 
WHERE IsProcessed = 0 
ORDER BY CreatedAt
```

## Building the Docker Image

```bash
docker build -t worker-autoscale .
```

## Running Locally

```bash
dotnet run
```

## Deploying to AWS ECS

1. **Push Docker Image to ECR**:
```bash
aws ecr create-repository --repository-name worker-autoscale
docker tag worker-autoscale:latest {account-id}.dkr.ecr.{region}.amazonaws.com/worker-autoscale:latest
aws ecr get-login-password --region {region} | docker login --username AWS --password-stdin {account-id}.dkr.ecr.{region}.amazonaws.com
docker push {account-id}.dkr.ecr.{region}.amazonaws.com/worker-autoscale:latest
```

2. **Create ECS Task Definition** with:
   - Container image: Your ECR image URI
   - Task role with CloudWatch Logs permissions
   - Environment variables: `AWS_REGION`, `ASPNETCORE_ENVIRONMENT`

3. **Create ECS Service** with the task definition

4. **Configure Auto Scaling**:
   - Create CloudWatch alarm based on `UnprocessedItemsCount` metric
   - Target tracking scaling policy: Scale up when UnprocessedItemsCount > 1000, scale down when < 500
   - Min instances: 1, Max instances: 10

## EMF Metrics Output

The service outputs structured logs in EMF format that CloudWatch automatically converts to metrics:
```json
{
  "_aws": {
    "Timestamp": 1234567890,
    "CloudWatchMetrics": [{
      "Namespace": "WorkerAutoscale",
      "Dimensions": [["Service"], ["Service", "Environment"]],
      "Metrics": [
        {"Name": "UnprocessedItemsCount", "Unit": "Count"},
        {"Name": "WorkerHealthy", "Unit": "Count"}
      ]
    }]
  },
  "Service": "WorkerService",
  "Environment": "Production",
  "UnprocessedItemsCount": 5432,
  "WorkerHealthy": 1
}
```

## Scaling Logic

The auto-scaling policy should use the `UnprocessedItemsCount` metric:
- **Scale Out**: When average UnprocessedItemsCount > 1000 for 2 minutes
- **Scale In**: When average UnprocessedItemsCount < 500 for 5 minutes

This ensures the service scales based on actual database workload demand.

## Required IAM Permissions

The ECS task role needs:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "logs:CreateLogGroup",
        "logs:CreateLogStream",
        "logs:PutLogEvents"
      ],
      "Resource": "arn:aws:logs:*:*:*"
    }
  ]
}
```

## Blog Post Topics to Cover

1. Setting up a .NET Worker Service for batch processing containerized workloads
2. Implementing repository pattern for database operations
3. Understanding EMF format and its advantages over traditional metrics
4. Publishing custom metrics based on database workload
5. Configuring ECS Service Auto Scaling with custom database metrics
6. Batch processing strategies: size, frequency, and error handling
7. Testing auto-scaling behavior with varying database loads
8. Monitoring and troubleshooting scaling actions in production
9. Replacing mock repository with real database (Entity Framework Core)

## Real Database Implementation Example

To replace the mock with a real database, create a repository implementation:

```csharp
public class SqlWorkItemRepository : IWorkItemRepository
{
    private readonly IDbConnection _connection;

    public async Task<int> GetUnprocessedCountAsync(CancellationToken cancellationToken)
    {
        return await _connection.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM WorkItems WHERE IsProcessed = 0"
        );
    }

    public async Task<List<WorkItem>> GetBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        return (await _connection.QueryAsync<WorkItem>(
            @"SELECT TOP (@batchSize) * 
              FROM WorkItems 
              WHERE IsProcessed = 0 
              ORDER BY CreatedAt",
            new { batchSize }
        )).ToList();
    }

    public async Task MarkAsProcessedAsync(List<int> itemIds, CancellationToken cancellationToken)
    {
        await _connection.ExecuteAsync(
            @"UPDATE WorkItems 
              SET IsProcessed = 1, ProcessedAt = GETUTCDATE() 
              WHERE Id IN @itemIds",
            new { itemIds }
        );
    }
}
```

Then register it in `Program.cs`:
```csharp
builder.Services.AddSingleton<IWorkItemRepository, SqlWorkItemRepository>();
```

## License

MIT
