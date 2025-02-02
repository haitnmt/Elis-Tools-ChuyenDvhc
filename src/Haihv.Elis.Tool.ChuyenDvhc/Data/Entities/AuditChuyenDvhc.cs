using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

[Table("AuditChuyenDvhc")]
public sealed class AuditChuyenDvhc
{
    [Key] [Column("Id")] public Guid Id { get; set; }

    [Required]
    [Column("Table")]
    [MaxLength(255)]
    public string Table { get; set; } = string.Empty;

    [Required]
    [Column("RowId")]
    [MaxLength(36)]
    public string RowId { get; set; } = string.Empty;

    [Column("OldValue")] public string OldValue { get; set; } = string.Empty;

    [Column("NewValue")] public string NewValue { get; set; } = string.Empty;

    [Column("MaDvhc")] public int MaDvhc { get; set; }

    [Column("ActivityTime")] public DateTime ActivityTime { get; set; } = DateTime.Now;
}