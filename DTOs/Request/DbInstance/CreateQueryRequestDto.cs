using System.ComponentModel.DataAnnotations;

namespace AmiyaDbaasManager.DTOs.Request.DbInstance {
    public class CreateQueryRequestDto {
        [Required(ErrorMessage = "Query is required")]
        public string Query { get; set; } = string.Empty;
        [Required(ErrorMessage = "Instance ID is required")]
        public string DbInstanceId {get; set;} = string.Empty;
        [Required(ErrorMessage = "User ID is required")]
        public Guid UserId {get; set;} 
    }
}