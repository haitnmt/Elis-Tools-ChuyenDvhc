DECLARE @MaDVHC int = 109526
SELECT COUNT(DISTINCT MaGCN) AS Total
FROM
      (SELECT DISTINCT gcn.MaGCN AS MaGCN
       FROM GCNQSDD gcn
       INNER JOIN DangKyQSDD dk ON gcn.MaDangKy = dk.MaDangKy
       INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
       INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
       WHERE MaDVHC = @MaDVHC
       UNION SELECT DISTINCT gcn.MaGCNLS AS MaGCN
       FROM GCNQSDDLS gcn
       INNER JOIN DangKyQSDD dk ON gcn.MaDangKyLS = dk.MaDangKy
       INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
       INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
       WHERE MaDVHC = @MaDVHC
       UNION SELECT DISTINCT gcn.MaGCNLS AS MaGCN
       FROM GCNQSDDLS gcn
       INNER JOIN DangKyQSDDLS dk ON gcn.MaDangKyLS = dk.MaDangKyLS
       INNER JOIN ThuaDatLS td ON dk.MaThuaDatLS = td.MaThuaDatLS
       INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
       WHERE MaDVHC = @MaDVHC
       UNION SELECT DISTINCT gcn.MaGCNLS AS MaGCN
       FROM GCNQSDDLS gcn
       INNER JOIN DangKyQSDDLS dk ON gcn.MaDangKyLS = dk.MaDangKyLS
       INNER JOIN ThuaDat td ON dk.MaThuaDatLS = td.MaThuaDat
       INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
       WHERE MaDVHC = @MaDVHC) AS CombinedResult