// Integration tests share one SQL Server + Redis container (see IntegrationEnvironment)
// and must run sequentially: each test gets its own database on the shared server, but
// Redis keys, the xUnit ITestOutputHelper, and WebApplicationFactory are not safe for
// parallel access. Running in parallel would cause sporadic failures and slowdown.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
