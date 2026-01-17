using MongoDB.Driver;
using TaskFlow.Core.Documents;
using TaskFlow.Core.Interfaces.Repositories;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly IMongoCollection<ActivityLog> _collection;

    public ActivityLogRepository(MongoDbContext mongoContext)
    {
        _collection = mongoContext.ActivityLogs;
    }

    public async Task LogActivityAsync(ActivityLog log)
    {
        log.Timestamp = DateTime.UtcNow;
        await _collection.InsertOneAsync(log);
    }

    public async Task<IEnumerable<ActivityLog>> GetProjectActivityAsync(Guid projectId, int limit = 50)
    {
        var filter = Builders<ActivityLog>.Filter.Eq(a => a.ProjectId, projectId);
        var sort = Builders<ActivityLog>.Sort.Descending(a => a.Timestamp);

        return await _collection
            .Find(filter)
            .Sort(sort)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<ActivityLog>> GetUserActivityAsync(Guid userId, int limit = 50)
    {
        var filter = Builders<ActivityLog>.Filter.Eq(a => a.UserId, userId);
        var sort = Builders<ActivityLog>.Sort.Descending(a => a.Timestamp);

        return await _collection
            .Find(filter)
            .Sort(sort)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<ActivityLog>> GetTaskActivityAsync(Guid taskId, int limit = 50)
    {
        var filter = Builders<ActivityLog>.Filter.And(
            Builders<ActivityLog>.Filter.Eq(a => a.EntityType, "task"),
            Builders<ActivityLog>.Filter.Eq(a => a.EntityId, taskId)
        );
        var sort = Builders<ActivityLog>.Sort.Descending(a => a.Timestamp);

        return await _collection
            .Find(filter)
            .Sort(sort)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<ActivityLog>> GetActivityByDateRangeAsync(
        Guid projectId, 
        DateTime startDate, 
        DateTime endDate)
    {
        var filter = Builders<ActivityLog>.Filter.And(
            Builders<ActivityLog>.Filter.Eq(a => a.ProjectId, projectId),
            Builders<ActivityLog>.Filter.Gte(a => a.Timestamp, startDate),
            Builders<ActivityLog>.Filter.Lte(a => a.Timestamp, endDate)
        );
        var sort = Builders<ActivityLog>.Sort.Descending(a => a.Timestamp);

        return await _collection
            .Find(filter)
            .Sort(sort)
            .ToListAsync();
    }
}