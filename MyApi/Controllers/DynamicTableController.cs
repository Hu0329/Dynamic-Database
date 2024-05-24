using Microsoft.AspNetCore.Mvc;
using MyApi.Data;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace MyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DynamicTableController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DynamicTableController(ApplicationDbContext context)
        {
            _context = context;
        }



        [HttpPost("create")]
        public async Task<IActionResult> CreateTable([FromQuery] string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return BadRequest("Table name is required.");
            }

            var createTableSql = $"CREATE TABLE {tableName} (id INT PRIMARY KEY AUTO_INCREMENT)";
            await _context.Database.ExecuteSqlRawAsync(createTableSql);
            return Ok(new { message = $"Table {tableName} created successfully." });
        }

        [HttpGet("get/{tableName}")]
        public async Task<IActionResult> GetTableData(string tableName)
        {
            var result = new List<Dictionary<string, object>>();

            using (var connection = new MySqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                await connection.OpenAsync();
                var command = new MySqlCommand($"SELECT * FROM {tableName}", connection);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.GetValue(i);
                        }
                        result.Add(row);
                    }
                }
            }

            return Ok(result);
        }



        [HttpPost("insert/{tableName}")]
        public async Task<IActionResult> InsertIntoTable(string tableName, [FromBody] Dictionary<string, object> rowData)
        {
            if (rowData.ContainsKey("Id"))
            {
                rowData.Remove("Id");
            }
            var columns = string.Join(",", rowData.Keys);
            var values = string.Join(",", rowData.Values.Select(v => $"'{v}'"));
            var insertSql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
            await _context.Database.ExecuteSqlRawAsync(insertSql);
            return Ok(new { message = "Row inserted successfully." });
        }

        [HttpPut("update/{tableName}/{id}")]
        public async Task<IActionResult> UpdateTableData(string tableName, int id, [FromBody] Dictionary<string, object> rowData)
        {
            var setClause = string.Join(",", rowData.Select(kv => $"{kv.Key} = '{kv.Value}'"));
            var updateSql = $"UPDATE {tableName} SET {setClause} WHERE Id = {id}";
            await _context.Database.ExecuteSqlRawAsync(updateSql);
            return Ok(new { message = "Row updated successfully." });
        }

        [HttpDelete("delete/{tableName}/{id}")]
        public async Task<IActionResult> DeleteFromTable(string tableName, int id)
        {
            var deleteSql = $"DELETE FROM {tableName} WHERE Id = {id}";
            await _context.Database.ExecuteSqlRawAsync(deleteSql);
            return Ok(new { message = "Row deleted successfully." });
        }

        [HttpPost("addColumn")]
        public async Task<IActionResult> AddColumn([FromQuery] string tableName, [FromQuery] string columnName, [FromQuery] string columnType)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName) || string.IsNullOrWhiteSpace(columnType))
            {
                return BadRequest("Table name, column name, and column type are required.");
            }

            var addColumnSql = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}";
            try
            {
                await _context.Database.ExecuteSqlRawAsync(addColumnSql);
                return Ok(new { message = $"Column {columnName} added to table {tableName} successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error adding column: {ex.Message}" });
            }
        }

        [HttpGet("getAllTables")]
        public async Task<IActionResult> GetAllTables()
        {
            var tables = new List<string>();
            var excludedTables = new List<string> { "__efmigrationshistory", "users" };
            var query = "SHOW TABLES";
            using (var connection = new MySqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                await connection.OpenAsync();
                var command = new MySqlCommand(query, connection);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var tableName = reader.GetString(0);
                        if (!excludedTables.Contains(tableName.ToLower()))
                        {
                            tables.Add(tableName);
                        }
                    }
                }
            }
            return Ok(tables);
        }

        [HttpGet("columns/{tableName}")]
        public async Task<IActionResult> GetTableColumns(string tableName)
        {
            var columns = new List<string>();
            var query = $"SHOW COLUMNS FROM {tableName}";
            try
            {
                using (var connection = new MySqlConnection(_context.Database.GetDbConnection().ConnectionString))
                {
                    await connection.OpenAsync();
                    var command = new MySqlCommand(query, connection);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            columns.Add(reader.GetString(0));
                        }
                    }
                }
                return Ok(columns);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching columns", details = ex.Message });
            }
        }


    }
}