using worker_autscale;

var builder = Host.CreateApplicationBuilder(args);

// Register repository and services
builder.Services.AddSingleton<IWorkItemRepository, MockWorkItemRepository>();
builder.Services.AddSingleton<IWorkItemProcessor, WorkItemProcessor>();
builder.Services.AddSingleton<IMetricsService, CloudWatchEMFMetricsService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
