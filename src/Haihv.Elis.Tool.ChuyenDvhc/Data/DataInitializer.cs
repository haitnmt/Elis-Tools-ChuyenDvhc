using Dapper;
using Microsoft.Data.SqlClient;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data;

/// <summary>
/// Lớp mở rộng để khởi tạo dữ liệu.
/// </summary>
public class DataInitializer(string connectionString)
{
    private const string TableName = "AuditChuyenDvhc";

    /// <summary>
    /// Tạo hoặc thay đổi bảng AuditChuyenDvhc.
    /// </summary>
    /// <returns>Trả về một nhiệm vụ không đồng bộ.</returns>
    public async Task CreatedOrAlterAuditTable()
    {
        var columns = new Dictionary<string, string>
        {
            { "Id", "uniqueidentifier" },
            { "Table", "nvarchar(255)" },
            { "RowId", "nvarchar(36)" },
            { "OldValue", "nvarchar(max)" },
            { "NewValue", "nvarchar(max)" },
            { "MaDvhc", "int" },
            { "ActivityTime", "datetime" }
        };

        await using var dbConnection = new SqlConnection(connectionString);

        // Kiểm tra xem bảng AuditChuyenDvhc có tồn tại hay không
        if (!await CheckTableExistAsync(dbConnection, TableName))
        {
            // Nếu bảng không tồn tại thì tạo bảng mới
            var createTableSql = columns.Aggregate($"CREATE TABLE {TableName} (",
                (current, column) => current + $"{column.Key} {column.Value},");
            createTableSql = createTableSql.TrimEnd(',') + ")";
            await dbConnection.ExecuteAsync(createTableSql);
        }
        else
        {
            // Xóa các cột không cần thiết
            var columnsInDb = await GetColumnNamesAsync(dbConnection, TableName);
            foreach (var column in columnsInDb.Where(column => !columns.ContainsKey(column)))
            {
                var sql = $"ALTER TABLE {TableName} DROP COLUMN {column}";
                await dbConnection.ExecuteAsync(sql);
            }

            foreach (var column in columns)
            {
                // Kiểm tra nếu cột đã tồn tại, bỏ qua
                if (await CheckColumnExistAsync(dbConnection, TableName, column.Key)) continue;

                // Xây dựng câu SQL động
                var sqlAddCol = $"ALTER TABLE {TableName} ADD {column.Key} {column.Value}";
                // Thực thi câu lệnh SQL
                await dbConnection.ExecuteAsync(sqlAddCol);
            }
        }
    }

    /// <summary>
    /// Kiểm tra xem bảng có tồn tại trong cơ sở dữ liệu hay không.
    /// </summary>
    /// <param name="dbConnection">Kết nối cơ sở dữ liệu.</param>
    /// <param name="tableName">Tên bảng cần kiểm tra.</param>
    /// <returns>Trả về true nếu bảng tồn tại, ngược lại trả về false.</returns>
    private static async Task<bool> CheckTableExistAsync(SqlConnection dbConnection, string tableName)
    {
        const string sql = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName";
        return await dbConnection.QuerySingleOrDefaultAsync<int>(sql, new { TableName = tableName }) == 1;
    }

    /// <summary>
    /// Kiểm tra xem cột có tồn tại trong bảng hay không.
    /// </summary>
    /// <param name="dbConnection">Kết nối cơ sở dữ liệu.</param>
    /// <param name="tableName">Tên bảng.</param>
    /// <param name="columnName">Tên cột.</param>
    /// <returns>Trả về true nếu cột tồn tại, ngược lại trả về false.</returns>
    private static async Task<bool> CheckColumnExistAsync(SqlConnection dbConnection,
        string tableName, string columnName)
    {
        const string sql =
            "SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName";
        return await dbConnection.QuerySingleOrDefaultAsync<int>(sql,
                   new { TableName = tableName, ColumnName = columnName })
               == 1;
    }

    private static async Task<List<string>> GetColumnNamesAsync(SqlConnection dbConnection, string tableName)
    {
        const string sql = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName";
        return (await dbConnection.QueryAsync<string>(sql, new { TableName = tableName })).ToList();
    }
}