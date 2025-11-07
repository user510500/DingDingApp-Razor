using Microsoft.EntityFrameworkCore;
using DingDingApp.Data;
using DingDingApp.Services;
using DingDingApp.Models;
using DingDingApp.DTOs;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// 配置数据库
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 添加HttpClientFactory
builder.Services.AddHttpClient();

// 注册服务
builder.Services.AddScoped<IDingTalkService, DingTalkService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMessageService, MessageService>();

// 配置Session（用于登录状态管理）
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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

var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "钉钉管理系统 API v1");
        c.RoutePrefix = string.Empty; // 将 Swagger UI 设置为根路径
    });
}

app.UseHttpsRedirection();
app.UseSession();
app.UseCors("AllowAll");

// 确保数据库已创建
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

// ==================== 认证相关 API ====================

// 获取二维码登录URL
app.MapGet("/api/auth/qrcode", async (IDingTalkService dingTalkService) =>
{
    try
    {
        var qrCodeUrl = await dingTalkService.GetQrCodeUrlAsync();
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

// 登录回调
app.MapGet("/api/auth/callback", async (
    string? code,
    string? state,
    HttpContext context,
    IDingTalkService dingTalkService,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrEmpty(code))
    {
        return Results.BadRequest(ApiResponse.FailResult("授权码不能为空"));
    }

    try
    {
        var userInfo = await dingTalkService.GetUserInfoByCodeAsync(code);
        if (userInfo != null && userInfo.ContainsKey("openid"))
        {
            // 保存登录信息到Session
            context.Session.SetString("UserId", userInfo["openid"]?.ToString() ?? "");
            context.Session.SetString("UserName", userInfo["nick"]?.ToString() ?? "");

            return Results.Ok(ApiResponse<LoginStatusResponse>.SuccessResult(
                new LoginStatusResponse
                {
                    IsLoggedIn = true,
                    UserId = userInfo["openid"]?.ToString(),
                    UserName = userInfo["nick"]?.ToString()
                },
                "登录成功"
            ));
        }

        return Results.BadRequest(ApiResponse.FailResult("登录失败，未获取到用户信息"));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "登录回调处理失败");
        return Results.BadRequest(ApiResponse.FailResult($"登录失败: {ex.Message}"));
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

app.Run();

