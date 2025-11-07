using System.ComponentModel.DataAnnotations;

namespace DingDingApp.DTOs
{
    public class CreateUserRequest
    {
        [Required(ErrorMessage = "用户ID不能为空")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "姓名不能为空")]
        public string Name { get; set; } = string.Empty;

        public string? Mobile { get; set; }

        public string? Email { get; set; }

        public string? Department { get; set; }

        public string? Position { get; set; }
    }

    public class UpdateUserRequest
    {
        [Required(ErrorMessage = "用户ID不能为空")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "姓名不能为空")]
        public string Name { get; set; } = string.Empty;

        public string? Mobile { get; set; }

        public string? Email { get; set; }

        public string? Department { get; set; }

        public string? Position { get; set; }
    }

    public class UserResponse
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

