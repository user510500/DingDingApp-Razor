namespace DingDingApp.Services
{
    public interface IDingTalkService
    {
        Task<string> GetAccessTokenAsync();
        Task<string> GetQrCodeUrlAsync();
        Task<Dictionary<string, object>?> GetUserInfoByCodeAsync(string code);
        Task<bool> SendMessageToAllAsync(string content);
        Task<bool> SendMessageToUserAsync(string userId, string content);
    }
}

