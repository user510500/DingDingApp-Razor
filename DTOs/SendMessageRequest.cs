using System.ComponentModel.DataAnnotations;

namespace DingDingApp.DTOs
{
    public class SendMessageRequest
    {
        [Required(ErrorMessage = "消息内容不能为空")]
        public string Content { get; set; } = string.Empty;
    }

    public class SendMessageToUserRequest : SendMessageRequest
    {
        [Required(ErrorMessage = "用户ID不能为空")]
        public string UserId { get; set; } = string.Empty;
    }
}

