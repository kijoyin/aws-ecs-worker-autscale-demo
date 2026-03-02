# AWS ECS Deployment Guide

This guide walks through deploying the Worker Auto-scale service to AWS ECS with auto-scaling based on database workload using custom EMF metrics.

## Architecture Overview

The service:
1. Fetches batches of 100 items from a database
2. Processes each batch with simulated work
3. Publishes `UnprocessedItemsCount` metric to CloudWatch every 60 seconds
4. ECS auto-scales based on the unprocessed items count

### Mock vs Real Database

**For the Blog Demo**: The `MockWorkItemRepository` simulates database operations:
- Returns random counts between 0-10,000 for unprocessed items
- Generates random batches to demonstrate scaling behavior
- No actual database required

**For Production**: Replace with real repository implementation connecting to:
- SQL Server with Entity Framework Core
- PostgreSQL with Dapper
- DynamoDB or any other database

See README.md for implementation example.

## Prerequisites

- AWS CLI configured with appropriate credentials
- Docker installed
- AWS account with permissions for ECS, ECR, CloudWatch, and IAM

## Step 1: Create IAM Roles

### ECS Task Execution Role
This role allows ECS to pull images and write logs:

```bash
aws iam create-role \
  --role-name ecsTaskExecutionRole \
  --assume-role-policy-document file://ecs-task-execution-role-trust-policy.json

aws iam attach-role-policy \
  --role-name ecsTaskExecutionRole \
  --policy-arn arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy
```

Trust policy (ecs-task-execution-role-trust-policy.json):
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Service": "ecs-tasks.amazonaws.com"
      },
      "Action": "sts:AssumeRole"
    }
  ]
}
```

### ECS Task Role
This role allows the container to write logs and metrics to CloudWatch:

```bash
aws iam create-role \
  --role-name workerAutoscaleTaskRole \
  --assume-role-policy-document file://ecs-task-role-trust-policy.json

aws iam put-role-policy \
  --role-name workerAutoscaleTaskRole \
  --policy-name CloudWatchLogsPolicy \
  --policy-document file://cloudwatch-logs-policy.json
```

CloudWatch policy (cloudwatch-logs-policy.json):
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "logs:CreateLogGroup",
        "logs:CreateLogStream",
        "logs:PutLogEvents",
        "logs:DescribeLogStreams"
      ],
      "Resource": "arn:aws:logs:*:*:*"
    }
  ]
}
```

## Step 2: Create ECR Repository and Push Image

```bash
# Set variables
export AWS_REGION=us-east-1
export AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
export REPOSITORY_NAME=worker-autoscale

# Create ECR repository
aws ecr create-repository --repository-name $REPOSITORY_NAME --region $AWS_REGION

# Build Docker image
docker build -t $REPOSITORY_NAME:latest .

# Tag image
docker tag $REPOSITORY_NAME:latest $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$REPOSITORY_NAME:latest

# Login to ECR
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com

# Push image
docker push $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$REPOSITORY_NAME:latest
```

## Step 3: Create ECS Cluster

```bash
aws ecs create-cluster --cluster-name worker-autoscale-cluster --region $AWS_REGION
```

## Step 4: Register Task Definition

Update `ecs-task-definition.json` with your account ID and region, then:

```bash
aws ecs register-task-definition --cli-input-json file://ecs-task-definition.json --region $AWS_REGION
```

## Step 5: Create ECS Service

```bash
# Get VPC and Subnet IDs
export VPC_ID=$(aws ec2 describe-vpcs --filters "Name=isDefault,Values=true" --query "Vpcs[0].VpcId" --output text --region $AWS_REGION)
export SUBNET_ID=$(aws ec2 describe-subnets --filters "Name=vpc-id,Values=$VPC_ID" --query "Subnets[0].SubnetId" --output text --region $AWS_REGION)

# Create security group
export SG_ID=$(aws ec2 create-security-group \
  --group-name worker-autoscale-sg \
  --description "Security group for worker autoscale" \
  --vpc-id $VPC_ID \
  --region $AWS_REGION \
  --query 'GroupId' \
  --output text)

# Create ECS service
aws ecs create-service \
  --cluster worker-autoscale-cluster \
  --service-name worker-autoscale-service \
  --task-definition worker-autoscale-task \
  --desired-count 1 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[$SUBNET_ID],securityGroups=[$SG_ID],assignPublicIp=ENABLED}" \
  --region $AWS_REGION
```

## Step 6: Configure Auto Scaling

### Register Scalable Target

```bash
aws application-autoscaling register-scalable-target \
  --service-namespace ecs \
  --resource-id service/worker-autoscale-cluster/worker-autoscale-service \
  --scalable-dimension ecs:service:DesiredCount \
  --min-capacity 1 \
  --max-capacity 10 \
  --region $AWS_REGION
```

### Create CloudWatch Alarms for Scaling

**Scale Out Alarm** (when unprocessed count is high):
```bash
aws cloudwatch put-metric-alarm \
  --alarm-name worker-autoscale-high-workload \
  --alarm-description "Scale out when unprocessed items count is high" \
  --metric-name UnprocessedItemsCount \
  --namespace WorkerAutoscale \
  --statistic Average \
  --period 120 \
  --evaluation-periods 2 \
  --threshold 1000 \
  --comparison-operator GreaterThanThreshold \
  --dimensions Name=Service,Value=WorkerService \
  --region $AWS_REGION
```

**Scale In Alarm** (when unprocessed count is low):
```bash
aws cloudwatch put-metric-alarm \
  --alarm-name worker-autoscale-low-workload \
  --alarm-description "Scale in when unprocessed items count is low" \
  --metric-name UnprocessedItemsCount \
  --namespace WorkerAutoscale \
  --statistic Average \
  --period 300 \
  --evaluation-periods 2 \
  --threshold 500 \
  --comparison-operator LessThanThreshold \
  --dimensions Name=Service,Value=WorkerService \
  --region $AWS_REGION
```

### Create Target Tracking Scaling Policy (Alternative)

Instead of step scaling, you can use target tracking:

```bash
aws application-autoscaling put-scaling-policy \
  --service-namespace ecs \
  --resource-id service/worker-autoscale-cluster/worker-autoscale-service \
  --scalable-dimension ecs:service:DesiredCount \
  --policy-name worker-autoscale-target-tracking \
  --policy-type TargetTrackingScaling \
  --target-tracking-scaling-policy-configuration file://target-tracking-config.json \
  --region $AWS_REGION
```

target-tracking-config.json:
```json
{
  "TargetValue": 750.0,
  "CustomizedMetricSpecification": {
    "MetricName": "UnprocessedItemsCount",
    "Namespace": "WorkerAutoscale",
    "Dimensions": [
      {
        "Name": "Service",
        "Value": "WorkerService"
      }
    ],
    "Statistic": "Average"
  },
  "ScaleOutCooldown": 60,
  "ScaleInCooldown": 300
}
```

## Step 7: Monitor Metrics

View metrics in CloudWatch:
```bash
aws cloudwatch get-metric-statistics \
  --namespace WorkerAutoscale \
  --metric-name UnprocessedItemsCount \
  --dimensions Name=Service,Value=WorkerService \
  --start-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Average \
  --region $AWS_REGION
```

## Cleanup

```bash
# Delete ECS service
aws ecs update-service \
  --cluster worker-autoscale-cluster \
  --service worker-autoscale-service \
  --desired-count 0 \
  --region $AWS_REGION

aws ecs delete-service \
  --cluster worker-autoscale-cluster \
  --service worker-autoscale-service \
  --region $AWS_REGION

# Delete cluster
aws ecs delete-cluster --cluster worker-autoscale-cluster --region $AWS_REGION

# Delete ECR repository
aws ecr delete-repository --repository-name $REPOSITORY_NAME --force --region $AWS_REGION

# Delete security group
aws ec2 delete-security-group --group-id $SG_ID --region $AWS_REGION
```

## Troubleshooting

### Metrics Not Appearing
- Check CloudWatch Logs for the ECS task
- Verify task role has permissions for CloudWatch Logs
- Ensure EMF logs are being written (check `/ecs/worker-autoscale` log group)

### Scaling Not Triggering
- Verify CloudWatch alarms are configured correctly
- Check alarm state: `aws cloudwatch describe-alarms --alarm-names worker-autoscale-high-workload`
- Ensure auto-scaling policy is attached to the service
- Verify metrics are being published: Check for `UnprocessedItemsCount` in CloudWatch

### Container Failures
- Check ECS task logs in CloudWatch
- Verify container has network access
- Check task role permissions
