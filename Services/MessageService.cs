using Microsoft.EntityFrameworkCore;
using DingDingApp.Data;
using DingDingApp.Models;

namespace DingDingApp.Services
{
    public class MessageService : IMessageService
    {
        private readonly IDingTalkService _dingTalkService;
        private readonly ApplicationDbContext _context;

        public MessageService(IDingTalkService dingTalkService, ApplicationDbContext context)
        {
            _dingTalkService = dingTalkService;
            _context = context;
        }

        public async Task<bool> SendMessageToAllAsync(string content)
        {
            try
            {
                var result = await _dingTalkService.SendMessageToAllAsync(content);
                
                // 记录日志
                var log = new MessageLog
                {
                    MessageType = "all",
                    Content = content,
                    SentAt = DateTime.Now,
                    IsSuccess = result
                };
                _context.MessageLogs.Add(log);
                await _context.SaveChangesAsync();

                return result;
            }
            catch (Exception ex)
            {
                var log = new MessageLog
                {
                    MessageType = "all",
                    Content = content,
                    SentAt = DateTime.Now,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
                _context.MessageLogs.Add(log);
                await _context.SaveChangesAsync();
                return false;
            }
        }

        public async Task<bool> SendMessageToUserAsync(string userId, string content)
        {
            try
            {
                var result = await _dingTalkService.SendMessageToUserAsync(userId, content);
                
                // 记录日志
                var log = new MessageLog
                {
                    MessageType = "specific",
                    TargetUserId = userId,
                    Content = content,
                    SentAt = DateTime.Now,
                    IsSuccess = result
                };
                _context.MessageLogs.Add(log);
                await _context.SaveChangesAsync();

                return result;
            }
            catch (Exception ex)
            {
                var log = new MessageLog
                {
                    MessageType = "specific",
                    TargetUserId = userId,
                    Content = content,
                    SentAt = DateTime.Now,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
                _context.MessageLogs.Add(log);
                await _context.SaveChangesAsync();
                return false;
            }
        }

        public async Task<List<MessageLog>> GetMessageLogsAsync()
        {
            return await _context.MessageLogs
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
        }
    }
}

