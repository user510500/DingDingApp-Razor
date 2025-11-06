using Microsoft.AspNetCore.Mvc;
using DingDingApp.Services;
using Microsoft.AspNetCore.Authorization;

namespace DingDingApp.Controllers
{
    public class MessageController : Controller
    {
        private readonly IMessageService _messageService;
        private readonly IUserService _userService;
        private readonly ILogger<MessageController> _logger;

        public MessageController(
            IMessageService messageService,
            IUserService userService,
            ILogger<MessageController> logger)
        {
            _messageService = messageService;
            _userService = userService;
            _logger = logger;
        }

        // 检查登录状态
        private bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        }

        public async Task<IActionResult> Index()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var logs = await _messageService.GetMessageLogsAsync();
            return View(logs);
        }

        public async Task<IActionResult> SendToAll()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToAll(string content)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                ModelState.AddModelError("", "消息内容不能为空");
                return View();
            }

            try
            {
                var result = await _messageService.SendMessageToAllAsync(content);
                if (result)
                {
                    TempData["SuccessMessage"] = "消息发送成功";
                }
                else
                {
                    TempData["ErrorMessage"] = "消息发送失败";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送全体消息失败");
                TempData["ErrorMessage"] = "发送消息时发生错误: " + ex.Message;
                return View();
            }
        }

        public async Task<IActionResult> SendToUser()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var users = await _userService.GetAllUsersAsync();
            ViewBag.Users = users;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToUser(string userId, string content)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                ModelState.AddModelError("", "请选择接收用户");
                var users = await _userService.GetAllUsersAsync();
                ViewBag.Users = users;
                return View();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                ModelState.AddModelError("", "消息内容不能为空");
                var users = await _userService.GetAllUsersAsync();
                ViewBag.Users = users;
                return View();
            }

            try
            {
                var result = await _messageService.SendMessageToUserAsync(userId, content);
                if (result)
                {
                    TempData["SuccessMessage"] = "消息发送成功";
                }
                else
                {
                    TempData["ErrorMessage"] = "消息发送失败";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送用户消息失败");
                TempData["ErrorMessage"] = "发送消息时发生错误: " + ex.Message;
                var users = await _userService.GetAllUsersAsync();
                ViewBag.Users = users;
                return View();
            }
        }
    }
}

