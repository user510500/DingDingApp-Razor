# 钉钉管理系统

基于 ASP.NET 8、Entity Framework Core、Docker、MVC、Razor 技术栈开发的钉钉集成管理系统。

## 功能特性

1. **钉钉扫码登录** - 使用钉钉开放平台API实现扫码登录功能
2. **人员管理** - 实现人员的增删改查（CRUD）功能
3. **消息发送** - 支持发送全体消息和特定人员消息

## 技术栈

- ASP.NET 8
- Entity Framework Core (Code First)
- Docker & Docker Compose
- MVC
- Razor Pages
- SQL Server

## 项目结构

```
DingDingApp/
├── Controllers/          # 控制器
│   ├── HomeController.cs
│   ├── UserController.cs
│   └── MessageController.cs
├── Models/              # 数据模型
│   ├── User.cs
│   └── MessageLog.cs
├── Data/                # 数据访问层
│   └── ApplicationDbContext.cs
├── Services/            # 业务服务层
│   ├── IDingTalkService.cs
│   ├── DingTalkService.cs
│   ├── IUserService.cs
│   ├── UserService.cs
│   ├── IMessageService.cs
│   └── MessageService.cs
├── Options/             # 配置选项
│   └── DingTalkOptions.cs
├── Views/               # 视图文件
├── wwwroot/             # 静态资源
├── Dockerfile           # Docker镜像构建文件
├── docker-compose.yml   # Docker Compose配置
└── appsettings.json     # 应用配置

```

## 配置说明

### 1. 钉钉应用配置

在 `appsettings.json` 中配置钉钉应用信息：

```json
{
  "DingTalk": {
    "AppKey": "your_app_key",
    "AppSecret": "your_app_secret",
    "AgentId": "your_agent_id",
    "CorpId": "your_corp_id"
  }
}
```

### 2. 数据库配置

在 `appsettings.json` 中配置数据库连接字符串：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=DingDingDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;"
  }
}
```

## 运行方式

### 方式一：使用 Docker Compose（推荐）

1. 确保已安装 Docker 和 Docker Compose
2. 修改 `appsettings.json` 中的钉钉配置
3. 运行以下命令：

```bash
docker-compose up -d
```

应用将在 `http://localhost:8080` 运行

### 方式二：本地运行

1. 确保已安装 .NET 8 SDK
2. 确保已安装 SQL Server 或使用 Docker 运行 SQL Server
3. 修改 `appsettings.json` 中的配置
4. 运行以下命令：

```bash
dotnet restore
dotnet build
dotnet run
```

## 钉钉API说明

本项目直接使用钉钉开放平台的 HTTP API，不使用官方 SDK。主要使用的API包括：

1. **获取Access Token**: `GET /gettoken`
2. **扫码登录**: `GET /connect/qrconnect` 和 `GET /connect/sns/gettoken_bycode`
3. **获取用户信息**: `GET /connect/sns/getuserinfo`
4. **发送工作通知**: `POST /topapi/message/corpconversation/asyncsend_v2`

## 注意事项

1. 钉钉应用的回调地址需要配置为：`http://your-domain/Home/Callback`
2. 确保钉钉应用已开通相应的权限（扫码登录、发送工作通知等）
3. 数据库首次运行会自动创建表结构（Code First）
4. 生产环境请修改默认密码和连接字符串

## 开发说明

- 使用 Entity Framework Core Code First 方式管理数据库
- 使用 Session 管理用户登录状态
- 所有钉钉API调用都通过 `DingTalkService` 服务类实现
- 消息发送记录保存在 `MessageLog` 表中

## 许可证

MIT License

