using Microsoft.AspNetCore.Mvc;
using DingDingApp.Models;
using DingDingApp.Services;
using Microsoft.AspNetCore.Authorization;

namespace DingDingApp.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
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

            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        public IActionResult Create()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _userService.CreateUserAsync(user);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "创建用户失败");
                    ModelState.AddModelError("", "创建用户失败: " + ex.Message);
                }
            }

            return View(user);
        }

        public async Task<IActionResult> Edit(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var updatedUser = await _userService.UpdateUserAsync(id, user);
                    if (updatedUser == null)
                    {
                        return NotFound();
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "更新用户失败");
                    ModelState.AddModelError("", "更新用户失败: " + ex.Message);
                }
            }

            return View(user);
        }

        public async Task<IActionResult> Delete(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var result = await _userService.DeleteUserAsync(id);
                if (!result)
                {
                    return NotFound();
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败");
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }
    }
}

