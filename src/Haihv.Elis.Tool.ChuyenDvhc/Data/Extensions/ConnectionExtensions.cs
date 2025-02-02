using Dapper;
using Microsoft.Data.SqlClient;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;

public static class ConnectionExtensions
{
    /// <summary>
    /// Tạo một đối tượng <see cref="SqlConnection"/> từ chuỗi kết nối đến cơ sở dữ liệu.
    /// </summary>
    /// <param name="connectionString">Chuỗi kết nối đến cơ sở dữ liệu.</param>
    /// <returns>Đối tượng <see cref="SqlConnection"/>
    /// được tạo từ chuỗi kết nối đến cơ sở dữ liệu.
    /// </returns>
    /// <exception cref="ArgumentNullException">Nếu <paramref name="connectionString"/> là null hoặc rỗng.</exception>
    public static SqlConnection GetConnection(this string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        return new SqlConnection(connectionString);
    }
}