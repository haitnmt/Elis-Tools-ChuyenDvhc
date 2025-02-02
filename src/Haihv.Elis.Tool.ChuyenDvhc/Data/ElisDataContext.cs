// using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
// using Microsoft.EntityFrameworkCore;
//
// namespace Haihv.Elis.Tool.ChuyenDvhc.Data;
//
// public sealed class ElisDataContext(string connectionString) : DbContext
// {
//     public DbSet<Dvhc> Dvhcs { get; set; } = null!;
//     public DbSet<ToBanDo> ToBanDos { get; set; } = null!;
//     public DbSet<ThuaDat> ThuaDats { get; set; } = null!;
//     public DbSet<ThuaDatCu> ThuaDatCus { get; set; } = null!;
//     public DbSet<ThuaDatLichSu> ThuaDatLichSus { get; set; } = null!;
//     public DbSet<DangKy> DangKys { get; set; } = null!;
//     public DbSet<DangKyLichSu> DangKyLichSus { get; set; } = null!;
//     public DbSet<DangKyTheChap> DangKyTheChaps { get; set; } = null!;
//     public DbSet<DangKyGopVon> DangKyGopVons { get; set; } = null!;
//     public DbSet<CayLs> CayLs { get; set; } = null!;
//     public DbSet<GiayChungNhan> GiayChungNhans { get; set; } = null!;
//     public DbSet<GiayChungNhanLichSu> GiayChungNhanLichSus { get; set; } = null!;
//     public DbSet<DangKyMdsdd> DangKyMdsdds { get; set; } = null!;
//     public DbSet<DangKyMdsddLichSu> DangKyMdsddLichSus { get; set; } = null!;
//     public DbSet<DangKyNgsdd> DangKyNgsdds { get; set; } = null!;
//     public DbSet<DangKyNgsddLichSu> DangKyNgsddLichSus { get; set; } = null!;
//     public DbSet<ThuaDatTaiSan> ThuaDatTaiSans { get; set; } = null!;
//     public DbSet<TaiSanLichSu> TaiSanLichSus { get; set; } = null!;
//
//     public DbSet<AuditChuyenDvhc> AuditChuyenDvhcs { get; set; } = null!;
//
//     protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//     {
//         optionsBuilder.UseSqlServer(connectionString);
//     }
// }