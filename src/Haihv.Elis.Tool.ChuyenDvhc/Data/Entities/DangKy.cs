using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

[Table("DangKyQSDD")]
public class DangKy
{
    [Column("MaDangKy", TypeName = "bigint")]
    [Key]
    public long MaDangKy { get; set; }

    [Column("MaThuaDat", TypeName = "bigint")]
    public long MaThuaDat { get; set; }

    [Column("MaGuid", TypeName = "nvarchar(36)")]
    public string MaGuid { get; set; } = string.Empty;
}

[Table("DangKyQSDDLS")]
public class DangKyLichSu
{
    [Column("MaDangKyLS", TypeName = "bigint")]
    [Key]
    public long MaDangKyLs { get; set; }

    [Column("MaThuaDatLS", TypeName = "bigint")]
    public long MaThuaDatLs { get; set; }

    [Column("MaGuid", TypeName = "nvarchar(36)")]
    public string MaGuid { get; set; } = string.Empty;
}

[Table("DangKy_TheChap")]
public class DangKyTheChap
{
    [Column("Ma", TypeName = "bigint")]
    [Key]
    public long Ma { get; set; }

    [Column("MaDangKy", TypeName = "bigint")]
    public long MaDangKy { get; set; }
}

[Table("DangKy_GopVon")]
public class DangKyGopVon
{
    [Column("Ma", TypeName = "bigint")]
    [Key]
    public long Ma { get; set; }

    [Column("MaDangKy", TypeName = "bigint")]
    public long MaDangKy { get; set; }
}