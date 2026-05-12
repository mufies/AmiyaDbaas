namespace AmiyaDbaasManager.DTOs.Response.DbInstance {
    public class QueryResponseDto {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public long ExecutionTimeMs { get; set; }
        public List<string> Columns { get; set; } = new();
        
        public List<Dictionary<string, object>> Rows { get; set; } = new(); 
        public int RowsAffected { get; set; }
    }
}