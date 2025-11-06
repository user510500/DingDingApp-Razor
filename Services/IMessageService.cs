using DingDingApp.Models;

namespace DingDingApp.Services
{
    public interface IMessageService
    {
        Task<bool> SendMessageToAllAsync(string content);
        Task<bool> SendMessageToUserAsync(string userId, string content);
        Task<List<MessageLog>> GetMessageLogsAsync();
    }
}

