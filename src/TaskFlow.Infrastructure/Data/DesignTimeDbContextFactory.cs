using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskFlow.Infrastructure.Data;

/// <summary>
/// Factory used by EF Core design-time tools (dotnet ef migrations, database update).
/// This avoids having to spin up the full application host at design time.
///
/// Connection string resolution order:
///   1. TASKFLOW_CONNECTION_STRING environment variable
///   2. Default development connection string
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TaskFlowDbContext>
{
    public TaskFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("TASKFLOW_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=taskflow;Username=taskflow;Password=dev_password";

        var optionsBuilder = new DbContextOptionsBuilder<TaskFlowDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions => npgsqlOptions.MigrationsAssembly("TaskFlow.Infrastructure"));

        return new TaskFlowDbContext(optionsBuilder.Options);
    }
}
