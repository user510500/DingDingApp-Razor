using System.ComponentModel.DataAnnotations;

namespace DingDingApp.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "用户ID")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "姓名")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "手机号")]
        public string? Mobile { get; set; }

        [Display(Name = "邮箱")]
        public string? Email { get; set; }

        [Display(Name = "部门")]
        public string? Department { get; set; }

        [Display(Name = "职位")]
        public string? Position { get; set; }

        [Display(Name = "创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "更新时间")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

