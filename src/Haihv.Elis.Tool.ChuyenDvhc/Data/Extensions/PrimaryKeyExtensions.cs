namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;

/// <summary>
/// Lớp chứa các phương thức mở rộng để tạo và xử lý khóa chính.
/// </summary>
public static class PrimaryKeyExtensions
{
    private const int Multiplier = 100000;

    /// <summary>
    /// Tạo khóa chính từ số thứ tự và mã đơn vị hành chính cấp xã.
    /// </summary>
    /// <param name="soThuTu">Số thứ tự</param>
    /// <param name="maDvhcCapXa">Mã đơn vị hành chính cấp xã</param>
    /// <param name="multiplier">Hệ số nhân (mặc định là 100000)</param>
    /// <returns>Khóa chính được tạo</returns>
    public static long GeneratePrimaryKey(this long soThuTu, int maDvhcCapXa, int multiplier = Multiplier)
        => maDvhcCapXa * multiplier + soThuTu;

    /// <summary>
    /// Lấy khóa chính lớn nhất có thể từ mã đơn vị hành chính cấp xã.
    /// </summary>
    /// <param name="maDvhcCapXa">Mã đơn vị hành chính cấp xã</param>
    /// <param name="multiplier">Hệ số nhân (mặc định là 100000)</param>
    /// <returns>Khóa chính lớn nhất có thể</returns>
    public static long GetMaxPrimaryKey(this int maDvhcCapXa, int multiplier = Multiplier)
        => maDvhcCapXa * multiplier + multiplier - 1;

    /// <summary>
    /// Lấy khóa chính nhỏ nhất có thể từ mã đơn vị hành chính cấp xã.
    /// </summary>
    /// <param name="maDvhcCapXa">Mã đơn vị hành chính cấp xã</param>
    /// <param name="multiplier">Hệ số nhân (mặc định là 100000)</param>
    /// <returns>Khóa chính nhỏ nhất có thể</returns>
    public static long GetMinPrimaryKey(this int maDvhcCapXa, int multiplier = Multiplier)
        => maDvhcCapXa * multiplier + 1;

    /// <summary>
    /// Loại bỏ mã đơn vị hành chính cấp xã trong khóa chính.
    /// </summary>
    /// <param name="primaryKey">Khóa chính</param>
    /// <param name="multiplier">Hệ số nhân (mặc định là 100000)</param>
    /// <returns>5 số cuối của mã</returns>
    public static long RemoveMaDvhcCapXaInPrimaryKey(this long primaryKey, int multiplier = Multiplier)
        => primaryKey % multiplier;

    /// <summary>
    /// Lấy khóa chính lớn nhất có thể từ hệ số nhân.
    /// </summary>
    /// <param name="multiplier">Hệ số nhân</param>
    /// <returns>Khóa chính lớn nhất có thể</returns>
    public static long GetMaximumPrimaryKey(int multiplier = Multiplier)
        => multiplier - 1;
}