using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

[Table("DVHC")]
public sealed class Dvhc
{
    [Column("MaDVHC", TypeName = "int")]
    [Key]
    public int MaDvhc { get; set; }

    [Column("Ten", TypeName = "nvarchar(50")]
    [MaxLength(50)]
    public required string Ten { get; set; }

    [Column("MaTinh", TypeName = "int")] public int MaTinh { get; set; }
    [Column("MaHuyen", TypeName = "int")] public int MaHuyen { get; set; }
    [Column("MaXa", TypeName = "int")] public int MaXa { get; set; }
}

/// <summary>
/// Ghi chú: Sử dụng record để định nghĩa một bản ghi dữ liệu.
/// </summary>
/// <param name="MaDvhc">
/// Mã đơn vị hành chính trong cơ sở dữ liệu ELIS SQL
/// </param>
/// <param name="Ma">
/// Mã được quy định trong quyết định của Bộ Nội vụ
/// </param>
/// <param name="Ten">
/// Tên của đơn vị hành chính
/// </param>
public sealed record DvhcRecord(int MaDvhc, int Ma, string Ten);