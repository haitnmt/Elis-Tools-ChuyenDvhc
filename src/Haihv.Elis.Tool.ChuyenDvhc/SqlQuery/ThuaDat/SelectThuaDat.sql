DECLARE
@MaDVHC int = 109496
SELECT DISTINCT MaThuaDat
FROM (SELECT DISTINCT td.MaThuaDat AS MaThuaDat
      FROM ThuaDat td
               INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
      WHERE tbd.MaDVHC = @MaDVHC
      UNION ALL
      SELECT DISTINCT td.MaThuaDatLS AS MaThuaDat
      FROM ThuaDatLS td
               INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
      WHERE tbd.MaDVHC = @MaDVHC) AS CombinedResult
ORDER BY MaThuaDat