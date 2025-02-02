namespace Haihv.Elis.Tool.ChuyenDvhc.Settings;

public static class FilePath
{
    public static string PathConnectionString =>
        Path.Combine(PathRootConfig(), "ConnectionInfo.inf");

    public static string CacheOnDisk => Path.Combine(PathRootConfig(), "CacheFiles");

    public static string LogFile(string fileName) => Path.Combine(PathRootConfig("Logs", true), fileName);

    private static string PathRootConfig(string folder = "", bool addDate = false)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ElisTool");
        if (!string.IsNullOrWhiteSpace(folder))
        {
            path = Path.Combine(path, folder);
        }

        if (addDate)
        {
            path = Path.Combine(path, DateTime.Now.ToString("yyyy-MM-dd"));
        }

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        return path;
    }
}

public static class ThamSoThayThe
{
    public static string ToBanDo => "{TBD}";
    public static string SoThua => "{TD}";
    public static string DonViHanhChinh => "{DVHC}";
    public static string NgaySapNhap => "{NSN}";

    public static string DinhDangNgaySapNhap => "dd/MM/yyyy";

    public static string DefaultToBanDoCu => $"{ToBanDo} - {DonViHanhChinh}";

    public static string DefaultGhiChuToBanDo => $"Trước ngày {NgaySapNhap} " +
                                                 $"là tờ bản đồ số {ToBanDo} {DonViHanhChinh}";

    public static string DefaultGhiChuThuaDat => $"Trước ngày {NgaySapNhap} " +
                                                 $"thuộc tờ bản đồ số {ToBanDo} {DonViHanhChinh}";

    public static string DefaultGhiChuGiayChungNhan => $"Trước ngày {NgaySapNhap} " +
                                                       $"là thửa đất số {SoThua} " +
                                                       $"tờ bản đồ số {ToBanDo} {DonViHanhChinh}";

    public static string DefaultDonViHanhChinhBiSapNhap => $"Nay là {DonViHanhChinh}";
}