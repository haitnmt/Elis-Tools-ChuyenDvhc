using Microsoft.Extensions.Caching.Distributed;

namespace Haihv.Elis.Tool.ChuyenDvhc.Services;

public class FileDistributedCache(IFileService fileService, string cacheDirectory) : IDistributedCache
{
    public byte[]? Get(string key)
        => fileService.ReadAllBytes(GetPathByKey(key));

    public Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
       return fileService.ReadAllBytesAsync(GetPathByKey(key), cancellationToken);
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        => fileService.WriteAllBytes(GetPathByKey(key), value);

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken cancellationToken = default)
        => fileService.WriteAllBytesAsync(GetPathByKey(key),value, cancellationToken);

    public void Remove(string key)
    {
        // Kiểm tra tồn tại file trước khi xóa
        var filePath = GetPathByKey(key);
        if (fileService.Exists(filePath))
            fileService.Delete(filePath);
        else
            fileService.DeleteDirectory(GetPathByKey(key, true));
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        // Kiểm tra tồn tại file trước khi xóa
        var filePath = GetPathByKey(key);
        if (fileService.ExistsAsync(filePath).Result)
            fileService.DeleteAsync(filePath, cancellationToken);
        else
            fileService.DeleteDirectoryAsync(GetPathByKey(key, true), cancellationToken);
        return Task.CompletedTask;
    }
        

    public void Refresh(string key)
    {
        // Không cần làm gì
    }

    public Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        Refresh(key);
        return Task.CompletedTask;
    }

    private string GetPathByKey(string key, bool isDirectory = false)
    {
        // Tách key thành các phần bởi dấu ':'
        var parts = key.Split(':');
        // Xác định phần cuối là file hay thư mục (nếu không phải thư mục thì thêm đuôi .cache)
        var fileName = $"{parts.Last()}{(isDirectory ? "" : ".cache")}";
        // Các phần trước là thư mục
        var directories = parts.Take(parts.Length - 1);
        // Kết hợp các thư mục và tên file
        return Path.Combine(cacheDirectory, Path.Combine(directories.ToArray()), fileName);
    }
}
