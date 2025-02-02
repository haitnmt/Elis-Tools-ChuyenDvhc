using System.Data;
using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.Data.SqlClient;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

/// <summary>
/// Repository để thao tác với bảng ToBanDo
/// </summary>
/// <param name="connectionString">Chuỗi kết nối đến cơ sở dữ liệu</param>
/// <param name="logger">Đối tượng ghi log (tùy chọn)</param>
public class ToBanDoRepository(string connectionString, ILogger? logger = null)
{
    public const long DefaultMaToBanDoTemp = 0;
    private const int Multiplier = 10000;

    /// <summary>
    /// Cập nhật thông tin Tờ Bản Đồ.
    /// </summary>
    /// <param name="thamChieuToBanDos">Danh sách tham chiếu Tờ Bản Đồ.</param>
    /// <param name="formatGhiChuToBanDo">Định dạng ghi chú Tờ Bản Đồ (tùy chọn).</param>
    /// <param name="ngaySapNhap">Ngày sắp nhập (tùy chọn).</param>
    /// <returns>Số lượng bản ghi được cập nhật.</returns>
    public async Task<int> UpdateToBanDoAsync(List<ThamChieuToBanDo> thamChieuToBanDos,
        string? formatGhiChuToBanDo = null, string? ngaySapNhap = null)
    {
        try
        {
            if (thamChieuToBanDos.Count == 0)
                return 0;
            // Nếu formatGhiChuToBanDo rỗng thì sử dụng giá trị mặc định
            if (string.IsNullOrWhiteSpace(formatGhiChuToBanDo))
                formatGhiChuToBanDo = ThamSoThayThe.DefaultGhiChuToBanDo;

            // Nếu ngaySapNhap rỗng thì sử dụng ngày hiện tại
            if (string.IsNullOrWhiteSpace(ngaySapNhap))
                ngaySapNhap = DateTime.Now.ToString(ThamSoThayThe.DinhDangNgaySapNhap);

            List<ToBanDo> toBanDos = [];
            toBanDos.AddRange(from thamChieuToBanDo in thamChieuToBanDos
                let ghiChuToBanDo = formatGhiChuToBanDo.Replace(ThamSoThayThe.NgaySapNhap, ngaySapNhap)
                    .Replace(ThamSoThayThe.ToBanDo, thamChieuToBanDo.SoToBanDoTruoc)
                    .Replace(ThamSoThayThe.DonViHanhChinh, thamChieuToBanDo.TenDvhcTruoc)
                select new ToBanDo
                {
                    MaToBanDo = thamChieuToBanDo.MaToBanDoTruoc, SoTo = thamChieuToBanDo.SoToBanDoSau,
                    MaDvhc = thamChieuToBanDo.MaDvhcSau, GhiChu = ghiChuToBanDo
                });
            const string sqlUpdate = """
                                     UPDATE ToBanDo
                                     SET SoTo = @SoTo, MaDvhc = @MaDvhc, GhiChu = @GhiChu
                                     WHERE MaToBanDo = @MaToBanDo
                                     """;
            // Cập nhật toàn bộ khối dữ liệu
            return await connectionString
                .GetConnection()
                .ExecuteAsync(sqlUpdate, toBanDos);
        }
        catch (Exception ex)
        {
            if (logger == null) throw;
            logger.Error(ex, "Lỗi khi cập nhật thông tin Tờ Bản Đồ.");
            return 0;
        }
    }

    /// <summary>
    /// Tạo một Tờ Bản Đồ tạm thời.
    /// </summary>
    /// <param name="connection">Kết nối cơ sở dữ liệu.</param>
    /// <param name="tempMaToBanDo">Mã Tờ Bản Đồ (tùy chọn).</param>
    /// <param name="logger">Đối tượng ghi log (tùy chọn).</param>
    /// <returns>Mã của Tờ Bản Đồ tạm thời được tạo.</returns>
    public static async Task<long> CreateTempToBanDoAsync(SqlConnection connection,
        long tempMaToBanDo = DefaultMaToBanDoTemp,
        ILogger? logger = null)
    {
        try
        {
            var toBanDo = new ToBanDo
            {
                MaToBanDo = tempMaToBanDo,
                TyLe = 1,
                SoTo = "Temp",
                MaDvhc = 100001,
                GhiChu = "Tờ bản đồ tạm thời"
            };

            const string upsertQuery = """
                                       MERGE INTO ToBanDo AS target
                                       USING (SELECT @MaToBanDo AS MaToBanDo) AS source
                                       ON target.MaToBanDo = source.MaToBanDo
                                       WHEN NOT MATCHED THEN
                                           INSERT (MaToBanDo, SoTo, Tyle, MaDvhc, GhiChu)
                                           VALUES (@MaToBanDo, @SoTo, @TyLe, @MaDvhc, @GhiChu)
                                       WHEN MATCHED THEN
                                           UPDATE SET SoTo = @SoTo, MaDvhc = @MaDvhc, GhiChu = @GhiChu;
                                       """;
            await connection.ExecuteAsync(upsertQuery, toBanDo);
            return toBanDo.MaToBanDo;
        }
        catch (Exception e)
        {
            if (logger == null) throw;
            logger.Error(e, "Lỗi khi tạo Tờ Bản Đồ tạm thời. [MaToBanDo: {MaToBanDo}]", tempMaToBanDo);
            return long.MinValue;
        }
    }

    /// <summary>
    /// Lấy danh sách Tờ Bản Đồ theo mã đơn vị hành chính.
    /// </summary>
    /// <param name="maDvhc">Mã đơn vị hành chính.</param>
    /// <returns>Danh sách các Tờ Bản Đồ.</returns>
    public async Task<IEnumerable<ToBanDo>> GetToBanDosAsync(int maDvhc)
    {
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();
            const string query = """
                                 SELECT MaToBanDo, SoTo, MaDVHC AS MaDvhc, TyLe, GhiChu
                                 FROM ToBanDo
                                 WHERE MaDvhc = @MaDvhc
                                 """;
            return await connection.QueryAsync<ToBanDo>(query, new { MaDvhc = maDvhc });
        }
        catch (Exception ex)
        {
            if (logger == null) throw;
            logger.Error(ex, "Lỗi khi lấy danh sách Tờ Bản Đồ theo mã đơn vị hành chính. [MaDVHC: {MaDVHC}]", maDvhc);
            return [];
        }
    }

    /// <summary>
    /// Lấy mã Tờ Bản Đồ lớn nhất theo mã đơn vị hành chính.
    /// </summary>
    /// <param name="dvhc">Đơn vị hành chính.</param>
    /// <returns>Mã Tờ Bản Đồ lớn nhất.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    private async Task<long> GetMaxMaToBanDosAsync(DvhcRecord dvhc)
    {
        try
        {
            await using var connection = connectionString.GetConnection();
            const string query = """
                                 SELECT MAX(MaToBanDo)
                                 FROM ToBanDo
                                 WHERE MaDvhc = @MaDvhc AND MaToBanDo > @MinMaToBanDo AND MaToBanDo < @MaxMaToBanDo
                                 """;
            var param = new
            {
                dvhc.MaDvhc,
                MinMaToBanDo = dvhc.Ma.GetMinPrimaryKey(Multiplier),
                MaxMaToBanDo = dvhc.Ma.GetMaxPrimaryKey(Multiplier)
            };
            return await connection.ExecuteScalarAsync<long>(query, param);
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi lấy mã Tờ Bản Đồ lớn nhất theo mã đơn vị hành chính. [MaDVHC: {MaDVHC}]", dvhc);
            throw;
        }
    }

    /// <summary>
    /// Lấy danh sách mã Tờ Bản Đồ theo mã đơn vị hành chính.
    /// </summary>
    /// <param name="dvhc">Đơn vị hành chính.</param>
    /// <param name="minMaToBanDo">Mã Tờ Bản Đồ bắt đầu (tùy chọn).</param>
    /// <param name="tempMaToBanDo">Mã Tờ Bản Đồ tạm thời (mặc định là <see cref="DefaultMaToBanDoTemp"/>).</param>
    /// <param name="limit">Số lượng bản ghi tối đa cần lấy.</param>
    /// <returns>Danh sách mã Tờ Bản Đồ.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    private async Task<IEnumerable<long>> GetMaToBanDosNeedRenewAsync(DvhcRecord dvhc, long? minMaToBanDo = null,
        long tempMaToBanDo = DefaultMaToBanDoTemp,
        int limit = 100)
    {
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();

            // Khởi tạo mã Tờ Bản Đồ nhỏ nhất trong đơn vị hành chính
            var minMaInDvhc = dvhc.Ma.GetMinPrimaryKey(Multiplier);
            minMaToBanDo ??= long.MinValue;
            // Câu truy vấn
            var query = $"""
                         SELECT TOP(@Limit) MaToBanDo
                         FROM ToBanDo
                         WHERE MaDvhc = @MaDvhc AND MaToBanDo >= @MaToBanDo AND MaToBanDo <> @TempMaToBanDo
                         {(minMaToBanDo < minMaInDvhc ? "AND MaToBanDo < @MinMaInDvhc" : "")}
                         ORDER BY MaToBanDo {(minMaToBanDo >= minMaInDvhc ? "DESC" : "ASC")}
                         """;
            return await connection.QueryAsync<long>(query,
                new
                {
                    Limit = limit,
                    dvhc.MaDvhc,
                    MaToBanDo = minMaToBanDo,
                    MinMaInDvhc = minMaInDvhc,
                    TempMaToBanDo = tempMaToBanDo
                });
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi lấy danh sách mã Tờ Bản Đồ theo mã đơn vị hành chính. [MaDVHC: {MaDVHC}]",
                dvhc);
            throw;
        }
    }

    /// <summary>
    /// Kiểm tra xem số lượng tờ bản đồ có vượt quá giới hạn không.
    /// </summary>
    /// <param name="dvhc">Đơn vị hành chính.</param>
    /// <returns>Trả về true nếu số lượng tờ bản đồ không vượt quá giới hạn, ngược lại trả về false.</returns>
    private async Task<bool> CheckOverflowAsync(DvhcRecord dvhc)
    {
        // Lấy tổng số lượng tờ bản đồ hiện có:
        const string query = """
                             SELECT COUNT(DISTINCT MaToBanDo)
                             FROM ToBanDo
                             WHERE MaDvhc = @MaDvhc
                             """;
        await using var connection = connectionString.GetConnection();
        var count = await connection.ExecuteScalarAsync<int>(query, new { dvhc.MaDvhc });
        return count <= PrimaryKeyExtensions.GetMaximumPrimaryKey(Multiplier);
    }

    /// <summary>
    /// Lấy danh sách mã Tờ Bản Đồ chưa được sử dụng theo mã đơn vị hành chính.
    /// </summary>
    /// <param name="dvhc">Đơn vị hành chính.</param>
    /// <param name="minMaToBanDo">Mã Tờ Bản Đồ bắt đầu (tùy chọn).</param>
    /// <param name="limit">Số lượng bản ghi tối đa cần lấy.</param>
    /// <returns>Danh sách mã Tờ Bản Đồ chưa được sử dụng.</returns>
    private async Task<IEnumerable<long>> GetUnusedMaToBanDosAsync(DvhcRecord dvhc, long? minMaToBanDo = null,
        int limit = 100)
    {
        try
        {
            await using var connection = connectionString.GetConnection();
            const string query = """
                                 SELECT TOP(@Limit) MaToBanDo
                                 FROM ToBanDo
                                 WHERE (MaDvhc = @MaDvhc) AND (MaToBanDo > @MinMaToBanDo) AND (MaToBanDo < @MaxMaToBanDoInDvhc)
                                 ORDER BY MaToBanDo
                                 """;
            List<long> result = [];
            var maToBanDo = minMaToBanDo ?? dvhc.Ma.GetMinPrimaryKey(Multiplier) - 1;
            var maxMaToBanDo = await GetMaxMaToBanDosAsync(dvhc) + 1;
            var maxMaToBanDoInDvhc = dvhc.Ma.GetMaxPrimaryKey(Multiplier);
            while (result.Count == 0)
            {
                if (maToBanDo >= maxMaToBanDo)
                {
                    // Trả về danh sách có limit phần tử liên tục từ maToBanDo
                    return Enumerable.Range(1, limit).Select(i => maToBanDo + i);
                }

                var usedIds = (await connection.QueryAsync<long>(query,
                    new
                    {
                        Limit = limit,
                        dvhc.MaDvhc,
                        MinMaToBanDo = maToBanDo,
                        MaxMaToBanDoInDvhc = maxMaToBanDoInDvhc
                    })).ToList();
                if (usedIds.Count == 0)
                {
                    // Trả về danh sách có limit phần tử liên tục từ maToBanDo
                    return Enumerable.Range(1, limit).Select(i => maxMaToBanDo + i);
                }

                var maxId = usedIds.Max();
                for (var i = maToBanDo + 1; i < maxId; i++)
                {
                    if (!usedIds.Contains(i))
                    {
                        result.Add(i);
                    }
                }

                if (result.Count == 0)
                {
                    maToBanDo = maxId;
                }
            }

            return result;
        }
        catch (Exception e)
        {
            logger?.Error(e,
                """
                Lỗi khi lấy danh sách mã Tờ Bản Đồ chưa được sử dụng theo mã đơn vị hành chính. 
                [DVHC: {DVHC}], [MinMaToBanDo: {MinMaToBanDo}] 
                """,
                dvhc, minMaToBanDo);
            throw;
        }
    }

    /// <summary>
    /// Cập nhật mã Tờ Bản Đồ theo mã đơn vị hành chính.
    /// </summary>
    /// <param name="dvhc">Đơn vị hành chính.</param>
    /// <param name="tempMaToBanDo">Mã Tờ Bản Đồ tạm thời (mặc định là <see cref="DefaultMaToBanDoTemp"/>).</param>
    /// <param name="limit">Số lượng bản ghi tối đa cần lấy.</param>
    /// <returns>Mã Tờ Bản Đồ tạm thời.</returns>
    /// <exception cref="OverflowException">Ném ra ngoại lệ khi số lượng Tờ Bản Đồ đã đạt giới hạn tối đa.</exception>
    /// <exception cref="Exception">Ném ra ngoại lệ khi có lỗi xảy ra trong quá trình cập nhật.</exception>
    public async Task<long> RenewMaToBanDoAsync(DvhcRecord dvhc, long tempMaToBanDo = DefaultMaToBanDoTemp,
        int limit = 100)
    {
        try
        {
            // Kiểm tra xem có cần cập nhật không
            if (!await CheckOverflowAsync(dvhc))
            {
                logger?.Error("Số lượng Tờ Bản Đồ đã đạt giới hạn tối đa. [DVHC: {DVHC}]", dvhc);
                throw new OverflowException("Số lượng Tờ Bản Đồ đã đạt giới hạn tối đa.");
            }

            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();

            // Tạo mã Tờ Bản Đồ tạm thời
            tempMaToBanDo = await CreateTempToBanDoAsync(connection, tempMaToBanDo, logger: logger);

            // Xóa thửa đất có mã Tờ Bản Đồ tạm thời
            await ThuaDatRepository.DeleteThuaDatByMaToBanDoAsync(connection, tempMaToBanDo, logger);
            // Mã tờ bản đồ bắt đầu:
            long? startId = null;
            // Lấy các mã Tờ Bản Đồ cần cập nhật
            // Lấy danh sách mã Tờ Bản Đồ cần cập nhật
            var unusedIds = new Queue<long>();
            // Lấy mã Tờ Bản Đồ nhỏ nhất chưa được sử dụng
            long? newMaToBanDo = null;
            // Cập nhật mã Tờ Bản Đồ
            const string query = """
                                     UPDATE ThuaDat
                                     SET MaToBanDo = @TempMaToBanDo
                                     WHERE MaToBanDo = @OldMaToBanDo;
                                     UPDATE ThuaDatLS
                                     SET MaToBanDo = @TempMaToBanDo
                                     WHERE MaToBanDo = @OldMaToBanDo;
                                     
                                     UPDATE ToBanDo
                                     SET MaToBanDo = @NewMaToBanDo
                                     WHERE MaToBanDo = @OldMaToBanDo;
                                 
                                     UPDATE ThuaDat
                                     SET MaToBanDo = @NewMaToBanDo
                                     WHERE MaToBanDo = @TempMaToBanDo;
                                     UPDATE ThuaDatLS
                                     SET MaToBanDo = @NewMaToBanDo
                                     WHERE MaToBanDo = @TempMaToBanDo;
                                 """;
            var minMaInDvhc = dvhc.Ma.GetMinPrimaryKey(Multiplier);
            while (true)
            {
                // Lấy danh sách mã Tờ Bản Đồ cần cập nhật
                var maToBanDosNeedRenew =
                    (await GetMaToBanDosNeedRenewAsync(dvhc, startId, tempMaToBanDo, limit)).ToList();
                foreach (var oldMaToBanDo in maToBanDosNeedRenew)
                {
                    if (unusedIds.Count == 0)
                    {
                        unusedIds = new Queue<long>(await GetUnusedMaToBanDosAsync(dvhc, newMaToBanDo, limit));
                    }

                    newMaToBanDo = unusedIds.Dequeue();
                    if (newMaToBanDo >= oldMaToBanDo && oldMaToBanDo >= minMaInDvhc)
                    {
                        maToBanDosNeedRenew = [];
                        break;
                    }

                    var param = new
                    {
                        TempMaToBanDo = tempMaToBanDo,
                        NewMaToBanDo = newMaToBanDo,
                        OldMaToBanDo = oldMaToBanDo
                    };

                    // Sử dụng giao dịch để đảm bảo tính nhất quán
                    if (connection.State == ConnectionState.Closed)
                    {
                        await connection.OpenAsync();
                    }

                    await using var transaction = await connection.BeginTransactionAsync();

                    try
                    {
                        // Thực thi câu lệnh SQL trong giao dịch
                        await connection.ExecuteAsync(query, param, transaction: transaction);
                        // Commit giao dịch nếu thành công
                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        // Rollback giao dịch nếu có lỗi
                        await transaction.RollbackAsync();
                        logger?.Error(ex,
                            """
                            Lỗi trong quá trình cập nhật Tờ bản đồ sang mã mới. 
                            [Mã cũ: {OldMaToBanDo}, Mã mới: {NewMaToBanDo}]
                            """,
                            oldMaToBanDo, newMaToBanDo);
                        throw;
                    }
                }

                // Nếu không còn mã Tờ Bản Đồ cần cập nhật hoặc đã cập nhật hết
                if (maToBanDosNeedRenew.Count == 0 && startId >= minMaInDvhc) break;

                // Lấy mã Tờ Bản Đồ bắt đầu tiếp theo để cập nhật
                startId = maToBanDosNeedRenew.Count > 0 ? maToBanDosNeedRenew.Max() + 1 : minMaInDvhc;
                startId = startId <= minMaInDvhc ? startId : newMaToBanDo + 1;
            }

            return tempMaToBanDo;
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi làm mới Mã Tờ Bản Đồ. [DVHC: {DVHC}]", dvhc);
            throw;
        }
    }

    /// <summary>
    /// Xóa Tờ Bản Đồ tạm thời.
    /// </summary>
    /// <param name="dbConnection">Kết nối cơ sở dữ liệu.</param>
    /// <param name="maToBanDoTemp">Mã Tờ Bản Đồ tạm thời (mặc định là <see cref="DefaultMaToBanDoTemp"/>).</param>
    /// <param name="logger">Đối tượng ghi log (tùy chọn).</param>
    /// <returns>Task đại diện cho thao tác bất đồng bộ.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình xóa.</exception>
    public static async Task DeleteToBanDoTempAsync(SqlConnection dbConnection,
        long maToBanDoTemp = DefaultMaToBanDoTemp, ILogger? logger = null)
    {
        try
        {
            const string query = "DELETE FROM ToBanDo WHERE MaToBanDo = @MaToBanDo";
            await dbConnection.ExecuteAsync(query, new { MaToBanDo = maToBanDoTemp });
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi xóa Tờ Bản Đồ tạm thời. [MaToBanDo: {MaToBanDo}]", maToBanDoTemp);
            throw;
        }
    }
}