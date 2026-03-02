# AWS ECS Worker Auto-Scale with Custom EMF Metrics

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![AWS ECS](https://img.shields.io/badge/AWS-ECS-FF9900)](https://aws.amazon.com/ecs/)
[![CloudWatch](https://img.shields.io/badge/AWS-CloudWatch-FF9900)](https://aws.amazon.com/cloudwatch/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A production-ready .NET 10 Worker Service demonstrating **database-driven auto-scaling on AWS ECS** using **CloudWatch Embedded Metric Format (EMF)** for custom metrics.

Perfect for blog posts, tutorials, and learning AWS ECS auto-scaling with real-world patterns!

## 🎯 What This Project Demonstrates

- ✅ **.NET 10 Worker Service** with BackgroundService pattern
- ✅ **Database batch processing** (100 items at a time)
- ✅ **CloudWatch EMF** for custom metrics without additional AWS SDK calls
- ✅ **ECS Auto-scaling** based on business metrics (unprocessed items count)
- ✅ **Repository pattern** for easy database swapping
- ✅ **Docker containerization** for ECS Fargate deployment
- ✅ **Mock database** for demo purposes (easily replaceable with real DB)

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        AWS ECS Cluster                          │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │   Worker     │  │   Worker     │  │   Worker     │           │
│  │  Container   │  │  Container   │  │  Container   │           │
│  │   (Task)     │  │   (Task)     │  │   (Task)     │           │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘           │
│         │                 │                 │                   │
│         └─────────────────┼─────────────────┘                   │
│                           │                                     │
└───────────────────────────┼─────────────────────────────────────┘
                            │
                    ┌───────▼────────┐
                    │   Database     │  ← Batch reads (100 items)
                    │  (Simulated)   │  ← GetUnprocessedCount()
                    └───────┬────────┘  ← MarkAsProcessed()
                            │
                    ┌───────▼────────┐
                    │  CloudWatch    │
                    │   EMF Metrics  │
                    │ UnprocessedItems│  ← Published every 60s
                    │ Count: 5,432   │
                    └───────┬────────┘
                            │
                    ┌───────▼────────┐
                    │  Auto Scaling  │
                    │     Policy     │
                    │  > 1000: +1    │  ← Scale OUT
                    │  < 500:  -1    │  ← Scale IN
                    └────────────────┘
```

## 🚀 Quick Start

### Local Development

```bash
# Clone the repository
git clone https://github.com/yourusername/worker-autoscale.git
cd worker-autoscale

# Run the worker service
dotnet run --project worker-autscale

# Expected output:
# info: Worker started at: 2024-01-15 10:30:00
# info: Processing batch of 45 items
# info: Current unprocessed items count: 5432
```

### Docker Build

```bash
docker build -t worker-autoscale .
docker run -e ASPNETCORE_ENVIRONMENT=Development worker-autoscale
```

## 📊 Custom Metrics

The service publishes the following metrics to CloudWatch every 60 seconds:

| Metric Name | Type | Purpose |
|------------|------|---------|
| `UnprocessedItemsCount` | Count | Primary scaling metric - indicates workload |
| `WorkerHealthy` | Count | Health indicator (1 = healthy) |

### Auto-scaling Thresholds

| Unprocessed Count | Action |
|------------------|--------|
| **> 1000** | Scale OUT (add containers) |
| **500-1000** | Stable (maintain) |
| **< 500** | Scale IN (remove containers) |

## 📁 Project Structure

```
worker-autoscale/
├── worker-autscale/
│   ├── Worker.cs                          # Main BackgroundService
│   ├── WorkItem.cs                        # Entity model
│   ├── IWorkItemRepository.cs             # Repository interface
│   ├── MockWorkItemRepository.cs          # Mock DB implementation
│   ├── IWorkItemProcessor.cs              # Processor interface
│   ├── WorkItemProcessor.cs               # Batch processing logic
│   ├── CloudWatchEMFMetricsService.cs     # EMF metrics publisher
│   ├── Program.cs                         # DI configuration
│   ├── appsettings.json                   # Configuration
│   ├── Dockerfile                         # Container definition
│   ├── README.md                          # Detailed project docs
│   ├── DEPLOYMENT.md                      # AWS deployment guide
│   ├── QUICKSTART.md                      # Quick start guide
│   ├── ecs-task-definition.json           # ECS task definition
│   └── target-tracking-config.json        # Auto-scaling config
├── .gitignore
├── LICENSE
└── README.md                              # This file
```

## ⚙️ Configuration

Key settings in `appsettings.json`:

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

## 🚢 Deployment to AWS ECS

### Prerequisites
- AWS CLI configured
- Docker installed
- AWS account with ECS, ECR, CloudWatch permissions

### Quick Deploy

```bash
# Set your AWS region and account
export AWS_REGION=us-east-1
export AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)

# Create ECR repository
aws ecr create-repository --repository-name worker-autoscale --region $AWS_REGION

# Build and push Docker image
docker build -t worker-autoscale .
docker tag worker-autoscale:latest $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/worker-autoscale:latest
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com
docker push $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/worker-autoscale:latest

# Create ECS cluster
aws ecs create-cluster --cluster-name worker-autoscale-cluster --region $AWS_REGION
```

**For complete deployment steps**, see [DEPLOYMENT.md](worker-autscale/DEPLOYMENT.md)

## 📖 Documentation

- **[worker-autscale/README.md](worker-autscale/README.md)** - Detailed project documentation
- **[worker-autscale/DEPLOYMENT.md](worker-autscale/DEPLOYMENT.md)** - Step-by-step AWS deployment guide
- **[worker-autscale/QUICKSTART.md](worker-autscale/QUICKSTART.md)** - Quick start and local testing

## 🔄 From Mock to Production

The project uses `MockWorkItemRepository` which simulates database operations. To use a real database:

```csharp
// Example: SQL Server with Dapper
public class SqlWorkItemRepository : IWorkItemRepository
{
    private readonly IDbConnection _connection;
    
    public async Task<int> GetUnprocessedCountAsync(CancellationToken cancellationToken)
    {
        return await _connection.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM WorkItems WHERE IsProcessed = 0"
        );
    }
    
    // ... implement other methods
}

// Register in Program.cs
builder.Services.AddSingleton<IWorkItemRepository, SqlWorkItemRepository>();
```

See [worker-autscale/README.md](worker-autscale/README.md) for complete production implementation example.

## 🧪 Testing Auto-scaling

1. Deploy to ECS following [DEPLOYMENT.md](worker-autscale/DEPLOYMENT.md)
2. Monitor CloudWatch metrics:
   - Go to CloudWatch → Metrics → Custom Namespaces → `WorkerAutoscale`
3. Observe auto-scaling events:
   - ECS → Clusters → worker-autoscale-cluster → Services → worker-autoscale-service
4. View container logs:
   - CloudWatch → Log Groups → `/ecs/worker-autoscale`

## 💡 Use Cases

This pattern is perfect for:

- **Background job processors** with variable workload
- **Data pipeline workers** processing batches from databases
- **Event processors** with queue-like behavior
- **Scheduled tasks** that need to scale based on pending work
- **ETL processes** with varying data volumes

## 🎓 Learning Topics Covered

1. ✅ .NET 10 Worker Service and BackgroundService pattern
2. ✅ Repository pattern for data access abstraction
3. ✅ CloudWatch Embedded Metric Format (EMF)
4. ✅ ECS Fargate task definitions and services
5. ✅ ECS auto-scaling with custom metrics
6. ✅ Target tracking vs step scaling policies
7. ✅ Docker containerization for .NET
8. ✅ AWS IAM roles for ECS tasks
9. ✅ CloudWatch Logs integration
10. ✅ Batch processing strategies

## 📝 Blog Post Outline

This repository is designed to support a blog post with the following structure:

1. **Introduction** - Why auto-scale based on business metrics?
2. **Architecture Overview** - ECS + EMF + Custom Metrics
3. **Code Walkthrough** - Worker Service implementation
4. **EMF Deep Dive** - How EMF simplifies metrics
5. **Deployment** - Step-by-step ECS deployment
6. **Testing & Monitoring** - Observing auto-scaling in action
7. **Production Considerations** - Real database integration
8. **Conclusion** - When to use this pattern

## 🤝 Contributing

This is a demo project for educational purposes. Feel free to:
- Fork and experiment
- Submit issues for improvements
- Share your implementations
- Use in your blog posts or tutorials (attribution appreciated!)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- AWS ECS and CloudWatch teams for excellent documentation
- .NET team for BackgroundService pattern
- Community for feedback and contributions

## 📞 Support

- Open an issue for questions or problems
- Check [DEPLOYMENT.md](worker-autscale/DEPLOYMENT.md) for troubleshooting
- See [QUICKSTART.md](worker-autscale/QUICKSTART.md) for common scenarios

---

**Built with ❤️ using .NET 10 and AWS**

If this project helps you, please consider giving it a ⭐!
