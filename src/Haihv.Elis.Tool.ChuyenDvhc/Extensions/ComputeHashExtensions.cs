using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Haihv.Elis.Tool.ChuyenDvhc.Extensions;

/// <summary>
/// Lớp mở rộng để tính toán băm.
/// </summary>
public static class ComputeHashExtensions
{
    /// <summary>
    /// Tính toán băm SHA256 cho chuỗi đầu vào.
    /// </summary>
    /// <param name="input">Chuỗi đầu vào.</param>
    /// <returns>Chuỗi băm dưới dạng hex.</returns>
    private static string ComputeHash(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexStringLower(hashBytes);
    }

    /// <summary>
    /// Tính toán băm SHA256 cho đối tượng đầu vào.
    /// </summary>
    /// <typeparam name="T">Kiểu của đối tượng đầu vào.</typeparam>
    /// <param name="input">Đối tượng đầu vào.</param>
    /// <returns>Chuỗi băm dưới dạng hex hoặc null nếu đầu vào là null.</returns>
    public static string? ComputeHash<T>(this T? input)
    {
        if (input == null) return null;
        if (input is string stringInput)
        {
            return ComputeHash(stringInput);
        }

        var jsonString = JsonSerializer.Serialize(input);
        var inputBytes = Encoding.UTF8.GetBytes(jsonString);
        var hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexStringLower(hashBytes);
    }

    /// <summary>
    /// Tính toán băm SHA256 cho chuỗi và đối tượng đầu vào.
    /// </summary>
    /// <typeparam name="T">Kiểu của đối tượng đầu vào.</typeparam>
    /// <param name="txts">Chuỗi đầu vào.</param>
    /// <param name="obj">Đối tượng đầu vào.</param>
    /// <returns>Chuỗi băm dưới dạng hex hoặc null nếu đầu vào là null hoặc rỗng.</returns>
    public static string? ComputeHash<T>(string[]? txts = null, T? obj = default)
    {
        // Nếu cả hai đầu vào đều null hoặc rỗng thì trả về null
        if (obj == null && (txts == null || txts.Length == 0)) return null;
        var jsonString = string.Empty;

        // Nếu có chuỗi đầu vào thì nối chuỗi
        if (txts is { Length: > 0 })
            jsonString = txts.Where(string.IsNullOrWhiteSpace).Aggregate(jsonString, (current, t) => current + t);

        // Nếu có đối tượng đầu vào thì nối chuỗi
        if (obj != null)
            jsonString += JsonSerializer.Serialize(obj);

        // Nếu chuỗi kết quả rỗng thì trả về null
        if (string.IsNullOrWhiteSpace(jsonString)) return null;

        // Tính toán băm và trả về kết quả
        var inputBytes = Encoding.UTF8.GetBytes(jsonString);
        var hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexStringLower(hashBytes);
    }

    /// <summary>
    /// So sánh băm của hai đối tượng.
    /// </summary>
    /// <param name="obj1">Đối tượng thứ nhất.</param>
    /// <param name="obj2">Đối tượng thứ hai.</param>
    /// <returns>True nếu băm của hai đối tượng bằng nhau, ngược lại là false.</returns>
    /// <remarks>
    /// Nếu cả hai đối tượng đều null thì coi là bằng nhau.
    /// </remarks>
    public static bool EqualsHash<T>(this T? obj1, T? obj2)
    {
        if (obj1 == null && obj2 == null) return true; // Nếu cả hai đối tượng đều null thì coi là bằng nhau
        var hash1 = ComputeHash(obj1);
        var hash2 = ComputeHash(obj2);
        return hash1 == hash2;
    }
}