using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

[Table("GCNQSDD")]
public class GiayChungNhan
{
    [Column("MaGCN", TypeName = "bigint")]
    [Key]
    public long MaGcn { get; set; }

    [Column("MaDangKy", TypeName = "bigint")]
    public long MaDangKy { get; set; }

    [Column("MaGuid", TypeName = "nvarchar(36)")]
    public string MaGuid { get; set; } = string.Empty;
}

[Table("GCNQSDDLS")]
public class GiayChungNhanLichSu
{
    [Column("MaGCNLS", TypeName = "bigint")]
    [Key]
    public long MaGcnLs { get; set; }

    [Column("MaDangKyLS", TypeName = "bigint")]
    public long MaDangKy { get; set; }

    [Column("MaGuid", TypeName = "nvarchar(36)")]
    public string MaGuid { get; set; } = string.Empty;
}

[Table("DangKyMDSDD")]
public class DangKyMdsdd
{
    [Column("MaDKMDSDD", TypeName = "bigint")]
    [Key]
    public long MaDkMdsdd { get; set; }

    [Column("MaGCN", TypeName = "bigint")] public long MaGcn { get; set; }
}

[Table("DangKyMDSDDLS")]
public class DangKyMdsddLichSu
{
    [Column("MaDKMDSDDLS", TypeName = "bigint")]
    [Key]
    public long MaDkMdsddLs { get; set; }

    [Column("MaGCNLS", TypeName = "bigint")]
    public long MaGcnLs { get; set; }
}

[Table("DangKyNGSDD")]
public class DangKyNgsdd
{
    [Column("MaDKNGSDD", TypeName = "bigint")]
    [Key]
    public long MaDkNgsdd { get; set; }

    [Column("MaGCN", TypeName = "bigint")] public long MaGcn { get; set; }
}

[Table("DangKyNGSDDLS")]
public class DangKyNgsddLichSu
{
    [Column("MaDKNGSDDLS", TypeName = "bigint")]
    [Key]
    public long MaDkNgsddLs { get; set; }

    [Column("MaGCNLS", TypeName = "bigint")]
    public long MaGcnLs { get; set; }
}