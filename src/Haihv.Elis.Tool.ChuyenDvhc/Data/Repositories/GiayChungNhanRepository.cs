using System.Data;
using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.Data.SqlClient;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

public class GiayChungNhanRepository(string connectionString, ILogger? logger = null)
{
    #region Cập nhật ghi chú vào Giấy chứng nhận

    /// <summary>
    /// Cập nhật ghi chú vào Giấy chứng nhận
    /// </summary>
    /// <param name="thuaDatCapNhats">Danh sách Thửa đất cần cập nhật ghi chú.</param>
    /// <param name="formatGhiChu">Định dạng ghi chú.</param>
    /// <param name="ngaySapNhap">Ngày sáp nhập.</param>
    /// <returns></returns>
    public async Task<bool> UpdateGhiChuGiayChungNhan(List<ThuaDatCapNhat> thuaDatCapNhats, string? formatGhiChu = null,
        string? ngaySapNhap = null)
    {
        if (thuaDatCapNhats.Count == 0)
            return false;

        if (string.IsNullOrWhiteSpace(formatGhiChu))
            formatGhiChu = ThamSoThayThe.DefaultGhiChuGiayChungNhan;

        if (string.IsNullOrWhiteSpace(ngaySapNhap))
            ngaySapNhap = DateTime.Now.ToString(ThamSoThayThe.DinhDangNgaySapNhap);
        await using var connection = connectionString.GetConnection();
        try
        {
            const string sql = """
                               UPDATE GCNQSDD
                               SET GhiChu = @GhiChu
                               FROM GCNQSDD
                               INNER JOIN DangKyQSDD ON GCNQSDD.MaDangKy = DangKyQSDD.MaDangKy
                               INNER JOIN ThuaDat ON DangKyQSDD.MaThuaDat = ThuaDat.MaThuaDat
                               WHERE ThuaDat.MaThuaDat = @MaThuaDat;
                               """;


            foreach (var thuaDatCapNhat in thuaDatCapNhats)
            {
                var ghiChu = formatGhiChu
                    .Replace(ThamSoThayThe.ToBanDo, thuaDatCapNhat.ToBanDo)
                    .Replace(ThamSoThayThe.DonViHanhChinh, thuaDatCapNhat.TenDonViHanhChinh)
                    .Replace(ThamSoThayThe.NgaySapNhap, ngaySapNhap);
                await connection.ExecuteAsync(sql, new { thuaDatCapNhat.MaThuaDat, GhiChu = ghiChu });
            }

            return true;
        }
        catch (Exception ex)
        {
            if (logger == null) throw;
            logger.Error(ex, "Lỗi khi cập nhật Ghi Chú Giấy Chứng Nhận.");
            return false;
        }
    }

    #endregion

    #region Làm mới mã Giấy chứng nhận

    private const long DefaultMaGiayChungNhanTemp = 0;

    /// <summary>
    /// Kiểm tra xem số lượng Giấy chứng nhận theo Đơn Vị Hành Chính có vượt quá giới hạn không.
    /// </summary>
    /// <param name="dvhc">Đơn vị hành chính.</param>
    /// <returns>Trả về true nếu không vượt quá giới hạn, ngược lại trả về false.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    private async Task<bool> CheckOverflowAsync(DvhcRecord dvhc)
    {
        // Lấy tổng số lượng Giấy chứng nhận theo Đơn Vị Hành Chính
        const string query = """
                             SELECT COUNT(DISTINCT MaGCN) AS Total
                             FROM (
                                 SELECT DISTINCT gcn.MaGCN AS MaGCN
                                 FROM GCNQSDD gcn
                                          INNER JOIN DangKyQSDD dk ON gcn.MaDangKy = dk.MaDangKy
                                          INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
                                          INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                 WHERE tbd.MaDVHC = @MaDVHC
                                 UNION
                                 SELECT DISTINCT gcn.MaGCNLS AS MaGCN
                                 FROM GCNQSDDLS gcn
                                          INNER JOIN DangKyQSDD dk ON gcn.MaDangKyLS = dk.MaDangKy
                                          INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
                                          INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                 WHERE tbd.MaDVHC = @MaDVHC
                                 UNION
                                 SELECT DISTINCT gcn.MaGCNLS AS MaGCN
                                 FROM GCNQSDDLS gcn
                                          INNER JOIN DangKyQSDDLS dk ON gcn.MaDangKyLS = dk.MaDangKyLS
                                          INNER JOIN ThuaDatLS td ON dk.MaThuaDatLS = td.MaThuaDatLS
                                          INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                 WHERE tbd.MaDVHC = @MaDVHC
                                 UNION
                                 SELECT DISTINCT gcn.MaGCNLS AS MaGCN
                                 FROM GCNQSDDLS gcn
                                          INNER JOIN DangKyQSDDLS dk ON gcn.MaDangKyLS = dk.MaDangKyLS
                                          INNER JOIN ThuaDat td ON dk.MaThuaDatLS = td.MaThuaDat
                                          INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                 WHERE tbd.MaDVHC = @MaDVHC
                                 ) AS CombinedResult
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
            logger?.Error(e, "Lỗi khi kiểm tra vượt quá giới hạn Giấy chứng nhận. [MaDVHC: {MaDVHC}]", dvhc.MaDvhc);
            return false;
        }
    }

    /// <summary>
    /// Tạo Giấy chứng nhận tạm thời.
    /// </summary>
    /// <param name="maGiayChungNhanTemp">Mã Giấy chứng nhận tạm thời.
    /// Mặc định: <see cref="DefaultMaGiayChungNhanTemp"/>
    /// </param>
    /// <param name="maDangKyTemp">Mã Đăng ký tạm thời.
    /// Mặc định: <see cref="DangKyThuaDatRepository.DefaultMaDangKyTemp"/>
    /// </param>
    /// <param name="reCreateTempDangKy">Tạo lại Đăng ký tạm thời.</param>
    /// <returns>Trả về mã Giấy chứng nhận tạm thời.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    private async Task<long> CreateTempGiayChungNhanAsync(long maGiayChungNhanTemp = DefaultMaGiayChungNhanTemp,
        long maDangKyTemp = DangKyThuaDatRepository.DefaultMaDangKyTemp, bool reCreateTempDangKy = false)
    {
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();

            // Tạo Chủ sử dụng tạm thời

            var maChuSuDungTemp = await new ChuSuDungRepository(connectionString, logger)
                .CreateChuSuDungTempAsync();

            // Tạo Đăng ký tạm thời nếu cần
            if (reCreateTempDangKy && maDangKyTemp == DangKyThuaDatRepository.DefaultMaDangKyTemp)
                maDangKyTemp = await new DangKyThuaDatRepository(connectionString, logger)
                    .CreateTempDangKyAsync(reCreateTempThuaDat: true);

            // Tạo câu lệnh SQL để tạo hoặc cập nhật Giấy chứng nhận tạm thời
            const string queryGiayChungNhan = """
                                              IF NOT EXISTS (SELECT 1 FROM GCNQSDD WHERE MaGCN = @MaGCN)
                                              BEGIN
                                                  INSERT INTO GCNQSDD (MaGCN, MaDangKy, MaChuSuDung, GhiChu)
                                                  VALUES (@MaGCN, @MaDangKy, @MaChuSuDung, @GhiChu)
                                              END
                                              ELSE
                                              BEGIN
                                                  UPDATE GCNQSDD
                                                  SET MaDangKy = @MaDangKy, MaChuSuDung=MaChuSuDung, GhiChu = @GhiChu
                                                  WHERE MaGCN = @MaGCN
                                              END;
                                              """;
            const string queryGiayChungNhanLichSu = """
                                                    IF NOT EXISTS (SELECT 1 FROM GCNQSDDLS WHERE MaGCNLS = @MaGCN)
                                                    BEGIN
                                                        INSERT INTO GCNQSDDLS (MaGCNLS, MaDangKyLS, MaChuSuDungLS, GhiChu)
                                                        VALUES (@MaGCN, @MaDangKy, @MaChuSuDung, @GhiChu)
                                                    END
                                                    ELSE
                                                    BEGIN
                                                        UPDATE GCNQSDDLS
                                                        SET MaDangKyLS = @MaDangKy, MaChuSuDungLS = @MaChuSuDung, GhiChu = @GhiChu
                                                        WHERE MaGCNLS = @MaGCN
                                                    END;
                                                    """;
            const string query = $"""
                                  {queryGiayChungNhan}
                                  {queryGiayChungNhanLichSu}
                                  """;

            // Khởi tạo tham số cho câu lệnh SQL
            var parameters = new
            {
                MaGCN = maGiayChungNhanTemp,
                MaDangKy = maDangKyTemp,
                MaChuSuDung = maChuSuDungTemp,
                GhiChu = "Giấy chứng nhận tạm thời."
            };
            if (connection.State == ConnectionState.Closed)
            {
                await connection.OpenAsync();
            }

            // Thực thi câu lệnh SQL
            await using var transaction = connection.BeginTransaction();
            try
            {
                await connection.ExecuteAsync(query, parameters, transaction);
                await transaction.CommitAsync();
                return maGiayChungNhanTemp;
            }
            catch (Exception e)
            {
                // Rollback transaction
                await transaction.RollbackAsync();
                Console.WriteLine(e);
                logger?.Error(e, "Lỗi khi tạo Giấy chứng nhận tạm thời.");
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            logger?.Error(ex, "Lỗi khi tạo Giấy chứng nhận tạm thời.");
            throw;
        }
    }

    /// <summary>
    /// Lấy mã Giấy chứng nhận lớn nhất.
    /// </summary>
    /// <param name="dvhc">Đơn vị hành chính.</param>
    /// <returns>Trả về mã Giấy chứng nhận lớn nhất.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    private async Task<long> GetMaxMaGiayChungNhanAsync(DvhcRecord dvhc)
    {
        // Tạo câu lệnh SQL để lấy mã Giấy chứng nhận lớn nhất
        const string queryGiayChungNhan = """
                                          SELECT ISNULL(MAX(gcn.MaGCN), 0) AS MaGCN
                                          FROM GCNQSDD gcn
                                               INNER JOIN DangKyQSDD dk ON gcn.MaDangKy = dk.MaDangKy
                                               INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
                                               INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                          WHERE tbd.MaDVHC = @MaDVHC
                                             AND gcn.MaGCN > @MinMaGCN
                                             AND gcn.MaGCN < @MaxMaGCN
                                          """;
        const string queryGiayChungNhanLichSuOnDangKyLs = """
                                                          SELECT ISNULL(MAX(gcn.MaGCNLS), 0) AS MaGCN
                                                          FROM GCNQSDDLS gcn
                                                                   INNER JOIN DangKyQSDDLS dk ON gcn.MaDangKyLS = dk.MaDangKyLS
                                                                   INNER JOIN ThuaDatLS td ON dk.MaThuaDatLS = td.MaThuaDatLS
                                                                   INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                                          WHERE tbd.MaDVHC = @MaDVHC
                                                              AND gcn.MaGCNLS > @MinMaGCN
                                                              AND gcn.MaGCNLS < @MaxMaGCN
                                                          """;
        const string queryGiayChungNhanLichSuOnDangKy = """
                                                        SELECT ISNULL(MAX(gcn.MaGCNLS), 0) AS MaGCN
                                                        FROM GCNQSDDLS gcn
                                                                 INNER JOIN DangKyQSDD dk ON gcn.MaDangKyLS = dk.MaDangKy
                                                                 INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
                                                                 INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                                        WHERE tbd.MaDVHC = @MaDVHC
                                                           AND gcn.MaGCNLS > @MinMaGCN
                                                           AND gcn.MaGCNLS < @MaxMaGCN
                                                        """;

        const string queryGiayChungNhanLichSuOnDangKyLsOnThuaDat = """
                                                                   SELECT ISNULL(MAX(gcn.MaGCNLS), 0) AS MaGCN
                                                                   FROM GCNQSDDLS gcn
                                                                            INNER JOIN DangKyQSDDLS dk ON gcn.MaDangKyLS = dk.MaDangKyLS
                                                                            INNER JOIN ThuaDat td ON dk.MaThuaDatLS = td.MaThuaDat
                                                                            INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                                                   WHERE tbd.MaDVHC = @MaDVHC
                                                                      AND gcn.MaGCNLS > @MinMaGCN
                                                                      AND gcn.MaGCNLS < @MaxMaGCN
                                                                   """;
        const string query = $"""
                              SELECT MAX(MaGCN)
                              FROM (
                                     {queryGiayChungNhan}
                                  UNION
                                     {queryGiayChungNhanLichSuOnDangKyLs}
                                  UNION
                                     {queryGiayChungNhanLichSuOnDangKy}
                                  UNION
                                     {queryGiayChungNhanLichSuOnDangKyLsOnThuaDat}
                                  ) AS CombinedResult
                              """;
        // Khởi tạo tham số cho câu lệnh SQL
        var parameters = new
        {
            MaDVHC = dvhc.MaDvhc,
            MinMaGCN = dvhc.Ma.GetMinPrimaryKey(),
            MaxMaGCN = dvhc.Ma.GetMaxPrimaryKey()
        };
        try
        {
            await using var connection = connectionString.GetConnection();
            return await connection.ExecuteScalarAsync<long>(query, parameters);
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi lấy mã Giấy chứng nhận lớn nhất.");
            throw;
        }
    }

    /// <summary>
    /// Lấy mã Giấy chứng nhận chưa sử dụng.
    /// </summary>
    /// <param name="dvhc">Đơn vị hành chính.</param>
    /// <param name="maGiayChungNhanStart">Mã Giấy chứng nhận bắt đầu.</param>
    /// <param name="limit">Số lượng mã cần lấy.</param>
    /// <returns>Trả về danh sách mã Giấy chứng nhận chưa sử dụng.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    private async Task<SortedSet<long>> GetUnusedMaGiayChungNhanAsync(DvhcRecord dvhc,
        long? maGiayChungNhanStart = null, int limit = 100)
    {
        // Tạo câu lệnh SQL để lấy mã Giấy chứng nhận chưa sử dụng
        const string queryGiayChungNhan = """
                                          SELECT DISTINCT gcn.MaGCN  AS MaGCN
                                          FROM GCNQSDD gcn
                                               INNER JOIN DangKyQSDD dk ON gcn.MaDangKy = dk.MaDangKy
                                               INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
                                               INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                          WHERE tbd.MaDVHC = @MaDVHC
                                             AND gcn.MaGCN > @MaGiayChungNhanStart
                                             AND gcn.MaGCN < @MaGiayChungNhanInDvhc
                                          """;
        const string queryGiayChungNhanLichSuOnDangKyLs = """
                                                          SELECT DISTINCT gcn.MaGCNLS  AS MaGCN
                                                          FROM GCNQSDDLS gcn
                                                                   INNER JOIN DangKyQSDDLS dk ON gcn.MaDangKyLS = dk.MaDangKyLS
                                                                   INNER JOIN ThuaDatLS td ON dk.MaThuaDatLS = td.MaThuaDatLS
                                                                   INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                                          WHERE tbd.MaDVHC = @MaDVHC
                                                             AND gcn.MaGCNLS > @MaGiayChungNhanStart
                                                             AND gcn.MaGCNLS < @MaGiayChungNhanInDvhc
                                                          """;
        const string queryGiayChungNhanLichSuOnDangKy = """
                                                        SELECT DISTINCT gcn.MaGCNLS  AS MaGCN
                                                        FROM GCNQSDDLS gcn
                                                                 INNER JOIN DangKyQSDD dk ON gcn.MaDangKyLS = dk.MaDangKy
                                                                 INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
                                                                 INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                                        WHERE tbd.MaDVHC = @MaDVHC
                                                          AND gcn.MaGCNLS > @MaGiayChungNhanStart
                                                          AND gcn.MaGCNLS < @MaGiayChungNhanInDvhc
                                                        """;

        const string queryGiayChungNhanLichSuOnDangKyLsOnThuaDat = """
                                                                   SELECT DISTINCT gcn.MaGCNLS  AS MaGCN
                                                                   FROM GCNQSDDLS gcn
                                                                            INNER JOIN DangKyQSDDLS dk ON gcn.MaDangKyLS = dk.MaDangKyLS
                                                                            INNER JOIN ThuaDat td ON dk.MaThuaDatLS = td.MaThuaDat
                                                                            INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                                                   WHERE tbd.MaDVHC = @MaDVHC
                                                                     AND gcn.MaGCNLS > @MaGiayChungNhanStart
                                                                     AND gcn.MaGCNLS < @MaGiayChungNhanInDvhc
                                                                   """;
        const string query = $"""
                              SELECT TOP (@Limit) MaGCN
                              FROM (
                                     {queryGiayChungNhan}
                                  UNION 
                                     {queryGiayChungNhanLichSuOnDangKyLs}
                                  UNION 
                                     {queryGiayChungNhanLichSuOnDangKy}
                                  UNION 
                                     {queryGiayChungNhanLichSuOnDangKyLsOnThuaDat}
                                  ) AS CombinedResult
                              ORDER BY MaGCN ASC;  
                              """;
        // Khởi tọa các tham số mặc định:
        SortedSet<long> result = [];
        maGiayChungNhanStart ??= dvhc.Ma.GetMinPrimaryKey();
        var maGiayChungNhanInDvhc = dvhc.Ma.GetMaxPrimaryKey();
        var maGiayChungNhanMax = await GetMaxMaGiayChungNhanAsync(dvhc);
        try
        {
            await using var connection = connectionString.GetConnection();

            // Lặp lại cho đến khi lấy đủ số lượng mã cần lấy
            while (result.Count == 0)
            {
                if (maGiayChungNhanStart >= maGiayChungNhanMax)
                    return new SortedSet<long>(Enumerable.Range(1, limit).Select(i => maGiayChungNhanMax + i));

                // Tạo tham số cho câu lệnh SQL
                var parameters = new
                {
                    MaDVHC = dvhc.MaDvhc,
                    MaGiayChungNhanStart = maGiayChungNhanStart,
                    MaGiayChungNhanInDvhc = maGiayChungNhanInDvhc,
                    Limit = limit
                };
                var usedMaGiayChungNhan = (await connection.QueryAsync<long>(query, parameters)).ToHashSet();
                if (usedMaGiayChungNhan.Count == 0)
                    return new SortedSet<long>(Enumerable.Range(1, limit).Select(i => maGiayChungNhanStart.Value + i));

                // Tìm mã Giấy chứng nhận chưa sử dụng
                var localMaGiayChungNhanStart = maGiayChungNhanStart.Value;
                var maxId = usedMaGiayChungNhan.Max();
                var count = (int)(maxId - localMaGiayChungNhanStart);
                var allMaGiayChungNhan =
                    new SortedSet<long>(Enumerable.Range(1, count).Select(i => localMaGiayChungNhanStart + i));
                result = new SortedSet<long>(allMaGiayChungNhan.Except(usedMaGiayChungNhan));
                if (result.Count == 0)
                    maGiayChungNhanStart = maxId;
            }

            return result;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            logger?.Error(exception, "Lỗi khi lấy mã Giấy chứng nhận chưa sử dụng.");
            throw;
        }
    }

    /// <summary>
    /// Lấy mã Giấy chứng nhận cần làm mới.
    /// </summary>
    /// <param name="dvhc">Đơn vị hành chính.</param>
    /// <param name="minGiayChungNhanStart">Mã Giấy chứng nhận bắt đầu.</param>
    /// <param name="maGiayChungNhanTemp">Mã Giấy chứng nhận tạm thời.</param>
    /// <param name="limit">Số lượng mã cần lấy.</param>
    /// <returns>Trả về danh sách mã Giấy chứng nhận cần làm mới.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    private async Task<IEnumerable<long>> GetMaGiayChungNhanNeedRenewAsync(DvhcRecord dvhc,
        long? minGiayChungNhanStart = null, long maGiayChungNhanTemp = DefaultMaGiayChungNhanTemp, int limit = 100)
    {
        // Khơi tạo các tham số mặc định
        var minMaGiayChungNhanInDvhc = dvhc.Ma.GetMinPrimaryKey();
        minGiayChungNhanStart ??= long.MinValue;
        // Tạo câu lệnh SQL để lấy mã Giấy chứng nhận chưa sử dụng
        var queryGiayChungNhan = $"""
                                  SELECT DISTINCT gcn.MaGCN  AS MaGCN
                                  FROM GCNQSDD gcn
                                       INNER JOIN DangKyQSDD dk ON gcn.MaDangKy = dk.MaDangKy
                                       INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
                                       INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                  WHERE tbd.MaDVHC = @MaDVHC
                                     AND gcn.MaGCN > @MaGiayChungNhanStart
                                     AND gcn.MaGCN <> @MaGiayChungNhanTemp
                                     {(minGiayChungNhanStart < minMaGiayChungNhanInDvhc ? "AND gcn.MaGCN < @MinMaGiayChungNhanInDvhc" : "")}
                                  """;
        var queryGiayChungNhanLichSuOnDangKyLs = $"""
                                                  SELECT DISTINCT gcn.MaGCNLS  AS MaGCN
                                                  FROM GCNQSDDLS gcn
                                                           INNER JOIN DangKyQSDDLS dk ON gcn.MaDangKyLS = dk.MaDangKyLS
                                                           INNER JOIN ThuaDatLS td ON dk.MaThuaDatLS = td.MaThuaDatLS
                                                           INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                                  WHERE tbd.MaDVHC = @MaDVHC
                                                  AND gcn.MaGCNLS > @MaGiayChungNhanStart
                                                  AND gcn.MaGCNLS <> @MaGiayChungNhanTemp
                                                  {(minGiayChungNhanStart < minMaGiayChungNhanInDvhc ? "AND gcn.MaGCNLS < @MinMaGiayChungNhanInDvhc" : "")}
                                                  """;
        var queryGiayChungNhanLichSuOnDangKy = $"""
                                                SELECT DISTINCT gcn.MaGCNLS  AS MaGCN
                                                FROM GCNQSDDLS gcn
                                                         INNER JOIN DangKyQSDD dk ON gcn.MaDangKyLS = dk.MaDangKy
                                                         INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
                                                         INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                                WHERE tbd.MaDVHC = @MaDVHC
                                                AND gcn.MaGCNLS > @MaGiayChungNhanStart
                                                AND gcn.MaGCNLS <> @MaGiayChungNhanTemp
                                                {(minGiayChungNhanStart < minMaGiayChungNhanInDvhc ? "AND gcn.MaGCNLS < @MinMaGiayChungNhanInDvhc" : "")}
                                                """;

        var queryGiayChungNhanLichSuOnDangKyLsOnThuaDat = $"""
                                                           SELECT DISTINCT gcn.MaGCNLS  AS MaGCN
                                                           FROM GCNQSDDLS gcn
                                                                    INNER JOIN DangKyQSDDLS dk ON gcn.MaDangKyLS = dk.MaDangKyLS
                                                                    INNER JOIN ThuaDat td ON dk.MaThuaDatLS = td.MaThuaDat
                                                                    INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                                           WHERE tbd.MaDVHC = @MaDVHC
                                                           AND gcn.MaGCNLS > @MaGiayChungNhanStart
                                                           AND gcn.MaGCNLS <> @MaGiayChungNhanTemp
                                                           {(minGiayChungNhanStart < minMaGiayChungNhanInDvhc ? "AND gcn.MaGCNLS < @MinMaGiayChungNhanInDvhc" : "")}
                                                           """;
        var query = $"""
                     SELECT TOP (@Limit) MaGCN
                     FROM (
                            {queryGiayChungNhan}
                         UNION 
                            {queryGiayChungNhanLichSuOnDangKyLs}
                         UNION 
                            {queryGiayChungNhanLichSuOnDangKy}
                         UNION 
                            {queryGiayChungNhanLichSuOnDangKyLsOnThuaDat}
                         ) AS CombinedResult
                     ORDER BY MaGCN {(minGiayChungNhanStart >= minMaGiayChungNhanInDvhc ? "DESC" : "ASC")};
                     """;
        // Khởi tạo tham số cho câu lệnh SQL
        var parameters = new
        {
            MaDVHC = dvhc.MaDvhc,
            MaGiayChungNhanStart = minGiayChungNhanStart,
            MaGiayChungNhanTemp = maGiayChungNhanTemp,
            MinMaGiayChungNhanInDvhc = minMaGiayChungNhanInDvhc,
            Limit = limit
        };

        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();
            // Thực thi câu lệnh SQL
            return await connection.QueryAsync<long>(query, parameters);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            logger?.Error(e, "Lỗi khi lấy mã Giấy chứng nhận cần làm mới.");
            throw;
        }
    }

    /// <summary>
    /// Làm mới mã Giấy chứng nhận.
    /// </summary>
    /// <param name="capXaSau">Cấp xã sau.</param>
    /// <param name="maGiayChungNhanTemp">Mã Giấy chứng nhận tạm thời. Mặc định: <see cref="DefaultMaGiayChungNhanTemp"/></param>
    /// <param name="maDangKyTemp">Mã Đăng ký tạm thời. Mặc định: <see cref="DangKyThuaDatRepository.DefaultMaDangKyTemp"/></param>
    /// <param name="limit">Số lượng mã cần làm mới. Mặc định: 100</param>
    /// <exception cref="OverflowException">Ném ra ngoại lệ nếu số lượng Giấy chứng nhận theo Đơn Vị Hành Chính vượt quá giới hạn.</exception>
    /// <exception cref="Exception">Ném ra ngoại lệ khi có lỗi xảy ra trong quá trình cập nhật.</exception>
    public async Task RenewMaGiayChungNhanAsync(DvhcRecord capXaSau,
        long maGiayChungNhanTemp = DefaultMaGiayChungNhanTemp,
        long maDangKyTemp = DangKyThuaDatRepository.DefaultMaDangKyTemp, int limit = 100)
    {
        // Kiểm tra xem số lượng Giấy chứng nhận theo Đơn Vị Hành Chính có vượt quá giới hạn không
        if (!await CheckOverflowAsync(capXaSau))
        {
            logger?.Warning("Số lượng Giấy chứng nhận theo Đơn Vị Hành Chính vượt quá giới hạn.");
            throw new OverflowException("Số lượng Giấy chứng nhận theo Đơn Vị Hành Chính vượt quá giới hạn.");
        }

        // Các câu lệnh SQL để cập nhật mã Giấy chứng nhận
        const string queryUpdateDkMucDichTemp = """
                                                UPDATE DangKyMDSDD
                                                    SET MaGCN = @MaGiayChungNhanTemp
                                                    WHERE MaGCN = @MaGiayChungNhanStartOld;
                                                UPDATE DangKyMDSDDLS
                                                    SET MaGCNLS = @MaGiayChungNhanTemp
                                                    WHERE MaGCNLS = @MaGiayChungNhanStartOld;
                                                """;
        const string queryUpdateDkNguonGocTemp = """
                                                 UPDATE DangKyNGSDD
                                                     SET MaGCN = @MaGiayChungNhanTemp
                                                     WHERE MaGCN = @MaGiayChungNhanStartOld;
                                                 UPDATE DangKyNGSDDLS
                                                     SET MaGCNLS = @MaGiayChungNhanTemp
                                                     WHERE MaGCNLS = @MaGiayChungNhanStartOld;
                                                 """;
        const string queryUpdateOtherTemp = """
                                            UPDATE DC_HanCheSuDungDat
                                                SET MaGCN = @MaGiayChungNhanTemp
                                                WHERE MaGCN = @MaGiayChungNhanStartOld;
                                            UPDATE DC_NghiaVuTaiChinh
                                                SET  MaGCN = @MaGiayChungNhanTemp
                                                WHERE MaGCN = @MaGiayChungNhanStartOld;
                                            UPDATE GCNQSDNha
                                                SET MaGCNNha = @MaGiayChungNhanTemp
                                                WHERE MaGCNNha = @MaGiayChungNhanStartOld;
                                            UPDATE TaiSan
                                                SET MaGCN = @MaGiayChungNhanTemp
                                                WHERE MaGCN = @MaGiayChungNhanStartOld;
                                            UPDATE TaiSanLS
                                                SET MaGCNLS = @MaGiayChungNhanTemp
                                                WHERE MaGCNLS = @MaGiayChungNhanStartOld;
                                            """;

        const string queryUpdateGiayChungNhan = """
                                                UPDATE GCNQSDD
                                                    SET MaGCN = @MaGiayChungNhanNew
                                                    WHERE MaGCN = @MaGiayChungNhanStartOld;
                                                UPDATE GCNQSDDLS
                                                    SET  MaGCNLS = @MaGiayChungNhanNew
                                                    WHERE MaGCNLS = @MaGiayChungNhanStartOld;
                                                """;
        const string queryUpdateDkMucDichNew = """
                                               UPDATE DangKyMDSDD
                                                   SET MaGCN = @MaGiayChungNhanNew
                                                   WHERE MaGCN = @MaGiayChungNhanTemp;
                                               UPDATE DangKyMDSDDLS
                                                   SET  MaGCNLS = @MaGiayChungNhanNew
                                                   WHERE MaGCNLS = @MaGiayChungNhanTemp;
                                               """;
        const string queryUpdateDkNguonGocNew = """
                                                UPDATE DangKyNGSDD
                                                    SET MaGCN = @MaGiayChungNhanNew
                                                    WHERE MaGCN = @MaGiayChungNhanTemp;
                                                UPDATE DangKyNGSDDLS
                                                    SET  MaGCNLS = @MaGiayChungNhanNew
                                                    WHERE MaGCNLS = @MaGiayChungNhanTemp;
                                                """;
        const string queryUpdateOtherNew = """
                                           UPDATE DC_HanCheSuDungDat
                                               SET MaGCN = @MaGiayChungNhanNew
                                               WHERE MaGCN = @MaGiayChungNhanTemp;
                                           UPDATE DC_NghiaVuTaiChinh
                                               SET  MaGCN = @MaGiayChungNhanNew
                                               WHERE MaGCN = @MaGiayChungNhanTemp;
                                           UPDATE GCNQSDNha
                                               SET MaGCNNha = @MaGiayChungNhanNew
                                               WHERE MaGCNNha = @MaGiayChungNhanTemp;
                                           UPDATE TaiSan
                                               SET MaGCN = @MaGiayChungNhanNew
                                               WHERE MaGCN = @MaGiayChungNhanTemp;
                                           UPDATE TaiSanLS
                                               SET MaGCNLS = @MaGiayChungNhanNew
                                               WHERE MaGCNLS = @MaGiayChungNhanTemp;
                                           """;
        const string queryUpdateOther = """
                                        UPDATE GiayToLienQuanPhieuChuyenNVTC
                                            SET MaGCN = @MaGiayChungNhanNew
                                            WHERE MaGCN = @MaGiayChungNhanStartOld;
                                        UPDATE NghiaVuTC
                                            SET MaGCN = @MaGiayChungNhanNew
                                            WHERE MaGCN = @MaGiayChungNhanStartOld;
                                        UPDATE TaiSan_ThuaDat
                                            SET MaGCN = @MaGiayChungNhanNew
                                            WHERE MaGCN = @MaGiayChungNhanStartOld;
                                        UPDATE ThamGiaBienDong
                                            SET MaGCN = @MaGiayChungNhanNew
                                            WHERE MaGCN = @MaGiayChungNhanStartOld;
                                        UPDATE ThamsoInBDTrenGCN
                                            SET MaGCN = @MaGiayChungNhanNew
                                            WHERE MaGCN = @MaGiayChungNhanStartOld;
                                        UPDATE ThamSoPhieuChuyenNVTC
                                            SET MaGCN = @MaGiayChungNhanNew
                                            WHERE MaGCN = @MaGiayChungNhanStartOld;
                                        UPDATE ThamSoPhieuChuyenNVTCCad
                                            SET MaGCN = @MaGiayChungNhanNew
                                            WHERE MaGCN = @MaGiayChungNhanStartOld;
                                        UPDATE GCNQR
                                            SET MaGCN = @MaGiayChungNhanNew
                                            WHERE MaGCN = @MaGiayChungNhanStartOld;
                                        """;
        const string queryUpdate = $"""
                                    {queryUpdateDkMucDichTemp}
                                    {queryUpdateDkNguonGocTemp}
                                    {queryUpdateOtherTemp}
                                    {queryUpdateGiayChungNhan}
                                    {queryUpdateDkMucDichNew}
                                    {queryUpdateDkNguonGocNew}
                                    {queryUpdateOtherNew}
                                    {queryUpdateOther}
                                    """;
        // Khởi tạo các giá trị ban đầu:
        maGiayChungNhanTemp = await CreateTempGiayChungNhanAsync(maGiayChungNhanTemp, maDangKyTemp);
        long? maGiayChungNhanStart = null;
        var unusedMaGiayChungNhan = new Queue<long>();
        long? maGiayChungNhanNew = null;
        var minMaGiayChungNhanInDvhc = capXaSau.Ma.GetMinPrimaryKey();

        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();
            while (true)
            {
                // Lấy mã Giấy chứng nhận cần làm mới
                var maGiayChungNhanNeedRenew =
                    (await GetMaGiayChungNhanNeedRenewAsync(capXaSau, maGiayChungNhanStart, maGiayChungNhanTemp, limit))
                    .ToHashSet();
                foreach (var maGiayChungNhanOld in maGiayChungNhanNeedRenew)
                {
                    if (unusedMaGiayChungNhan.Count == 0)
                    {
                        unusedMaGiayChungNhan =
                            new Queue<long>(await GetUnusedMaGiayChungNhanAsync(capXaSau, maGiayChungNhanNew, limit));
                    }

                    maGiayChungNhanNew = unusedMaGiayChungNhan.Dequeue();
                    if (maGiayChungNhanNew >= maGiayChungNhanOld && maGiayChungNhanOld >= minMaGiayChungNhanInDvhc)
                    {
                        maGiayChungNhanNeedRenew = [];
                        break;
                    }

                    // Tạo tham số cho câu lệnh SQL
                    var parameters = new
                    {
                        MaGiayChungNhanStartOld = maGiayChungNhanOld,
                        MaGiayChungNhanTemp = maGiayChungNhanTemp,
                        MaGiayChungNhanNew = maGiayChungNhanNew
                    };

                    // Kiểm tra trạng thái kết nối cơ sở dữ liệu
                    if (connection.State == ConnectionState.Closed)
                    {
                        await connection.OpenAsync();
                    }

                    // Sử dụng transaction để thực thi câu lệnh SQL
                    await using var transaction = connection.BeginTransaction();
                    try
                    {
                        // Thực thi câu lệnh SQL
                        await connection.ExecuteAsync(queryUpdate, parameters, transaction);
                        await transaction.CommitAsync();
                    }
                    catch (Exception exception)
                    {
                        // Rollback transaction
                        await transaction.RollbackAsync();
                        Console.WriteLine(exception);
                        logger?.Error(exception, "Lỗi khi làm mới mã Giấy chứng nhận.[DVHC: {DVHC}]", capXaSau);
                        throw;
                    }
                }

                // Nếu không còn mã cần làm mới thì kết thúc
                if (maGiayChungNhanNeedRenew.Count == 0 && maGiayChungNhanStart >= minMaGiayChungNhanInDvhc)
                    break;
                // Lấy mã Giấy chứng nhận bắt đầu
                maGiayChungNhanStart = maGiayChungNhanNeedRenew.Count > 0
                    ? maGiayChungNhanNeedRenew.Max()
                    : minMaGiayChungNhanInDvhc;
                maGiayChungNhanStart = maGiayChungNhanStart <= minMaGiayChungNhanInDvhc
                    ? maGiayChungNhanStart
                    : maGiayChungNhanNew + 1;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            logger?.Error(e, "Lỗi khi làm mới mã Giấy chứng nhận.[DVHC: {DVHC}]", capXaSau);
            throw;
        }
    }

    /// <summary>
    /// Xóa Giấy chứng nhận tạm thời.
    /// </summary>
    /// <param name="dbConnection">Kết nối cơ sở dữ liệu.</param>
    /// <param name="maGiayChungNhanTemp">Mã Giấy chứng nhận tạm thời. Mặc định: <see cref="DefaultMaGiayChungNhanTemp"/>.</param>
    /// <param name="logger">Logger để ghi lại thông tin lỗi. Mặc định là null.</param>
    /// <returns>Task không đồng bộ.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình xóa.</exception>
    public static async Task DeleteGiayChungNhanTemp(SqlConnection dbConnection,
        long maGiayChungNhanTemp = DefaultMaGiayChungNhanTemp, ILogger? logger = null)
    {
        try
        {
            // Câu lệnh SQL để xóa Giấy chứng nhận tạm thời
            const string queryGiayChungNhan = """
                                              DELETE FROM GCNQSDD WHERE MaGCN = @MaGCN;
                                              DELETE FROM GCNQSDDLS WHERE MaGCNLS = @MaGCN;
                                              """;
            // Khởi tạo tham số cho câu lệnh SQL
            var parameters = new { MaGCN = maGiayChungNhanTemp };

            // Thực thi câu lệnh SQL
            await dbConnection.ExecuteAsync(queryGiayChungNhan, parameters);
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi xóa Giấy chứng nhận tạm thời.");
            throw;
        }
    }

    /// <summary>
    /// Xóa Giấy chứng nhận theo Mã Đăng ký.
    /// </summary>
    /// <param name="dbConnection">Kết nối cơ sở dữ liệu.</param>
    /// <param name="maDangKy">Mã Đăng ký. Mặc định: <see cref="DangKyThuaDatRepository.DefaultMaDangKyTemp"/>.</param>
    /// <param name="logger">Logger để ghi lại thông tin lỗi. Mặc định là null.</param>
    /// <returns>Task không đồng bộ.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình xóa.</exception>
    public static async Task DeleteGiayChungNhanByMaDangKyAsync(SqlConnection dbConnection,
        long maDangKy = DangKyThuaDatRepository.DefaultMaDangKyTemp, ILogger? logger = null)
    {
        try
        {
            // Câu lệnh SQL để xóa Giấy chứng nhận theo Mã Đăng ký
            const string queryGiayChungNhan = """
                                              DELETE FROM GCNQSDD WHERE MaDangKy = @MaDangKy;
                                              DELETE FROM GCNQSDDLS WHERE MaDangKyLS = @MaDangKy;
                                              """;
            // Khởi tạo tham số cho câu lệnh SQL
            var parameters = new { MaDangKy = maDangKy };

            // Thực thi câu lệnh SQL
            await dbConnection.ExecuteAsync(queryGiayChungNhan, parameters);
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi xóa Giấy chứng nhận theo Mã Đăng ký. [MaDangKy: {MaDangKy}]", maDangKy);
            throw;
        }
    }

    #endregion
}