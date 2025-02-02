using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

[Table("TS_ThuaDat_TaiSan")]
public class ThuaDatTaiSan
{
    [Column("idTD_TS", TypeName = "nvarchar(36)")]
    [Key]
    public string IdTdTs { get; set; } = string.Empty;

    [Column("idThuaDat", TypeName = "bigint")]
    public long IdThuaDat { get; set; }
}

[Table("TS_LichSu")]
public class TaiSanLichSu
{
    [Column("idBDTS", TypeName = "nvarchar(36)")]
    [Key]
    public string IdBdTs { get; set; } = string.Empty;

    [Column("idThuaDat", TypeName = "bigint")]
    public long IdThuaDat { get; set; }
}