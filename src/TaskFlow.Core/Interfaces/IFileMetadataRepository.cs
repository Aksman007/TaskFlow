using TaskFlow.Core.Documents;

namespace TaskFlow.Core.Interfaces.Repositories;

public interface IFileMetadataRepository
{
    Task<FileMetadata> AddFileAsync(FileMetadata file);
    Task<FileMetadata?> GetFileByIdAsync(string id);
    Task<IEnumerable<FileMetadata>> GetTaskFilesAsync(Guid taskId);
    Task DeleteFileAsync(string id);
    Task<long> GetTotalFileSizeForTaskAsync(Guid taskId);
}