using MongoDB.Driver;
using TaskFlow.Core.Documents;
using TaskFlow.Core.Interfaces.Repositories;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class TaskCommentRepository : ITaskCommentRepository
{
    private readonly IMongoCollection<TaskComment> _collection;

    public TaskCommentRepository(MongoDbContext mongoContext)
    {
        _collection = mongoContext.TaskComments;
    }

    public async Task<TaskComment> AddCommentAsync(TaskComment comment)
    {
        comment.CreatedAt = DateTime.UtcNow;
        await _collection.InsertOneAsync(comment);
        return comment;
    }

    public async Task<TaskComment?> GetCommentByIdAsync(string id)
    {
        var filter = Builders<TaskComment>.Filter.Eq(c => c.Id, id)
            & Builders<TaskComment>.Filter.Ne(c => c.IsDeleted, true);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<TaskComment>> GetTaskCommentsAsync(Guid taskId)
    {
        var filter = Builders<TaskComment>.Filter.Eq(c => c.TaskId, taskId)
            & Builders<TaskComment>.Filter.Ne(c => c.IsDeleted, true);
        var sort = Builders<TaskComment>.Sort.Ascending(c => c.CreatedAt);

        return await _collection
            .Find(filter)
            .Sort(sort)
            .ToListAsync();
    }

    public async Task UpdateCommentAsync(string id, string content)
    {
        var filter = Builders<TaskComment>.Filter.Eq(c => c.Id, id);
        var update = Builders<TaskComment>.Update
            .Set(c => c.Content, content)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task DeleteCommentAsync(string id)
    {
        var filter = Builders<TaskComment>.Filter.Eq(c => c.Id, id);
        var update = Builders<TaskComment>.Update
            .Set(c => c.IsDeleted, true)
            .Set(c => c.DeletedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task<int> GetTaskCommentCountAsync(Guid taskId)
    {
        var filter = Builders<TaskComment>.Filter.Eq(c => c.TaskId, taskId)
            & Builders<TaskComment>.Filter.Ne(c => c.IsDeleted, true);
        return (int)await _collection.CountDocumentsAsync(filter);
    }

    public async Task<(IEnumerable<TaskComment> Items, int TotalCount)> GetTaskCommentsPagedAsync(Guid taskId, int skip, int take)
    {
        var filter = Builders<TaskComment>.Filter.Eq(c => c.TaskId, taskId) &
                     Builders<TaskComment>.Filter.Ne(c => c.IsDeleted, true);
        var sort = Builders<TaskComment>.Sort.Ascending(c => c.CreatedAt);

        var totalCount = (int)await _collection.CountDocumentsAsync(filter);
        var items = await _collection
            .Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(take)
            .ToListAsync();

        return (items, totalCount);
    }
}