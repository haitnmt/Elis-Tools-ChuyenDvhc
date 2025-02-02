DECLARE @MaDVHC int = 109508
SELECT MaThuaDat, ThuaDatSo, SoTo, MaToBanDo
FROM (
         SELECT DISTINCT td.MaThuaDat AS MaThuaDat, td.ThuaDatSo, tbd.SoTo, tbd.MaToBanDo
         FROM ThuaDat td
                  INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
         WHERE tbd.MaDVHC = @MaDVHC
         UNION
         SELECT DISTINCT td.MaThuaDatLS AS MaThuaDat,  td.ThuaDatSo, tbd.SoTo, tbd.MaToBanDo
         FROM ThuaDatLS td
                  INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
         WHERE tbd.MaDVHC = @MaDVHC
     ) AS CombinedResult
ORDER BY MaThuaDat