using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using DingDingApp.Data;
using DingDingApp.Services;
using DingDingApp.Models;
using DingDingApp.DTOs;
using Microsoft.AspNetCore.Mvc;
using Radzen;
using Serilog;
using Microsoft.Extensions.Options;
using DingDingApp.Options;

var builder = WebApplication.CreateBuilder(args);

// 添加 Blazor Server 支持
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// 配置数据库
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 33))));

// 添加HttpClientFactory，配置 Cookie 处理
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor(); // 添加 HttpContextAccessor 支持

// 注册服务
builder.Services.AddScoped<IDingTalkService, DingTalkService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMessageService, MessageService>();

// 注册 API 服务（用于前端调用）
builder.Services.AddScoped<ApiService>();

// 注册 Radzen 服务
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

// 配置Session（用于登录状态管理）
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax; // 添加 SameSite 设置
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // 允许 HTTP
});

// 如果其他服务需要，可在此注册 HttpContextAccessor

// 配置钉钉应用信息
builder.Services.Configure<DingDingApp.Options.DingTalkOptions>(builder.Configuration.GetSection("DingTalk"));

// 添加 Swagger/OpenAPI 支持
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "钉钉管理系统 API",
        Version = "v1",
        Description = "基于 Minimal API 的钉钉集成管理系统"
    });
});

// 添加 CORS 支持
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 配置 Serilog 文件日志
try
{
    // 确保 logs 目录存在
    var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
    if (!Directory.Exists(logsDir))
    {
        Directory.CreateDirectory(logsDir);
    }

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: Path.Combine(logsDir, "dingding-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    builder.Host.UseSerilog();
    
    Log.Information("Serilog 日志已配置，日志文件位置: {LogPath}", logsDir);
}
catch (Exception ex)
{
    // 如果 Serilog 配置失败，使用默认日志
    Console.WriteLine($"警告：Serilog 配置失败，将使用默认日志: {ex.Message}");
}

var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "钉钉管理系统 API v1");
        c.RoutePrefix = "swagger"; // Swagger UI 路径改为 /swagger
    });
}
else
{
    app.UseExceptionHandler("/Error");
}

// app.UseHttpsRedirection(); // 在Docker中只使用HTTP，所以注释掉HTTPS重定向
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseCors("AllowAll");
app.UseAuthorization();

// 确保数据库已创建（暂时禁用，避免阻塞启动）
try
{
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
    }
}
catch (Exception ex)
{
    // 记录错误但不阻塞应用启动
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "数据库初始化失败，但应用将继续启动");
}

// ==================== 认证相关 API ====================
// 注意：API 端点必须在 Blazor 路由之前注册

// 获取二维码登录URL
app.MapGet("/api/auth/qrcode", async (IDingTalkService dingTalkService, HttpRequest request) =>
{
    try
    {
        // 获取当前请求的基础URL（协议+主机+端口）
        var baseUrl = $"{request.Scheme}://{request.Host}";
        var qrCodeUrl = await dingTalkService.GetQrCodeUrlAsync(baseUrl);
        return Results.Ok(ApiResponse<AuthResponse>.SuccessResult(
            new AuthResponse { QrCodeUrl = qrCodeUrl },
            "获取二维码成功"
        ));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ApiResponse<AuthResponse>.FailResult($"获取二维码失败: {ex.Message}"));
    }
})
.WithName("GetQrCode")
.WithTags("认证");

// 诊断API - 验证钉钉配置
app.MapGet("/api/auth/diagnose", async (IDingTalkService dingTalkService, IOptions<DingTalkOptions> options, ILogger<Program> logger) =>
{
    try
    {
        var diagnoseResults = new Dictionary<string, object>();
        
        // 1. 检查配置是否存在
        diagnoseResults["AppKey配置"] = string.IsNullOrEmpty(options.Value.AppKey) ? "未配置" : $"已配置 (长度: {options.Value.AppKey.Length})";
        diagnoseResults["AppSecret配置"] = string.IsNullOrEmpty(options.Value.AppSecret) ? "未配置" : $"已配置 (长度: {options.Value.AppSecret.Length})";
        diagnoseResults["AppKey值"] = options.Value.AppKey;
        
        // 2. 测试普通API的access_token
        try
        {
            var accessToken = await dingTalkService.GetAccessTokenAsync();
            diagnoseResults["普通API测试"] = "成功";
            diagnoseResults["AccessToken"] = accessToken.Substring(0, Math.Min(20, accessToken.Length)) + "...";
        }
        catch (Exception ex)
        {
            diagnoseResults["普通API测试"] = $"失败: {ex.Message}";
        }
        
        // 3. 生成二维码URL（测试配置）
        try
        {
            var qrCodeUrl = await dingTalkService.GetQrCodeUrlAsync("http://localhost:54507");
            diagnoseResults["二维码URL生成"] = "成功";
            diagnoseResults["二维码URL"] = qrCodeUrl;
        }
        catch (Exception ex)
        {
            diagnoseResults["二维码URL生成"] = $"失败: {ex.Message}";
        }
        
        // 4. 提供配置建议
        var suggestions = new List<string>();
        if (string.IsNullOrEmpty(options.Value.AppKey) || options.Value.AppKey == "appKey")
        {
            suggestions.Add("AppKey 未正确配置，请检查 appsettings.json");
        }
        if (string.IsNullOrEmpty(options.Value.AppSecret) || options.Value.AppSecret == "appSecret")
        {
            suggestions.Add("AppSecret 未正确配置，请检查 appsettings.json");
        }
        if (options.Value.AppKey == "dingakpqehbge8672usj")
        {
            suggestions.Add("当前使用的 AppKey 可能是普通企业应用的，请确认是否使用了扫码登录应用的 AppKey");
        }
        if (suggestions.Count == 0)
        {
            suggestions.Add("配置看起来正常，如果仍然遇到 40001 错误，请确认：");
            suggestions.Add("1. 使用的是扫码登录应用的 AppKey 和 AppSecret（不是普通企业应用）");
            suggestions.Add("2. 在钉钉开放平台已配置回调地址：http://localhost:54507/api/auth/callback");
            suggestions.Add("3. 扫码登录应用已启用");
        }
        diagnoseResults["配置建议"] = suggestions;
        
        return Results.Ok(ApiResponse<Dictionary<string, object>>.SuccessResult(diagnoseResults, "诊断完成"));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "诊断API异常");
        return Results.BadRequest(ApiResponse<Dictionary<string, object>>.FailResult($"诊断失败: {ex.Message}"));
    }
})
.WithName("Diagnose")
.WithTags("认证");

// 登录回调 - 处理钉钉回调后重定向到前端
app.MapGet("/api/auth/callback", async (
    string? code,
    string? state,
    HttpContext context,
    IDingTalkService dingTalkService,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrEmpty(code))
    {
        var errorMsg = Uri.EscapeDataString("授权码不能为空");
        return Results.Redirect($"/?error={errorMsg}");
    }

    try
    {
        logger.LogInformation("收到登录回调，code: {Code}, state: {State}", code, state);
        
        // 确保 Session 已加载
        await context.Session.LoadAsync();
        
        var userInfo = await dingTalkService.GetUserInfoByCodeAsync(code);
        
        if (userInfo == null)
        {
            logger.LogWarning("GetUserInfoByCodeAsync 返回 null，可能 API 调用失败");
            var errorMsg = Uri.EscapeDataString("登录失败：无法获取用户信息，请查看服务器日志");
            return Results.Redirect($"/?error={errorMsg}");
        }
        
        logger.LogInformation("获取到用户信息: {UserInfo}", System.Text.Json.JsonSerializer.Serialize(userInfo));
        
        if (!userInfo.ContainsKey("openid"))
        {
            logger.LogWarning("用户信息中缺少 openid 字段: {UserInfo}", System.Text.Json.JsonSerializer.Serialize(userInfo));
            var errorMsg = Uri.EscapeDataString($"登录失败：用户信息不完整。获取到的信息: {System.Text.Json.JsonSerializer.Serialize(userInfo)}");
            return Results.Redirect($"/?error={errorMsg}");
        }
        
        var openid = userInfo["openid"]?.ToString();
        var nick = userInfo.ContainsKey("nick") ? userInfo["nick"]?.ToString() : "";
        
        if (string.IsNullOrEmpty(openid))
        {
            logger.LogWarning("openid 为空");
            var errorMsg = Uri.EscapeDataString("登录失败：用户ID为空");
            return Results.Redirect($"/?error={errorMsg}");
        }
        
        // 保存登录信息到Session
        context.Session.SetString("UserId", openid);
        context.Session.SetString("UserName", nick ?? "");
        
        // 提交 Session 更改
        await context.Session.CommitAsync();

        logger.LogInformation("登录成功 - UserId: {UserId}, UserName: {UserName}", openid, nick);

        // 重定向到用户管理页面
        return Results.Redirect("/users");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "登录回调处理失败: {Message}, 堆栈: {StackTrace}", ex.Message, ex.StackTrace);
        
        // 开发环境：提示查看日志
        var errorDetails = app.Environment.IsDevelopment() 
            ? $"登录失败：{ex.Message}\n\n请查看运行应用的命令行窗口或 Visual Studio 的输出窗口中的日志信息。"
            : "登录失败，请查看服务器日志";
        
        var errorMsg = Uri.EscapeDataString(errorDetails);
        return Results.Redirect($"/?error={errorMsg}");
    }
})
.WithName("AuthCallback")
.WithTags("认证");

// 检查登录状态
app.MapGet("/api/auth/status", (HttpContext context) =>
{
    var userId = context.Session.GetString("UserId");
    var userName = context.Session.GetString("UserName");
    var isLoggedIn = !string.IsNullOrEmpty(userId);

    return Results.Ok(ApiResponse<LoginStatusResponse>.SuccessResult(
        new LoginStatusResponse
        {
            IsLoggedIn = isLoggedIn,
            UserId = userId,
            UserName = userName
        }
    ));
})
.WithName("GetLoginStatus")
.WithTags("认证");

// 登出
app.MapPost("/api/auth/logout", (HttpContext context) =>
{
    context.Session.Clear();
    return Results.Ok(ApiResponse.SuccessResult("登出成功"));
})
.WithName("Logout")
.WithTags("认证");

// 开发模式：跳过登录
app.MapPost("/api/auth/dev-login", (HttpContext context, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("开发模式登录请求开始");
        
        // 确保 Session 已加载
        if (!context.Session.IsAvailable)
        {
            logger.LogWarning("Session 不可用，尝试加载");
            context.Session.LoadAsync().Wait();
        }
        
        // 设置开发模式的登录信息
        context.Session.SetString("UserId", "dev-user-001");
        context.Session.SetString("UserName", "开发测试用户");
        
        // 提交 Session 更改
        context.Session.CommitAsync().Wait();
        
        logger.LogInformation("开发模式登录成功 - UserId: dev-user-001, SessionId: {SessionId}", 
            context.Session.Id);
        
        var response = ApiResponse<LoginStatusResponse>.SuccessResult(
            new LoginStatusResponse
            {
                IsLoggedIn = true,
                UserId = "dev-user-001",
                UserName = "开发测试用户"
            },
            "开发模式登录成功"
        );
        
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "开发模式登录失败: {Error}", ex.Message);
        return Results.BadRequest(ApiResponse<LoginStatusResponse>.FailResult(
            $"开发模式登录失败: {ex.Message}\n堆栈跟踪: {ex.StackTrace}"));
    }
})
.WithName("DevLogin")
.WithTags("认证");

// ==================== 用户管理 API ====================

// 获取所有用户
app.MapGet("/api/users", async (IUserService userService, HttpContext context) =>
{
    if (string.IsNullOrEmpty(context.Session.GetString("UserId")))
    {
        return Results.Unauthorized();
    }

    var users = await userService.GetAllUsersAsync();
    var userResponses = users.Select(u => new UserResponse
    {
        Id = u.Id,
        UserId = u.UserId,
        Name = u.Name,
        Mobile = u.Mobile,
        Email = u.Email,
        Department = u.Department,
        Position = u.Position,
        CreatedAt = u.CreatedAt,
        UpdatedAt = u.UpdatedAt
    }).ToList();

    return Results.Ok(ApiResponse<List<UserResponse>>.SuccessResult(userResponses));
})
.WithName("GetAllUsers")
.WithTags("用户管理");

// 获取单个用户
app.MapGet("/api/users/{id:int}", async (int id, IUserService userService, HttpContext context) =>
{
    if (string.IsNullOrEmpty(context.Session.GetString("UserId")))
    {
        return Results.Unauthorized();
    }

    var user = await userService.GetUserByIdAsync(id);
    if (user == null)
    {
        return Results.NotFound(ApiResponse<UserResponse>.FailResult("用户不存在"));
    }

    var userResponse = new UserResponse
    {
        Id = user.Id,
        UserId = user.UserId,
        Name = user.Name,
        Mobile = user.Mobile,
        Email = user.Email,
        Department = user.Department,
        Position = user.Position,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };

    return Results.Ok(ApiResponse<UserResponse>.SuccessResult(userResponse));
})
.WithName("GetUserById")
.WithTags("用户管理");

// 创建用户
app.MapPost("/api/users", async (
    [FromBody] CreateUserRequest request,
    IUserService userService,
    HttpContext context,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrEmpty(context.Session.GetString("UserId")))
    {
        return Results.Unauthorized();
    }

    try
    {
        var user = new User
        {
            UserId = request.UserId,
            Name = request.Name,
            Mobile = request.Mobile,
            Email = request.Email,
            Department = request.Department,
            Position = request.Position
        };

        var createdUser = await userService.CreateUserAsync(user);

        var userResponse = new UserResponse
        {
            Id = createdUser.Id,
            UserId = createdUser.UserId,
            Name = createdUser.Name,
            Mobile = createdUser.Mobile,
            Email = createdUser.Email,
            Department = createdUser.Department,
            Position = createdUser.Position,
            CreatedAt = createdUser.CreatedAt,
            UpdatedAt = createdUser.UpdatedAt
        };

        return Results.Created($"/api/users/{createdUser.Id}",
            ApiResponse<UserResponse>.SuccessResult(userResponse, "创建用户成功"));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "创建用户失败");
        return Results.BadRequest(ApiResponse<UserResponse>.FailResult($"创建用户失败: {ex.Message}"));
    }
})
.WithName("CreateUser")
.WithTags("用户管理");

// 更新用户
app.MapPut("/api/users/{id:int}", async (
    int id,
    [FromBody] UpdateUserRequest request,
    IUserService userService,
    HttpContext context,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrEmpty(context.Session.GetString("UserId")))
    {
        return Results.Unauthorized();
    }

    try
    {
        var user = new User
        {
            Id = id,
            UserId = request.UserId,
            Name = request.Name,
            Mobile = request.Mobile,
            Email = request.Email,
            Department = request.Department,
            Position = request.Position
        };

        var updatedUser = await userService.UpdateUserAsync(id, user);
        if (updatedUser == null)
        {
            return Results.NotFound(ApiResponse<UserResponse>.FailResult("用户不存在"));
        }

        var userResponse = new UserResponse
        {
            Id = updatedUser.Id,
            UserId = updatedUser.UserId,
            Name = updatedUser.Name,
            Mobile = updatedUser.Mobile,
            Email = updatedUser.Email,
            Department = updatedUser.Department,
            Position = updatedUser.Position,
            CreatedAt = updatedUser.CreatedAt,
            UpdatedAt = updatedUser.UpdatedAt
        };

        return Results.Ok(ApiResponse<UserResponse>.SuccessResult(userResponse, "更新用户成功"));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "更新用户失败");
        return Results.BadRequest(ApiResponse<UserResponse>.FailResult($"更新用户失败: {ex.Message}"));
    }
})
.WithName("UpdateUser")
.WithTags("用户管理");

// 删除用户
app.MapDelete("/api/users/{id:int}", async (
    int id,
    IUserService userService,
    HttpContext context,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrEmpty(context.Session.GetString("UserId")))
    {
        return Results.Unauthorized();
    }

    try
    {
        var result = await userService.DeleteUserAsync(id);
        if (!result)
        {
            return Results.NotFound(ApiResponse.FailResult("用户不存在"));
        }

        return Results.Ok(ApiResponse.SuccessResult("删除用户成功"));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "删除用户失败");
        return Results.BadRequest(ApiResponse.FailResult($"删除用户失败: {ex.Message}"));
    }
})
.WithName("DeleteUser")
.WithTags("用户管理");

// ==================== 消息管理 API ====================

// 获取消息日志
app.MapGet("/api/messages", async (IMessageService messageService, HttpContext context) =>
{
    if (string.IsNullOrEmpty(context.Session.GetString("UserId")))
    {
        return Results.Unauthorized();
    }

    var logs = await messageService.GetMessageLogsAsync();
    return Results.Ok(ApiResponse<List<MessageLog>>.SuccessResult(logs));
})
.WithName("GetMessageLogs")
.WithTags("消息管理");

// 发送全体消息
app.MapPost("/api/messages/send-all", async (
    [FromBody] SendMessageRequest request,
    IMessageService messageService,
    HttpContext context,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrEmpty(context.Session.GetString("UserId")))
    {
        return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(request.Content))
    {
        return Results.BadRequest(ApiResponse.FailResult("消息内容不能为空"));
    }

    try
    {
        var result = await messageService.SendMessageToAllAsync(request.Content);
        if (result)
        {
            return Results.Ok(ApiResponse.SuccessResult("消息发送成功"));
        }
        else
        {
            return Results.BadRequest(ApiResponse.FailResult("消息发送失败"));
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "发送全体消息失败");
        return Results.BadRequest(ApiResponse.FailResult($"发送消息时发生错误: {ex.Message}"));
    }
})
.WithName("SendMessageToAll")
.WithTags("消息管理");

// 发送用户消息
app.MapPost("/api/messages/send-user", async (
    [FromBody] SendMessageToUserRequest request,
    IMessageService messageService,
    HttpContext context,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrEmpty(context.Session.GetString("UserId")))
    {
        return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(request.UserId))
    {
        return Results.BadRequest(ApiResponse.FailResult("用户ID不能为空"));
    }

    if (string.IsNullOrWhiteSpace(request.Content))
    {
        return Results.BadRequest(ApiResponse.FailResult("消息内容不能为空"));
    }

    try
    {
        var result = await messageService.SendMessageToUserAsync(request.UserId, request.Content);
        if (result)
        {
            return Results.Ok(ApiResponse.SuccessResult("消息发送成功"));
        }
        else
        {
            return Results.BadRequest(ApiResponse.FailResult("消息发送失败"));
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "发送用户消息失败");
        return Results.BadRequest(ApiResponse.FailResult($"发送消息时发生错误: {ex.Message}"));
    }
})
.WithName("SendMessageToUser")
.WithTags("消息管理");

// 配置 Blazor（必须在所有 API 端点之后）
app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

