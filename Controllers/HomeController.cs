using Microsoft.AspNetCore.Mvc;
using DingDingApp.Services;
using System.Diagnostics;

namespace DingDingApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDingTalkService _dingTalkService;

        public HomeController(ILogger<HomeController> logger, IDingTalkService dingTalkService)
        {
            _logger = logger;
            _dingTalkService = dingTalkService;
        }

        public async Task<IActionResult> Index()
        {
            // 检查是否已登录
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                // 获取扫码登录URL
                var qrCodeUrl = await _dingTalkService.GetQrCodeUrlAsync();
                ViewBag.QrCodeUrl = qrCodeUrl;
                return View();
            }

            return RedirectToAction("Index", "User");
        }

        public async Task<IActionResult> Callback(string code, string state)
        {
            if (string.IsNullOrEmpty(code))
            {
                return RedirectToAction("Index");
            }

            try
            {
                var userInfo = await _dingTalkService.GetUserInfoByCodeAsync(code);
                if (userInfo != null && userInfo.ContainsKey("openid"))
                {
                    // 保存登录信息到Session
                    HttpContext.Session.SetString("UserId", userInfo["openid"]?.ToString() ?? "");
                    HttpContext.Session.SetString("UserName", userInfo["nick"]?.ToString() ?? "");
                    return RedirectToAction("Index", "User");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录回调处理失败");
            }

            return RedirectToAction("Index");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}

