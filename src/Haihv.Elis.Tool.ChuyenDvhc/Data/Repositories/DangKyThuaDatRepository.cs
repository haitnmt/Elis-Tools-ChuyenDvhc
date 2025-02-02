using System.Data;
using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;
using Microsoft.Data.SqlClient;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

public class DangKyThuaDatRepository(string connectionString, ILogger? logger = null)
{
    public const long DefaultMaDangKyTemp = 0;

    /// <summary>
    /// Kiểm tra xem mã Đăng ký có vượt quá giới hạn không.
    /// </summary>
    /// <param name="dvhc">Đơn vị hành chính.</param>
    /// <returns>Trả về true nếu không vượt quá giới hạn, ngược lại trả về false.</returns>
    private async Task<bool> CheckOverflowAsync(DvhcRecord dvhc)
    {
        // Lấy tổng số lượng Đăng ký theo Đơn Vị Hành Chính
        const string query = """
                             SELECT COUNT(DISTINCT MaDangKy) 
                             FROM (
                                 SELECT DISTINCT dk.MaDangKy AS MaDangKy
                                 FROM DangKyQSDD dk
                                          INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
                                          INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                 WHERE tbd.MaDVHC = @MaDVHC
                                 UNION ALL
                                 SELECT DISTINCT dk.MaDangKyLS AS MaDangKy
                                 FROM DangKyQSDDLS dk
                                          INNER JOIN ThuaDatLS td ON dk.MaThuaDatLS = td.MaThuaDatLS
                                          INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                 WHERE tbd.MaDVHC = @MaDVHC
                                 UNION ALL
                                 SELECT DISTINCT dk.MaDangKyLS AS MaDangKy
                                 FROM  DangKyQSDDLS dk
                                     INNER JOIN ThuaDat td ON dk.MaThuaDatLS = td.MaThuaDat
                                     INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                 WHERE tbd.MaDVHC = @MaDVHC
                             ) AS CombinedResults
                             """;
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();
            // Tạo tham số cho câu lệnh SQL
            var parameters = new { MaDVHC = dvhc.MaDvhc };
            // Thực thi câu lệnh SQL
            var count = await connection.ExecuteScalarAsync<int>(query, parameters);
            // Kiểm tra xem số lượng Đăng ký có vượt quá giới hạn không
            return count < PrimaryKeyExtensions.GetMaximumPrimaryKey();
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi kiểm tra vượt quá giới hạn Đăng ký. [MaDVHC: {MaDVHC}]", dvhc.MaDvhc);
            return false;
        }
    }

    /// <summary>
    /// Tạo Đăng ký tạm thời.
    /// </summary>
    /// <param name="maDangKyTemp">
    /// Mã Thửa Đất tạm thời (tùy chọn).
    /// Mặc định: <see cref="DefaultMaDangKyTemp"/>
    /// </param>
    /// <param name="maThuaDatTemp">Mã thửa đất tạm thời.
    /// Mặc định: <see cref="ThuaDatRepository.DefaultMaThuaDatTemp"/>
    /// </param>
    /// <param name="reCreateTempThuaDat">Tạo lại thửa đất tạm thời (tùy chọn). Mặc định = false.</param>
    /// <returns>Mã Đăng ký tạm thời.</returns>
    public async Task<long> CreateTempDangKyAsync(long maDangKyTemp = DefaultMaDangKyTemp,
        long maThuaDatTemp = ThuaDatRepository.DefaultMaThuaDatTemp, bool reCreateTempThuaDat = false)
    {
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();
            if (reCreateTempThuaDat && maThuaDatTemp == ThuaDatRepository.DefaultMaThuaDatTemp)
            {
                // Tạo lại thửa đất tạm thời
                maThuaDatTemp = await new ThuaDatRepository(connectionString, logger)
                    .CreateThuaDatTempAsync(reCreateTempToBanDo: true);
            }

            // Tạo câu lệnh SQL để tạo hoặc cập nhật đăng ký tạm thời
            const string queryDangKy = """
                                       IF NOT EXISTS (SELECT 1 FROM DangKyQSDD WHERE MaDangKy = @MaDangKy)
                                       BEGIN
                                           INSERT INTO DangKyQSDD (MaDangKy, MaThuaDat, DienTichDangKy, DienTichChung, GhiChu)
                                           VALUES (@MaDangKy, @MaThuaDat, 0, 0, @GhiChu)
                                       END
                                       ELSE
                                       BEGIN
                                           UPDATE DangKyQSDD
                                           SET MaThuaDat = @MaThuaDat, GhiChu = @GhiChu
                                           WHERE MaDangKy = @MaDangKy
                                       END;
                                       """;
            const string queryDangKyLs = """
                                         IF NOT EXISTS (SELECT 1 FROM DangKyQSDDLS WHERE MaDangKyLS = @MaDangKy)
                                         BEGIN
                                              INSERT INTO DangKyQSDDLS (MaDangKyLS, MaThuaDatLS, DienTichDangKy, DienTichChung, GhiChu)
                                              VALUES (@MaDangKy, @MaThuaDat, 0, 0, @GhiChu)
                                         END
                                         ELSE
                                         BEGIN
                                              UPDATE DangKyQSDDLS
                                              SET MaThuaDatLS = @MaThuaDat, GhiChu = @GhiChu
                                              WHERE MaDangKyLS = @MaDangKy
                                         END;
                                         """;
            const string query = $"""
                                    {queryDangKy}
                                    {queryDangKyLs}
                                  """;
            // Tạo tham số cho câu lệnh SQL
            var parameters = new
            {
                MaDangKy = maDangKyTemp,
                MaThuaDat = maThuaDatTemp,
                GhiChu = "Thửa đất tạm thời"
            };

            if (connection.State == ConnectionState.Closed)
            {
                await connection.OpenAsync();
            }
            // Sử dụng giao dịch để đảm bảo tính nhất quán

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Thực thi câu lệnh SQL trong giao dịch
                await connection.ExecuteAsync(query, parameters, transaction: transaction);
                // Commit giao dịch nếu thành công
                await transaction.CommitAsync();
                return parameters.MaDangKy;
            }
            catch (Exception ex)
            {
                // Rollback giao dịch nếu có lỗi
                await transaction.RollbackAsync();
                logger?.Error(ex,
                    "Lỗi khi tạo hoặc cập nhật Thửa Đất tạm thời. [MaThuaDat: {MaThuaDatTemp}, MaDangKy: {MaDangKyTemp}]",
                    maThuaDatTemp, maDangKyTemp);
                throw;
            }
        }
        catch (Exception e)
        {
            if (logger == null) throw;
            logger.Error(e,
                "Lỗi khi tạo Thửa Đất tạm thời. [MaThuaDat: {MaThuaDatTemp}, MaDangKy: {MaDangKyTemp}]",
                maThuaDatTemp, maDangKyTemp);
            throw;
        }
    }

    /// <summary>
    /// Lấy mã Đăng ký lớn nhất theo Đơn Vị Hành Chính.
    /// </summary>
    /// <param name="dvhc">Bản ghi Đơn Vị Hành Chính.</param>
    /// <returns>Mã Đăng ký lớn nhất.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    private async Task<long> GetMaxMaDangKyAsync(DvhcRecord dvhc)
    {
        // Tạo câu lệnh SQL để lấy mã Đăng ký lớn nhất
        const string queryMaxMaDangKy = """
                                        SELECT ISNULL(MAX(dk.MaDangKy),0) AS MaDangKy
                                        FROM DangKyQSDD dk
                                            INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
                                            INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                        WHERE tbd.MaDVHC = @MaDVHC
                                            AND dk.MaDangKy > @MinMaDangKy
                                            AND dk.MaDangKy < @MaxMaDangKy
                                        """;

        const string queryMaxMaDangKyLs = """
                                          SELECT ISNULL(MAX(dk.MaDangKyLS),0) AS MaDangKy
                                          FROM DangKyQSDDLS dk
                                              INNER JOIN ThuaDatLS td ON dk.MaThuaDatLS = td.MaThuaDatLS
                                              INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                          WHERE tbd.MaDVHC = @MaDVHC
                                              AND dk.MaDangKyLS > @MinMaDangKy
                                              AND dk.MaDangKyLS < @MaxMaDangKy
                                          """;
        const string queryDangKyLsOnThuaDat = """
                                              SELECT ISNULL(MAX(dk.MaDangKyLS),0) AS MaDangKy
                                              FROM DangKyQSDDLS dk
                                                  INNER JOIN ThuaDat td ON dk.MaThuaDatLS = td.MaThuaDat
                                                  INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                              WHERE tbd.MaDVHC = @MaDVHC
                                                  AND dk.MaDangKyLS > @MinMaDangKy
                                                  AND dk.MaDangKyLS < @MaxMaDangKy
                                              """; // Lấy Mã Đăng ký từ bảng DangKyLS ON ThuaDat
        const string query = $"""
                              SELECT MAX(MaDangKy) AS MaDangKy
                              FROM (
                                  {queryMaxMaDangKy}
                                  UNION ALL
                                  {queryMaxMaDangKyLs}
                                  UNION ALL
                                  {queryDangKyLsOnThuaDat}
                              ) AS CombinedResults
                              """; // Kết hợp kết quả từ 2 bảng DangKy và DangKyLS
        // Tạo tham số cho câu lệnh SQL
        var parameters = new
        {
            MaDVHC = dvhc.MaDvhc,
            MinMaDangKy = dvhc.Ma.GetMinPrimaryKey(),
            MaxMaDangKy = dvhc.Ma.GetMaxPrimaryKey()
        };
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();

            // Thực thi câu lệnh SQL
            return await connection.ExecuteScalarAsync<long>(query, parameters);
        }
        catch (Exception exception)
        {
            logger?.Error(exception, "Lỗi khi lấy Mã Đăng ký lớn nhất. [DVHC: {DVHC}]", dvhc.MaDvhc);
            throw;
        }
    }

    /// <summary>
    /// Lấy danh sách mã Đăng ký chưa sử dụng theo Đơn Vị Hành Chính.
    /// </summary>
    /// <param name="dvhc">Đơn vị hành chính.</param>
    /// <param name="maDangKyStart">Mã Đăng ký nhỏ nhất cần lấy.</param>
    /// <param name="limit">Số lượng bản ghi tối đa cần lấy.</param>
    /// <returns>Danh sách Mã Thửa Đất chưa sử dụng.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    private async Task<SortedSet<long>> GetUnusedMaDangKyAsync(DvhcRecord dvhc, long? maDangKyStart = null,
        int limit = 100)
    {
        try
        {
            // Tạo câu lệnh SQL để lấy danh sách Mã Đăng ký chưa sử dụng
            const string queryDangKy = """
                                       SELECT DISTINCT dk.MaDangKy AS MaDangKy
                                       FROM DangKyQSDD dk
                                           INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
                                           INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                       WHERE tbd.MaDVHC = @MaDVHC
                                           AND dk.MaDangKy > @MaDangKyStart
                                           AND dk.MaDangKy < @MaxMaDangKyInDvhc
                                       """; // Lấy Mã Đăng ký từ bảng DangKy
            const string queryDangKyLs = """
                                         SELECT DISTINCT dk.MaDangKyLS AS MaDangKy
                                         FROM DangKyQSDDLS dk
                                             INNER JOIN ThuaDatLS td ON dk.MaThuaDatLS = td.MaThuaDatLS
                                             INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                         WHERE tbd.MaDVHC = @MaDVHC
                                             AND dk.MaDangKyLS > @MaDangKyStart
                                             AND dk.MaDangKyLS < @MaxMaDangKyInDvhc
                                         """; // Lấy Mã Đăng ký từ bảng DangKyLS
            const string queryDangKyLsOnThuaDat = """
                                                  SELECT DISTINCT dk.MaDangKyLS AS MaDangKy
                                                  FROM DangKyQSDDLS dk
                                                      INNER JOIN ThuaDat td ON dk.MaThuaDatLS = td.MaThuaDat
                                                      INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                                  WHERE tbd.MaDVHC = @MaDVHC
                                                      AND dk.MaDangKyLS > @MaDangKyStart
                                                      AND dk.MaDangKyLS < @MaxMaDangKyInDvhc
                                                  """; // Lấy Mã Đăng ký từ bảng DangKyLS ON ThuaDat
            const string query = $"""
                                  SELECT TOP (@Limit) MaDangKy
                                  FROM (
                                      {queryDangKy}
                                      UNION
                                      {queryDangKyLs}
                                      UNION
                                      {queryDangKyLsOnThuaDat}
                                  ) AS CombinedResults
                                  ORDER BY MaDangKy ASC;
                                  """; // Kết hợp kết quả từ 2 bảng DangKy và DangKyLS

            // Khởi tạo các tham số măc định
            SortedSet<long> result = [];
            maDangKyStart ??= dvhc.Ma.GetMinPrimaryKey() - 1;
            var maxMaDangKy = await GetMaxMaDangKyAsync(dvhc);
            var maxMaDangKyInDvhc = dvhc.Ma.GetMaxPrimaryKey();

            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();

            // Lặp lại cho đến khi lấy đủ số lượng bản ghi cần lấy
            while (result.Count == 0)
            {
                if (maDangKyStart >= maxMaDangKy)
                    return new SortedSet<long>(Enumerable.Range(1, limit).Select(i => maDangKyStart.Value + i));
                // Tạo tham số cho câu lệnh SQL
                var parameters = new
                {
                    MaDVHC = dvhc.MaDvhc,
                    MaDangKyStart = maDangKyStart,
                    MaxMaDangKyInDvhc = maxMaDangKyInDvhc,
                    Limit = limit
                };
                var usedMaDangKy = (await connection.QueryAsync<long>(query, parameters)).ToHashSet();
                if (usedMaDangKy.Count == 0)
                {
                    // Trả về danh sách có limit phần tử liên tục từ maDangKyStart
                    return new SortedSet<long>(Enumerable.Range(1, limit).Select(i => maDangKyStart.Value + i));
                }

                // Tìm các Mã Đăng ký chưa sử dụng
                var localMaDangKy = maDangKyStart.Value;
                var maxId = usedMaDangKy.Max();
                var count = (int)(maxId - localMaDangKy);
                var allMaDangKy = new SortedSet<long>(Enumerable.Range(1, count).Select(i => localMaDangKy + i));
                result = new SortedSet<long>(allMaDangKy.Except(usedMaDangKy));
                if (result.Count == 0)
                {
                    maDangKyStart = maxId;
                }
            }

            return result;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            logger?.Error(exception, "Lỗi khi lấy Mã Đăng ký chưa sử dụng. [DVHC: {DVHC}]", dvhc);
            throw;
        }
    }

    /// <summary>
    /// Lấy danh sách mã Đăng ký cần làm mới.
    /// </summary>
    /// <param name="dvhc">Đơn vị hành chính.</param>
    /// <param name="minMaDangKy">Mã Đăng ký nhỏ nhất cần lấy.</param>
    /// <param name="maDangKyTemp">Mã Đăng ký tạm thời. Mặc định: <see cref="DefaultMaDangKyTemp"/>.</param>
    /// <param name="limit">Số lượng bản ghi tối đa cần lấy.</param>
    /// <returns>Danh sách Mã Đăng ký cần làm mới.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    private async Task<IEnumerable<long>> GetMaDangKyNeedRenewAsync(DvhcRecord dvhc, long? minMaDangKy = null,
        long maDangKyTemp = DefaultMaDangKyTemp, int limit = 100)
    {
        try
        {
            // Khơi tạo các tham số mặc định
            var minMaInDvhc = dvhc.Ma.GetMinPrimaryKey();
            minMaDangKy ??= long.MinValue;
            // Câu lệnh SQL để lấy danh sách Mã Đăng ký cần làm mới
            var queryDangKy = $"""
                               SELECT DISTINCT dk.MaDangKy AS MaDangKy
                               FROM DangKyQSDD dk
                                   INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
                                   INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                               WHERE tbd.MaDVHC = @MaDVHC
                                   AND dk.MaDangKy > @MinMaDangKy
                                   AND dk.MaDangKy <> @MaDangKyTemp
                                   {(minMaDangKy < minMaInDvhc ? "AND dk.MaDangKy < @MinMaInDvhc" : "")}
                               """; // Lấy Mã Đăng ký từ bảng DangKy
            var queryDangKyLs = $"""
                                 SELECT DISTINCT dk.MaDangKyLS AS MaDangKy
                                 FROM DangKyQSDDLS dk
                                     INNER JOIN ThuaDatLS td ON dk.MaThuaDatLS = td.MaThuaDatLS
                                     INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                 WHERE tbd.MaDVHC = @MaDVHC
                                     AND dk.MaDangKyLS > @MinMaDangKy
                                     AND dk.MaDangKyLS <> @MaDangKyTemp
                                     {(minMaDangKy < minMaInDvhc ? "AND dk.MaDangKyLS < @MinMaInDvhc" : "")}
                                 """; // Lấy Mã Đăng ký từ bảng DangKyLS
            var queryDangKyLsOnThuaDat = $"""
                                          SELECT DISTINCT dk.MaDangKyLS AS MaDangKy
                                          FROM DangKyQSDDLS dk
                                              INNER JOIN ThuaDat td ON dk.MaThuaDatLS = td.MaThuaDat
                                              INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                          WHERE tbd.MaDVHC = @MaDVHC
                                              AND dk.MaDangKyLS > @MinMaDangKy
                                              AND dk.MaDangKyLS <> @MaDangKyTemp
                                          {(minMaDangKy < minMaInDvhc ? "AND dk.MaDangKyLS < @MinMaInDvhc" : "")}
                                          """; // Lấy Mã Đăng ký từ bảng DangKyLS ON ThuaDat
            var query = $"""
                         SELECT TOP (@Limit) MaDangKy
                         FROM (
                             {queryDangKy}
                             UNION 
                             {queryDangKyLs}
                             UNION
                             {queryDangKyLsOnThuaDat}
                         ) AS CombinedResult
                         ORDER BY MaDangKy {(minMaDangKy >= minMaInDvhc ? "DESC" : "ASC")};
                         """; // Kết hợp kết quả từ 2 bảng DangKy và DangKyLS

            // Tạo tham số cho câu lệnh SQL
            var parameters = new
            {
                Limit = limit,
                MaDVHC = dvhc.MaDvhc,
                MinMaDangKy = minMaDangKy,
                MaDangKyTemp = maDangKyTemp,
                MinMaInDvhc = minMaInDvhc
            };
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();
            // Thực thi câu lệnh SQL
            return await connection.QueryAsync<long>(query, parameters);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            logger?.Error(e, "Lỗi khi lấy Mã Đăng ký cần làm mới. [DVHC: {DVHC}]", dvhc);
            throw;
        }
    }

    /// <summary>
    /// Làm mới Mã Đăng ký.
    /// </summary>
    /// <param name="capXaSau">Bản ghi cấp xã sau khi sáp nhập.</param>
    /// <param name="maDangKyTemp">Mã Thửa Đất tạm thời. (<see cref="DefaultMaDangKyTemp"/></param>)
    /// <param name="maThuaDatTemp">
    /// Mã Tờ Bản Đồ tạm thời. (<see cref="ThuaDatRepository.DefaultMaThuaDatTemp"/>)
    /// </param>
    /// <param name="limit">Số lượng giới hạn kết quả trả về.</param>
    /// <returns>Task bất đồng bộ.</returns>
    /// <exception cref="OverflowException">Ném ra ngoại lệ khi số lượng Tờ Bản Đồ đã đạt giới hạn tối đa.</exception>
    /// <exception cref="Exception">Ném ra ngoại lệ khi có lỗi xảy ra trong quá trình cập nhật.</exception>
    public async Task<long> RenewMaDangKyAsync(DvhcRecord capXaSau, long maDangKyTemp = DefaultMaDangKyTemp,
        long maThuaDatTemp = ThuaDatRepository.DefaultMaThuaDatTemp, int limit = 100)
    {
        try
        {
            // Kiểm tra xem có cần cập nhật không
            if (!await CheckOverflowAsync(capXaSau))
            {
                logger?.Error("Số lượng Đăng ký của Đơn Vị Hành Chính vượt quá giới hạn.");
                throw new OverflowException("Số lượng Đăng ký của Đơn Vị Hành Chính vượt quá giới hạn.");
            }

            // Khởi tạo các tham số mặc định
            maDangKyTemp = await CreateTempDangKyAsync(maDangKyTemp, maThuaDatTemp);

            // Mã Đăng ký nhỏ nhất cần lấy
            long? startId = null;
            // Danh sách mã đăng ký chưa sử dụng
            var unusedIds = new Queue<long>();
            // Mã Đăng ký cần làm mới
            long? newMaDangKy = null;
            // Mã đăng ký nhỏ nhất của Đơn Vị Hành Chính
            var minMaDangKyInDvhc = capXaSau.Ma.GetMinPrimaryKey();

            // Các Câu lệnh SQL để cập nhật thông tin
            const string queryUpdateGiayChungNhan = """
                                                    UPDATE GCNQSDD
                                                        SET MaDangKy = @MaDangKyTemp
                                                        WHERE MaDangKy = @OldMaDangKy;
                                                    UPDATE GCNQSDDLS
                                                        SET  MaDangKyLS = @MaDangKyTemp
                                                        WHERE MaDangKyLS = @OldMaDangKy;
                                                    """;
            const string queryUpdateCayLichSu = """
                                                UPDATE CayLS
                                                    SET MaDangKyHT = @MaDangKyTemp
                                                    WHERE MaDangKyHT = @OldMaDangKy;
                                                UPDATE CayLS
                                                    SET MaDangKyLS = @MaDangKyTemp
                                                    WHERE MaDangKyLS = @OldMaDangKy;
                                                """;
            const string queryUpdateOtherTemp = """
                                                UPDATE NguoiDaiDien
                                                    SET MaDangKy = @MaDangKyTemp
                                                    WHERE MaDangKy = @OldMaDangKy;
                                                UPDATE TaiSan_ThuaDat
                                                  SET MaDangKy = @MaDangKyTemp
                                                  WHERE MaDangKy = @OldMaDangKy;
                                                """;
            const string queryUpdateDangKy = """
                                             UPDATE DangKyQSDD
                                                 SET MaDangKy = @NewMaDangKy
                                                 WHERE MaDangKy = @OldMaDangKy;

                                             UPDATE DangKyQSDDLS
                                                SET MaDangKyLS = @NewMaDangKy
                                                WHERE MaDangKyLS = @OldMaDangKy;
                                             """;
            const string queryUpdateDangKyTheChap = """
                                                    UPDATE DangKy_TheChap
                                                        SET MaDangKy = @NewMaDangKy
                                                        WHERE MaDangKy = @OldMaDangKy;
                                                    """;
            const string queryUpdateDangKyGopVon = """
                                                   UPDATE DangKy_GopVon
                                                       SET MaDangKy = @NewMaDangKy
                                                       WHERE MaDangKy = @OldMaDangKy;
                                                   """; // Cập nhật thông tin trong các bảng liên quan
            const string queryUpdateGiayChungNhanNew = """
                                                       UPDATE GCNQSDD
                                                           SET MaDangKy = @NewMaDangKy
                                                           WHERE MaDangKy = @MaDangKyTemp;
                                                       UPDATE GCNQSDDLS
                                                           SET  MaDangKyLS = @NewMaDangKy
                                                           WHERE MaDangKyLS = @MaDangKyTemp;
                                                       """;
            const string queryUpdateCayLichSuNew = """
                                                   UPDATE CayLS
                                                       SET MaDangKyHT = @NewMaDangKy
                                                       WHERE MaDangKyHT = @MaDangKyTemp;
                                                   UPDATE CayLS
                                                        SET MaDangKyLS = @NewMaDangKy
                                                        WHERE MaDangKyLS = @MaDangKyTemp;
                                                   """;
            const string queryUpdateOtherNew = """
                                               UPDATE NguoiDaiDien
                                                   SET MaDangKy = @NewMaDangKy
                                                   WHERE MaDangKy = @MaDangKyTemp;
                                               UPDATE TaiSan_ThuaDat
                                                 SET MaDangKy = @NewMaDangKy
                                                 WHERE MaDangKy = @MaDangKyTemp;
                                               """;
            const string queryUpdateOther = """
                                            UPDATE DangKyBienDong
                                                SET MaDangKy = @NewMaDangKy
                                                WHERE MaDangKy = @OldMaDangKy;
                                            UPDATE GCNQSDNha
                                                SET MaDangKy = @NewMaDangKy
                                                WHERE MaDangKy = @OldMaDangKy;
                                            UPDATE tblSoHong
                                                SET MaDangKy = @NewMaDangKy
                                                WHERE MaDangKy = @OldMaDangKy;
                                            UPDATE TD_KNLichSu
                                                SET MaDangKy = @NewMaDangKy
                                                WHERE MaDangKy = @OldMaDangKy;
                                            UPDATE TD_LichSu
                                                SET MaDangKy = @NewMaDangKy
                                                WHERE MaDangKy = @OldMaDangKy;
                                            """;
            const string queryUpdate = $"""
                                        {queryUpdateGiayChungNhan}
                                        {queryUpdateCayLichSu}
                                        {queryUpdateOtherTemp}
                                        {queryUpdateDangKy}
                                        {queryUpdateDangKyTheChap}
                                        {queryUpdateDangKyGopVon}
                                        {queryUpdateGiayChungNhanNew}
                                        {queryUpdateCayLichSuNew}
                                        {queryUpdateOtherNew}
                                        {queryUpdateOther}
                                        """;
            // Lấy thông tin kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();

            // Xóa Giấy Chứng Nhận và Cây Lịch Sử có Mã Đăng ký tạm thời
            await GiayChungNhanRepository.DeleteGiayChungNhanByMaDangKyAsync(connection, maDangKyTemp, logger);
            await DeleteCayLichSuByMaDangKyAsync(connection, maDangKyTemp, logger);
            // Xóa các dữ liệu tạm khác:
            await DeleteOtherByMaDangKyAsync(connection, maDangKyTemp, logger);
            while (true)
            {
                // Lấy danh sách Mã Đăng ký cần làm mới
                var maDangKyNeedRenew =
                    (await GetMaDangKyNeedRenewAsync(capXaSau, startId, maDangKyTemp, limit)).ToHashSet();
                foreach (var oldMaDangKy in maDangKyNeedRenew)
                {
                    if (unusedIds.Count == 0)
                    {
                        unusedIds = new Queue<long>(await GetUnusedMaDangKyAsync(capXaSau, newMaDangKy, limit));
                    }

                    newMaDangKy = unusedIds.Dequeue();
                    if (newMaDangKy >= oldMaDangKy && oldMaDangKy >= minMaDangKyInDvhc)
                    {
                        maDangKyNeedRenew = [];
                        break;
                    }

                    // Tạo tham số cho câu lệnh SQL
                    var parameters = new
                    {
                        NewMaDangKy = newMaDangKy,
                        OldMaDangKy = oldMaDangKy,
                        MaDangKyTemp = maDangKyTemp
                    };
                    // Sử dụng giao dịch để đảm bảo tính nhất quán
                    if (connection.State == ConnectionState.Closed)
                    {
                        await connection.OpenAsync();
                    }

                    await using var transaction = connection.BeginTransaction();
                    try
                    {
                        // Thực thi câu lệnh SQL trong giao dịch
                        await connection.ExecuteAsync(queryUpdate, parameters, transaction: transaction);
                        // Commit giao dịch nếu thành công
                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        // Rollback giao dịch nếu có lỗi
                        await transaction.RollbackAsync();
                        Console.WriteLine(e);
                        logger?.Error(e,
                            "Lỗi khi cập nhật Mã Đăng ký. [OldMaDangKy: {OldMaDangKy}, NewMaDangKy: {NewMaDangKy}]",
                            oldMaDangKy, newMaDangKy);
                        throw;
                    }
                }

                // Nếu không còn Mã Đăng ký cần làm mới hoặc đã hết
                if (maDangKyNeedRenew.Count == 0 && startId >= minMaDangKyInDvhc)
                    break;

                // Lấy mã Đăng ký nhỏ nhất cần lấy tiếp theo
                startId = maDangKyNeedRenew.Count > 0 ? maDangKyNeedRenew.Max() : minMaDangKyInDvhc;
                startId = startId <= minMaDangKyInDvhc ? startId : newMaDangKy + 1;
            }

            return maDangKyTemp;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            logger?.Error(exception, "Lỗi khi Làm mới Mã Đăng ký. [DVHC: {DVHC}]", capXaSau);
            throw;
        }
    }

    /// <summary>
    /// Xóa Đăng ký tạm thời.
    /// </summary>
    /// <param name="dbConnection">Kết nối cơ sở dữ liệu.</param>
    /// <param name="maDangKyTemp">Mã Đăng ký tạm thời.</param>
    /// <param name="logger">Ghi log.</param>
    public static async Task DeleteDangKyTempAsync(SqlConnection dbConnection,
        long maDangKyTemp = DefaultMaDangKyTemp, ILogger? logger = null)
    {
        try
        {
            // Câu lệnh SQL để xóa thông tin Đăng ký tạm thời
            const string query = """
                                 DELETE FROM DangKyQSDD WHERE MaDangKy = @MaDangKyTemp;
                                 DELETE FROM DangKyQSDDLS WHERE MaDangKyLS = @MaDangKyTemp;
                                 """;
            // Tạo tham số cho câu lệnh SQL
            var param = new { MaDangKyTemp = maDangKyTemp };
            // Thực thi câu lệnh SQL
            await dbConnection.ExecuteAsync(query, param);
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi xóa Đăng ký tạm thời. [MaDangKyTemp: {MaDangKyTemp}]", maDangKyTemp);
            throw;
        }
    }

    /// <summary>
    /// Xóa Đăng ký theo Mã Thửa Đất.
    /// </summary>
    /// <param name="dbConnection">Kết nối cơ sở dữ liệu.</param>
    /// <param name="maThuaDat">Mã Thửa Đất. Mặc định: <see cref="ThuaDatRepository.DefaultMaThuaDatTemp"/>.</param>
    /// <param name="logger">Ghi log.</param>
    /// <returns>Task bất đồng bộ.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình xóa.</exception>
    public static async Task DeleteDangKyByMaThuaDatAsync(SqlConnection dbConnection,
        long maThuaDat = ThuaDatRepository.DefaultMaThuaDatTemp, ILogger? logger = null)
    {
        try
        {
            // Câu lệnh SQL để xóa thông tin Đăng ký theo Mã Thửa Đất
            const string query = """
                                 DELETE FROM DangKyQSDD WHERE MaThuaDat = @MaThuaDat;
                                 DELETE FROM DangKyQSDDLS WHERE MaThuaDatLS = @MaThuaDat;
                                 """;
            // Tạo tham số cho câu lệnh SQL
            var param = new { MaThuaDat = maThuaDat };
            // Thực thi câu lệnh SQL
            await dbConnection.ExecuteAsync(query, param);
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi xóa Đăng ký theo Mã Thửa Đất. [MaThuaDat: {MaThuaDat}]", maThuaDat);
            throw;
        }
    }

    /// <summary>
    /// Xóa thông tin Cây Lịch Sử theo Mã Đăng Ký.
    /// </summary>
    /// <param name="dbConnection">Kết nối cơ sở dữ liệu.</param>
    /// <param name="maDangKy">Mã Đăng Ký. Mặc định: <see cref="DefaultMaDangKyTemp"/>.</param>
    /// <param name="logger">Ghi log.</param>
    /// <returns>Task bất đồng bộ.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình xóa.</exception>
    public static async Task DeleteCayLichSuByMaDangKyAsync(SqlConnection dbConnection,
        long maDangKy = DefaultMaDangKyTemp, ILogger? logger = null)
    {
        try
        {
            // Câu lệnh SQL để xóa thông tin Đăng ký theo Mã Thửa Đất
            const string query = "DELETE FROM CayLS WHERE MaDangKyHT = @MaDangKy OR MaDangKyLS = @MaDangKy;";
            // Tạo tham số cho câu lệnh SQL
            var param = new { MaDangKy = maDangKy };
            // Thực thi câu lệnh SQL
            await dbConnection.ExecuteAsync(query, param);
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi xóa Cây lịch sử. [MaDangKy: {MaDangKy}]", maDangKy);
            throw;
        }
    }

    public static async Task DeleteOtherByMaDangKyAsync(SqlConnection dbConnection,
        long maDangKy = DefaultMaDangKyTemp, ILogger? logger = null)
    {
        try
        {
            // Câu lệnh SQL để xóa thông tin tạm khác theo Mã Đăng Ký
            const string query = """
                                 DELETE FROM NguoiDaiDien WHERE MaDangKy = @MaDangKy;
                                 DELETE FROM TaiSan_ThuaDat WHERE MaDangKy = @MaDangKy;
                                 """;
            // Tạo tham số cho câu lệnh SQL
            var param = new { MaDangKy = maDangKy };
            // Thực thi câu lệnh SQL
            await dbConnection.ExecuteAsync(query, param);
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi xóa Các thông tin khác theo Mã thửa đất. [MaDangKy: {MaDangKy}]", maDangKy);
            throw;
        }
    }
}