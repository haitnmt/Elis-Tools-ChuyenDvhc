DECLARE @MaDVHC int = 109184
SELECT COUNT(DISTINCT MaDangKy) AS Total
FROM (
         SELECT DISTINCT dk.MaDangKy AS MaDangKy
         FROM DangKyQSDD dk
                  INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
                  INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
         WHERE tbd.MaDVHC = @MaDVHC
         UNION ALL
         SELECT DISTINCT dk.MaDangKyLS AS MaDangKy
         FROM DangKyQSDDLS dk
                  INNER JOIN ThuaDatLS td ON dk.MaThuaDatLS = td.MaThuaDatLS
                  INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
         WHERE tbd.MaDVHC = @MaDVHC
         UNION ALL
         SELECT DISTINCT dk.MaDangKyLS AS MaDangKy
         FROM  DangKyQSDDLS dk
                   INNER JOIN ThuaDat td ON dk.MaThuaDatLS = td.MaThuaDat
                   INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
         WHERE tbd.MaDVHC = @MaDVHC
     ) AS CombinedResults