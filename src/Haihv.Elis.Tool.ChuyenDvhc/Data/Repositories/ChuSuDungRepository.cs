using System.Data;
using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;
using Microsoft.Data.SqlClient;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

public class ChuSuDungRepository(string connectionString, ILogger? logger = null)
{
    private const long DefaultMaChuSuDungTemp = 0;

    /// <summary>
    /// Tạo chủ sử dụng tạm thời.
    /// </summary>
    /// <param name="maChuSuDungTemp">Mã chủ sử dụng tạm thời</param>
    /// <returns>Mã chủ sử dụng tạm thời</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    public async Task<long> CreateChuSuDungTempAsync(long maChuSuDungTemp = DefaultMaChuSuDungTemp)
    {
        // Tạo câu lệnh SQL để tạo hoặc cập nhật Chủ sử dụng tạm thời.
        const string queryChuSuDung = """
                                      IF NOT EXISTS (SELECT 1 FROM ChuSuDung WHERE MaChuSuDung = @MaChuSuDung)
                                      BEGIN
                                            INSERT INTO ChuSuDung (MaChuSuDung, MaDoiTuong, Ten1, SoDinhDanh1, GhiChu)
                                            VALUES (@MaChuSuDung, @MaDoiTuong, @Ten, @SoDinhDanh, @GhiChu)
                                      END
                                      ELSE
                                      BEGIN
                                            UPDATE ChuSuDung
                                            SET MaDoiTuong = @MaDoiTuong, Ten1 = @Ten, SoDinhDanh1 = @SoDinhDanh, GhiChu = @GhiChu
                                            WHERE MaChuSuDung = @MaChuSuDung
                                      END;
                                      """;
        const string queryChuSuDungLichSu = """
                                            IF NOT EXISTS (SELECT 1 FROM ChuSuDungLS WHERE MaChuSuDungLS = @MaChuSuDung)
                                            BEGIN
                                                INSERT INTO ChuSuDungLS (MaChuSuDungLS, MaDoiTuong, Ten1, SoDinhDanh1, GhiChu)
                                                VALUES (@MaChuSuDung, @MaDoiTuong, @Ten, @SoDinhDanh, @GhiChu)
                                            END
                                            ELSE
                                            BEGIN
                                                UPDATE ChuSuDungLS
                                                SET Ten1 = @Ten, MaDoiTuong = @MaDoiTuong, SoDinhDanh1 = @SoDinhDanh, GhiChu = @GhiChu
                                                WHERE MaChuSuDungLS = @MaChuSuDung
                                            END;
                                            """;
        const string query = $"""
                              {queryChuSuDung}
                              {queryChuSuDungLichSu}
                              """;
        // Tạo tham số cho câu lệnh SQL.
        var parameters = new
        {
            MaChuSuDung = maChuSuDungTemp,
            Ten = "Chủ sử dụng tạm thời",
            MaDoiTuong = 2,
            SoDinhDanh = "000000000",
            GhiChu = "Chủ sử dụng tạm thời"
        };
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            // Thực thi câu lệnh SQL
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                // Thực thi câu lệnh SQL
                await connection.ExecuteAsync(query, parameters, transaction);
                await transaction.CommitAsync();
                return maChuSuDungTemp;
            }
            catch (Exception e)
            {
                // Rollback transaction nếu có lỗi xảy ra
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            logger?.Error(e, "Lỗi khi tạo chủ sử dụng tạm thời.");
            throw;
        }
    }

    /// <summary>
    /// Xóa chủ sử dụng tạm thời.
    /// </summary>
    /// <param name="dbConnection">Kết nối cơ sở dữ liệu</param>
    /// <param name="maChuSuDungTemp">Mã chủ sử dụng tạm thời</param>
    /// <param name="logger">Logger để ghi lại thông tin lỗi (tùy chọn)</param>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    public static async Task DeleteChuSuDungTempAsync(SqlConnection dbConnection,
        long maChuSuDungTemp = DefaultMaChuSuDungTemp, ILogger? logger = null)
    {
        const string query = """
                             DELETE FROM ChuSuDung WHERE MaChuSuDung = @MaChuSuDung;
                             DELETE FROM ChuSuDungLS WHERE MaChuSuDungLS = @MaChuSuDung;
                             """;
        var parameters = new { MaChuSuDung = maChuSuDungTemp };
        try
        {
            await dbConnection.ExecuteAsync(query, parameters);
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi xóa chủ sử dụng tạm thời.");
            throw;
        }
    }
}