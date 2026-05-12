using System.Data;
using System.Data.Common;
using System.Diagnostics;
using AmiyaDbaasManager.DTOs.Request.DbInstance;
using AmiyaDbaasManager.DTOs.Response.DbInstance;
using AmiyaDbaasManager.Enums;
using AmiyaDbaasManager.Models;
using AmiyaDbaasManager.Repositories.Interfaces;
using AmiyaDbaasManager.Services.Interfaces;
using Microsoft.Data.SqlClient;
using MongoDB.Bson;
using MongoDB.Driver;
using MySqlConnector;
using Npgsql;

namespace AmiyaDbaasManager.Services;

public class SqlQueryRunService : IQueryRunService
{
    private readonly IDbInstanceRepo _dbInstanceRepo;
    private readonly IEncryptionService _encryptionService;

    public SqlQueryRunService(IDbInstanceRepo dbInstanceRepo, IEncryptionService encryptionService)
    {
        _dbInstanceRepo = dbInstanceRepo;
        _encryptionService = encryptionService;
    }

    public async Task<QueryResponseDto> RunQueryAsync(CreateQueryRequestDto request)
    {
        var response = new QueryResponseDto();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!Guid.TryParse(request.DbInstanceId, out var instanceId))
            {
                throw new Exception("Invalid instance ID format.");
            }

            var instance = await _dbInstanceRepo.GetById(instanceId);
            if (instance == null)
            {
                throw new Exception("Instance not found.");
            }

            // Check ownership
            if (instance.UserId != request.UserId)
            {
                throw new Exception("You don't have permission to query this instance.");
            }

            if (instance.Status != "Running")
            {
                throw new Exception("Instance is not running.");
            }

            // Xử lý riêng cho MongoDB
            if (instance.Engine.Equals("MongoDB", StringComparison.OrdinalIgnoreCase) || 
                instance.Engine.Equals("Mongo", StringComparison.OrdinalIgnoreCase))
            {
                return await RunMongoQueryAsync(instance, request.Query, stopwatch);
            }

            // 1. Tạo connection dựa trên Engine (SQL)
            using DbConnection connection = CreateConnection(instance);
            await connection.OpenAsync();

            // 2. Tạo command và thiết lập Timeout (Rất quan trọng)
            using DbCommand command = connection.CreateCommand();
            command.CommandText = request.Query;
            command.CommandTimeout = 15; // Giới hạn 15 giây

            // 3. Phân biệt loại câu lệnh
            if (IsSelectQuery(request.Query))
            {
                using DbDataReader reader = await command.ExecuteReaderAsync();
                
                // Lấy danh sách tên cột
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    response.Columns.Add(reader.GetName(i));
                }

                // Đọc dữ liệu (giới hạn số dòng để tránh OOM)
                int maxRows = 1000;
                int rowCount = 0;
                
                while (await reader.ReadAsync() && rowCount < maxRows)
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[response.Columns[i]] = value ?? string.Empty;
                    }
                    response.Rows.Add(row);
                    rowCount++;
                }
            }
            else
            {
                // Thực thi các lệnh không trả về bảng (INSERT, UPDATE, DELETE, CREATE...)
                response.RowsAffected = await command.ExecuteNonQueryAsync();
            }

            response.IsSuccess = true;
        }
        catch (Exception ex)
        {
            response.IsSuccess = false;
            response.ErrorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            response.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return response;
    }

    private DbConnection CreateConnection(DbInstance instance)
    {
        var engineEnum = DbEngineConfig.ParseEngine(instance.Engine);
        
        // Giải mã password trước khi build chuỗi kết nối
        var plainTextPassword = _encryptionService.Decrypt(instance.Password);

        // Use DbEngineConfig to build connection string
        var connectionString = DbEngineConfig.BuildConnectionString(
            engineEnum,
            instance.Host,
            instance.AllocatedPort,
            plainTextPassword
        );

        return engineEnum switch
        {
            DbEngine.MySQL => new MySqlConnection(connectionString),
            DbEngine.PostgreSQL => new NpgsqlConnection(connectionString),
            DbEngine.MSSQL => new SqlConnection(connectionString),
            DbEngine.MongoDB => throw new NotSupportedException("MongoDB requires a different driver approach and does not support standard SQL commands."),
            _ => throw new NotSupportedException($"Engine {instance.Engine} is not supported.")
        };
    }

    private bool IsSelectQuery(string sql)
    {
        var upperSql = sql.TrimStart().ToUpper();
        return upperSql.StartsWith("SELECT") || upperSql.StartsWith("SHOW") || upperSql.StartsWith("EXPLAIN");
    }

    private async Task<QueryResponseDto> RunMongoQueryAsync(DbInstance instance, string query, Stopwatch stopwatch)
    {
        var response = new QueryResponseDto();
        try
        {
            var engineEnum = DbEngineConfig.ParseEngine(instance.Engine);
            
            // Giải mã password
            var plainTextPassword = _encryptionService.Decrypt(instance.Password);
            
            var connectionString = DbEngineConfig.BuildConnectionString(
                engineEnum,
                instance.Host,
                instance.AllocatedPort,
                plainTextPassword
            );

            var client = new MongoClient(connectionString);
            var dbName = DbEngineConfig.DefaultDatabase.MongoDB;
            var database = client.GetDatabase(dbName);

            // Parse raw query (JSON) to BsonDocument
            // VD: { "find": "users", "filter": { "status": "active" } }
            var command = BsonDocument.Parse(query);

            var result = await database.RunCommandAsync<BsonDocument>(new BsonDocumentCommand<BsonDocument>(command));

            // Nếu kết quả trả về có chứa 'cursor' (kết quả của lệnh find)
            if (result.Contains("cursor") && result["cursor"].IsBsonDocument && result["cursor"].AsBsonDocument.Contains("firstBatch"))
            {
                var batch = result["cursor"]["firstBatch"].AsBsonArray;
                var columnsSet = new HashSet<string>();

                // Quét qua các documents để lấy tên tất cả các field làm Column name
                foreach (var item in batch)
                {
                    if (item.IsBsonDocument)
                    {
                        foreach (var element in item.AsBsonDocument.Elements)
                        {
                            columnsSet.Add(element.Name);
                        }
                    }
                }

                response.Columns = columnsSet.ToList();

                // Lấy dữ liệu từng Row
                foreach (var item in batch)
                {
                    if (item.IsBsonDocument)
                    {
                        var row = new Dictionary<string, object>();
                        foreach (var col in response.Columns)
                        {
                            if (item.AsBsonDocument.Contains(col))
                            {
                                var val = item.AsBsonDocument[col];
                                row[col] = ConvertBsonToObject(val) ?? string.Empty;
                            }
                            else
                            {
                                row[col] = string.Empty;
                            }
                        }
                        response.Rows.Add(row);
                    }
                }
            }
            else
            {
                // Nếu không phải cursor (VD: response của lệnh insert/update), trả về chuỗi JSON kết quả
                response.Columns.Add("CommandResult");
                response.Rows.Add(new Dictionary<string, object> { { "CommandResult", result.ToJson() } });
            }

            response.IsSuccess = true;
        }
        catch (Exception ex)
        {
            response.IsSuccess = false;
            response.ErrorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            response.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return response;
    }

    private object? ConvertBsonToObject(BsonValue value)
    {
        if (value.IsBsonNull) return null;
        if (value.IsString) return value.AsString;
        if (value.IsInt32) return value.AsInt32;
        if (value.IsInt64) return value.AsInt64;
        if (value.IsDouble) return value.AsDouble;
        if (value.IsBoolean) return value.AsBoolean;
        if (value.IsObjectId) return value.AsObjectId.ToString();
        if (value.IsBsonDateTime) return value.ToUniversalTime();

        // Object phức tạp (array, sub-document) sẽ gom lại thành chuỗi JSON
        return value.ToJson();
    }
}