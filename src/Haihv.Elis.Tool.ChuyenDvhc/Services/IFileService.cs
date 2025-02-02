namespace Haihv.Elis.Tool.ChuyenDvhc.Services;

public interface IFileService
{
    Task<string> ReadFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task WriteFileAsync(string filePath, string content, CancellationToken cancellationToken = default);
    Task WriteAllBytesAsync(string filePath, byte[] content, CancellationToken cancellationToken = default);
    void WriteAllBytes(string filePath, byte[] content);
    Task<bool> CreateAsync(string filePath);
    Task DeleteAsync(string filePath, CancellationToken cancellationToken = default);
    void Delete(string filePath);
    Task DeleteDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);
    bool DeleteDirectory(string directoryPath);
    Task<bool> ExistsAsync(string filePath);
    bool Exists(string filePath);
    Task<byte[]?> ReadAllBytesAsync(string filePath, CancellationToken cancellationToken = default);
    byte[]? ReadAllBytes(string filePath);
}