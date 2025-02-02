using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

public sealed record ThuaDatCapNhat(long MaThuaDat, string ThuaDatSo, string ToBanDo, string TenDonViHanhChinh);

[Table("ThuaDat")]
public sealed class ThuaDat
{
    [Column("MaThuaDat", TypeName = "bigint")]
    [Key]
    public long MaThuaDat { get; set; }

    [Column("MaToBanDo", TypeName = "bigint")]
    public long MaToBanDo { get; set; }

    [Column("ThuaDatSo", TypeName = "char(10)")]
    [MaxLength(10)]
    public required string ThuaDatSo { get; set; }

    [Column("GhiChu", TypeName = "nvarchar(2000)")]
    [MaxLength(2000)]
    public string? GhiChu { get; set; }
}

[Table("ThuaDatLS")]
public sealed class ThuaDatLichSu
{
    [Column("MaThuaDatLS", TypeName = "bigint")]
    [Key]
    public long MaThuaDatLs { get; set; }

    [Column("MaToBanDo", TypeName = "bigint")]
    public long MaToBanDo { get; set; }

    [Column("ThuaDatSo", TypeName = "char(10)")]
    [MaxLength(10)]
    public required string ThuaDatSo { get; set; }

    [Column("GhiChu", TypeName = "nvarchar(2000)")]
    [MaxLength(2000)]
    public string? GhiChu { get; set; }
}