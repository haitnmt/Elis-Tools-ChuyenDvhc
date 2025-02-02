using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

[Table("CayLS")]
public sealed class CayLs
{
    [Column("MaCayLS", TypeName = "nvarchar(36)")]
    [Key]
    public string MaCayLs { get; set; } = string.Empty;

    [Column("MaDangKyHT", TypeName = "bigint")]
    public long MaDangKyHt { get; set; }

    [Column("MaDangKyLS", TypeName = "bigint")]
    public long MaDangKyLs { get; set; }
}