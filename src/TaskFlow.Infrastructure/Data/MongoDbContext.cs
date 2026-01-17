using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using TaskFlow.Core.Documents;

namespace TaskFlow.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration["MongoDB:ConnectionString"]
            ?? "mongodb://taskflow:dev_password@localhost:27017";
        var databaseName = configuration["MongoDB:DatabaseName"] ?? "taskflow";

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);

        // Create indexes
        CreateIndexes();
    }

    public IMongoCollection<TaskComment> TaskComments =>
        _database.GetCollection<TaskComment>("task_comments");

    public IMongoCollection<ActivityLog> ActivityLogs =>
        _database.GetCollection<ActivityLog>("activity_logs");

    public IMongoCollection<FileMetadata> FileMetadata =>
        _database.GetCollection<FileMetadata>("file_metadata");

    private void CreateIndexes()
    {
        // TaskComments indexes
        var taskCommentsIndexKeys = Builders<TaskComment>.IndexKeys
            .Ascending(c => c.TaskId)
            .Descending(c => c.CreatedAt);

        TaskComments.Indexes.CreateOne(
            new CreateIndexModel<TaskComment>(taskCommentsIndexKeys));

        // ActivityLogs indexes
        var activityLogsIndexKeys = Builders<ActivityLog>.IndexKeys
            .Ascending(a => a.ProjectId)
            .Descending(a => a.Timestamp);

        ActivityLogs.Indexes.CreateOne(
            new CreateIndexModel<ActivityLog>(activityLogsIndexKeys));

        // FileMetadata indexes
        var fileMetadataIndexKeys = Builders<FileMetadata>.IndexKeys
            .Ascending(f => f.TaskId);

        FileMetadata.Indexes.CreateOne(
            new CreateIndexModel<FileMetadata>(fileMetadataIndexKeys));
    }
}