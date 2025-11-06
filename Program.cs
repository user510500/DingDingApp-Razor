using Microsoft.EntityFrameworkCore;
using DingDingApp.Data;
using DingDingApp.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加服务到容器
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// 配置数据库
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 添加HttpClientFactory
builder.Services.AddHttpClient();

// 注册服务
builder.Services.AddScoped<IDingTalkService, DingTalkService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMessageService, MessageService>();

// Radzen组件主要用于Blazor，在MVC中我们使用标准HTML组件

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

var app = builder.Build();

// 配置HTTP请求管道
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// 确保数据库已创建
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();

