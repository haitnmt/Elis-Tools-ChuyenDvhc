namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

public class ThamChieuToBanDo
{
    public int Id { get; set; }
    public int MaDvhcTruoc { get; set; }
    public string TenDvhcTruoc { get; set; } = string.Empty;
    public int MaDvhcSau { get; set; }
    public string TenDvhcSau { get; set; } = string.Empty;
    public long MaToBanDoTruoc { get; set; }
    public int MaToBanDoSau { get; set; }
    public string SoToBanDoTruoc { get; set; } = string.Empty;
    public string SoToBanDoSau { get; set; } = string.Empty;
}

