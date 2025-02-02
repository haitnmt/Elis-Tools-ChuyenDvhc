using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

/// <summary>
/// Repository quản lý các đơn vị hành chính.
/// </summary>
/// <param name="connectionString">Chuỗi kết nối cơ sở dữ liệu.</param>
/// <param name="logger">Đối tượng ghi log (tùy chọn).</param>
public sealed class DonViHanhChinhRepository(string connectionString, ILogger? logger = null)
{
    /// <summary>
    /// Cập nhật thông tin đơn vị hành chính mới và các đơn vị hành chính bị sáp nhập.
    /// </summary>
    /// <param name="dvhcMoi">Thông tin đơn vị hành chính mới.</param>
    /// <param name="maDvhcBiSapNhaps">Danh sách mã đơn vị hành chính bị sáp nhập.</param>
    /// <param name="formatTenDonViHanhChinhBiSapNhap">Định dạng tên đơn vị hành chính bị sáp nhập (tùy chọn).</param>
    /// <param name="cancellationToken">Token hủy bỏ để hủy tác vụ không đồng bộ.</param>
    public async Task<bool> UpdateDonViHanhChinhAsync(DvhcRecord dvhcMoi, List<int> maDvhcBiSapNhaps,
        string? formatTenDonViHanhChinhBiSapNhap = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();
            // Cập nhật đơn vị hành chính mới
            var dvhc = new Dvhc
            {
                MaDvhc = dvhcMoi.MaDvhc,
                Ten = dvhcMoi.Ten
            };
            const string updateQuery = """
                                       UPDATE DVHC
                                         SET Ten = @Ten
                                         WHERE MaDVHC = @MaDVHC
                                       """;
            await connection.ExecuteAsync(updateQuery, dvhc);

            // Cập nhật các đơn vị hành chính bị sáp nhập
            if (maDvhcBiSapNhaps.Count == 0) return true;

            // Nếu formatTenDonViHanhChinhBiSapNhap rỗng thì sử dụng giá trị mặc định
            if (string.IsNullOrWhiteSpace(formatTenDonViHanhChinhBiSapNhap))
                formatTenDonViHanhChinhBiSapNhap = ThamSoThayThe.DefaultDonViHanhChinhBiSapNhap;

            List<Dvhc> dvhcs = [];
            dvhcs.AddRange(maDvhcBiSapNhaps.Select(maDvhcBiSapNhap =>
                new Dvhc
                {
                    MaDvhc = maDvhcBiSapNhap,
                    Ten = formatTenDonViHanhChinhBiSapNhap.Replace(ThamSoThayThe.DonViHanhChinh, dvhcMoi.Ten)
                }));
            const string updateQueryBiSapNhap = """
                                                UPDATE DVHC
                                                  SET Ten = @Ten
                                                  WHERE MaDVHC = @MaDVHC
                                                """;
            await connection.ExecuteAsync(updateQueryBiSapNhap, dvhcs);
            return true;
        }
        catch (Exception exception)
        {
            if (logger == null) throw;
            logger.Error(exception, "Lỗi khi cập nhật đơn vị hành chính. [{MaDVHC}]", dvhcMoi.MaDvhc);
            return false;
        }
    }

    /// <summary>
    /// Lấy danh sách các đơn vị hành chính cấp tỉnh.
    /// </summary>
    /// <returns>Danh sách các đơn vị hành chính cấp tỉnh.</returns>
    public async ValueTask<IEnumerable<DvhcRecord>> GetCapTinhAsync()
    {
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();
            const string query = """
                                 SELECT MaDVHC AS MaDvhc, MaTinh AS Ma, Ten
                                 FROM DVHC
                                 WHERE MaHuyen = 0 AND MaXa = 0
                                 ORDER BY MaTinh
                                 """;
            return await connection.QueryAsync<DvhcRecord>(query);
        }
        catch (Exception exception)
        {
            logger?.Error(exception, "Lỗi khi lấy danh sách đơn vị hành chính cấp tỉnh.");
            throw;
        }
    }

    /// <summary>
    /// Lấy danh sách các đơn vị hành chính cấp huyện.
    /// </summary>
    /// <param name="maTinh">Mã tỉnh.</param>
    /// <returns>Danh sách các đơn vị hành chính cấp huyện.</returns>
    public async ValueTask<IEnumerable<DvhcRecord>> GetCapHuyenAsync(int maTinh)
    {
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();
            const string query = """
                                 SELECT MaDVHC AS MaDvhc, MaHuyen AS Ma, Ten
                                 FROM DVHC
                                 WHERE MaTinh = @MaTinh AND MaXa = 0 AND MaHuyen != 0
                                 ORDER BY MaHuyen
                                 """;
            return await connection.QueryAsync<DvhcRecord>(query, new { MaTinh = maTinh });
        }
        catch (Exception e)
        {
            if (logger == null) throw;
            logger.Error(e, "Lỗi khi lấy danh sách đơn vị hành chính cấp huyện theo mã tỉnh. [{MaTinh}]", maTinh);
            return [];
        }
    }

    /// <summary>
    /// Lấy danh sách các đơn vị hành chính cấp xã.
    /// </summary>
    /// <param name="maHuyen">Mã huyện.</param>
    /// <returns>Danh sách các đơn vị hành chính cấp xã.</returns>
    public async ValueTask<IEnumerable<DvhcRecord>> GetCapXaAsync(int maHuyen)
    {
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();
            const string query = """
                                 SELECT MaDVHC AS MaDvhc, MaXa AS Ma, Ten
                                 FROM DVHC
                                 WHERE MaHuyen = @MaHuyen AND MaXa != 0
                                 ORDER BY MaXa
                                 """;
            return await connection.QueryAsync<DvhcRecord>(query, new { MaHuyen = maHuyen });
        }
        catch (Exception e)
        {
            if (logger == null) throw;
            logger.Error(e, "Lỗi khi lấy danh sách đơn vị hành chính cấp xã theo mã huyện. [{MaHuyen}]", maHuyen);
            return [];
        }
    }
}