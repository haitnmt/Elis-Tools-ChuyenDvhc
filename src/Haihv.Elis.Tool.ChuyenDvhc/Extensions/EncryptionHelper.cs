using System.Security.Cryptography;
using System.Text;

namespace Haihv.Elis.Tool.ChuyenDvhc.Extensions;

/// <summary>
/// Lớp chứa các phương thức mở rộng để mã hóa và giải mã chuỗi văn bản.
/// </summary>
public static class EncryptionHelper
{
    private const string Key = "iHK2DThdy8ZJw4E753V5n8a7gYXSn9sU"; // Must be 16, 24, or 32 bytes long

    private const string Iv = "WDZKEVFjsM3q8F5D"; // Must be 16 bytes long

    private static byte[] GenerateKey(this string input, int keySize = 256)
    {
        // Sử dụng hàm băm SHA256 để tạo một bản rút gọn của chuỗi
        var hashedInput = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        // Chuyển đổi độ dài của khóa từ bit sang byte
        var keyLengthBytes = keySize / 8; //Phép chia lấy phần nguyên
        // Nếu độ dài của bản băm ngắn hơn độ dài yêu cầu của khóa, hãy lặp lại chuỗi ban đầu để đủ độ dài
        if (hashedInput.Length < keyLengthBytes)
        {
            var repeatedInput = new byte[keyLengthBytes];
            var remainingBytes = keyLengthBytes;
            var position = 0;
            // Lặp lại chuỗi ban đầu để tạo khóa có độ dài mong muốn
            while (remainingBytes > 0)
            {
                var bytesToCopy = Math.Min(hashedInput.Length, remainingBytes);
                Array.Copy(hashedInput, 0, repeatedInput, position, bytesToCopy);
                remainingBytes -= bytesToCopy;
                position += bytesToCopy;
            }

            return repeatedInput;
        }
        // Nếu độ dài của bản băm dài hơn độ dài yêu cầu của khóa, hãy cắt bớt
        else
        {
            var trimmedKey = new byte[keyLengthBytes];
            Array.Copy(hashedInput, trimmedKey, keyLengthBytes);
            return trimmedKey;
        }
    }

    private static byte[] GenerateIv(this string? iv, int blockSize = 128)
    {
        // Nếu có input string, tạo IV từ string đó
        if (iv is not null)
        {
            // Đảm bảo độ dài IV luôn là 16 bytes (128 bits)
            return SHA256.HashData(Encoding.UTF8.GetBytes(iv)).Take(16).ToArray();
        }

        // Nếu không có input, tạo IV ngẫu nhiên
        using var aesAlg = Aes.Create();
        aesAlg.GenerateIV(); // AES.GenerateIV() luôn tạo IV 16 bytes
        return aesAlg.IV;
    }

    private static string Encrypt(this string plainText, string secretKey, string iv, int keySize = 256)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = secretKey.GenerateKey(keySize);
        aesAlg.IV = iv.GenerateIv();
        var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// Mã hóa một chuỗi văn bản thành chuỗi Base64.
    /// </summary>
    /// <param name="plainText">Chuỗi văn bản cần mã hóa.</param>
    /// <returns>Chuỗi đã được mã hóa dưới dạng Base64.</returns>
    public static string Encrypt(this string plainText)
    {
        return plainText.Encrypt(Key, Iv);
    }

    private static string Decrypt(this string cipherText, string secretKey, string iv, int keySize = 256)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = GenerateKey(secretKey, keySize);
        aesAlg.IV = iv.GenerateIv();
        var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
        var cipherBytes = Convert.FromBase64String(cipherText);
        var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    /// <summary>
    /// Giải mã một chuỗi Base64 thành chuỗi văn bản.
    /// </summary>
    /// <param name="cipherText">Chuỗi Base64 cần giải mã.</param>
    /// <returns>Chuỗi văn bản đã được giải mã.</returns>
    public static string Decrypt(this string cipherText)
    {
        return cipherText.Decrypt(Key, Iv);
    }
}