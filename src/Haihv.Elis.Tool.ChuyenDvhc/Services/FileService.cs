using System.Text;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc.Services;

public class FileService(ILogger logger) : IFileService
{
    public async Task<string> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                logger.Debug("Tệp không tồn tại! [{filePath}]", filePath);
                return string.Empty;
            }

            // Sử dụng using để đảm bảo giải phóng tài nguyên
            await using var fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            // Sử dụng UTF8 encoding để đọc chính xác
            using var reader = new StreamReader(
                fileStream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 4096,
                leaveOpen: false);

            return await reader.ReadToEndAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.Warning($"Lỗi khi đọc tệp: {ex.Message}");
            return string.Empty;
        }
    }
    
    public async Task<byte[]?> ReadAllBytesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(filePath)) return await File.ReadAllBytesAsync(filePath, cancellationToken);
            logger.Debug("Tệp không tồn tại! [{filePath}]", filePath);
            return null;

        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Lỗi khi đọc tệp!");
            return null;
        }
    }
    
    public byte[]? ReadAllBytes(string filePath)
    {
        try
        {
            if (File.Exists(filePath)) return File.ReadAllBytes(filePath);
            logger.Debug("Tệp không tồn tại! [{filePath}]", filePath);
            return null;

        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Lỗi khi đọc tệp!");
            return null;
        }
    }

    public async Task WriteFileAsync(string filePath, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            // Đảm bảo directory tồn tại
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Sử dụng FileMode.Create để ghi đè file cũ nếu tồn tại
            await using var fileStream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true);

            // Sử dụng UTF8 encoding không có BOM
            await using var writer = new StreamWriter(
                fileStream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                bufferSize: 4096,
                leaveOpen: false);
            await writer.WriteAsync(content);
            await writer.FlushAsync(cancellationToken); // Đảm bảo dữ liệu được ghi xuống disk
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Lỗi khi đọc tệp!");
        }
    }
    
    public async Task WriteAllBytesAsync(string filePath, byte[] content, CancellationToken cancellationToken = default)
    {
        try
        {
            // Đảm bảo directory tồn tại
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            await File.WriteAllBytesAsync(filePath, content, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Lỗi khi đọc tệp!");
        }
    }
    
    public void WriteAllBytes(string filePath, byte[] content)
    {
        try
        {
            // Đảm bảo directory tồn tại
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllBytes(filePath, content);
        }
        catch (Exception ex)
        {
            logger.Warning(ex,"Lỗi khi ghi tệp");
        }
    }

    public async Task<bool> CreateAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                logger.Warning("Têp đã tồn tại!");
                return false;
            }
            await using var file = File.Create(filePath);
            return true;
        }
        catch (Exception ex)
        {
            logger.Warning(ex,"Lỗi khi tạo tệp");
            return false;
        }
    }

    public async Task DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await ExistsAsync(filePath))
            {
                logger.Information("Tệp không tồn tại! [{filePath}]", filePath);
            }
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            logger.Warning(ex,"Lỗi khi xóa tệp");
        }
    }

    public void Delete(string filePath)
    {
        try
        {
            if (!Exists(filePath))
            {
                logger.Debug("Tệp không tồn tại! [{filePath}]", filePath);
            }
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            logger.Warning(ex,"Lỗi khi xóa tệp");
        }
    }

    public Task DeleteDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                logger.Information("Thư mục không tồn tại! [{directoryPath}]", directoryPath);
            }
            Directory.Delete(directoryPath, true);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.Warning(ex,"Lỗi khi xóa thư mục");
            return Task.CompletedTask;
        }
    }

    public bool DeleteDirectory(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                logger.Debug("Thư mục không tồn tại! [{directoryPath}]", directoryPath);
            }
            Directory.Delete(directoryPath, true);
            return true;
        }
        catch (Exception ex)
        {
            logger.Warning(ex,"Lỗi khi xóa thư mục");
            return false;
        }
    }

    public Task<bool> ExistsAsync(string filePath)
    {
        return Task.FromResult(File.Exists(filePath));
    }
    
    public bool Exists(string filePath)
    {
        return File.Exists(filePath);
    }
}