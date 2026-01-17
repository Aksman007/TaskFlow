using MongoDB.Driver;
using TaskFlow.Core.Documents;
using TaskFlow.Core.Interfaces.Repositories;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class FileMetadataRepository : IFileMetadataRepository
{
    private readonly IMongoCollection<FileMetadata> _collection;

    public FileMetadataRepository(MongoDbContext mongoContext)
    {
        _collection = mongoContext.FileMetadata;
    }

    public async Task<FileMetadata> AddFileAsync(FileMetadata file)
    {
        file.UploadedAt = DateTime.UtcNow;
        await _collection.InsertOneAsync(file);
        return file;
    }

    public async Task<FileMetadata?> GetFileByIdAsync(string id)
    {
        var filter = Builders<FileMetadata>.Filter.Eq(f => f.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<FileMetadata>> GetTaskFilesAsync(Guid taskId)
    {
        var filter = Builders<FileMetadata>.Filter.Eq(f => f.TaskId, taskId);
        var sort = Builders<FileMetadata>.Sort.Descending(f => f.UploadedAt);

        return await _collection
            .Find(filter)
            .Sort(sort)
            .ToListAsync();
    }

    public async Task DeleteFileAsync(string id)
    {
        var filter = Builders<FileMetadata>.Filter.Eq(f => f.Id, id);
        await _collection.DeleteOneAsync(filter);
    }

    public async Task<long> GetTotalFileSizeForTaskAsync(Guid taskId)
    {
        var filter = Builders<FileMetadata>.Filter.Eq(f => f.TaskId, taskId);
        var files = await _collection.Find(filter).ToListAsync();
        return files.Sum(f => f.FileSize);
    }
}