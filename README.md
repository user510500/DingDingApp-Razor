# 钉钉管理系统

基于 ASP.NET 8、Blazor Server、Minimal API、Entity Framework Core、Docker 技术栈开发的钉钉集成管理系统。

## 功能特性

1. **钉钉扫码登录** - 使用钉钉开放平台API实现扫码登录功能
2. **开发模式登录** - 支持开发环境下跳过登录，直接进入后台进行功能测试
3. **人员管理** - 实现人员的增删改查（CRUD）功能，使用 Radzen Blazor 组件提供现代化 UI
4. **消息发送** - 支持发送全体消息和特定人员消息
5. **RESTful API** - 使用 Minimal API 架构，提供标准的 RESTful 接口
6. **Swagger 文档** - 自动生成 API 文档，方便测试和集成
7. **现代化 UI** - 使用 Radzen Blazor 组件库，提供美观易用的用户界面

## 技术栈

- ASP.NET 8 Blazor Server
- Minimal API
- Entity Framework Core (Code First)
- Radzen Blazor UI 组件库
- Docker & Docker Compose
- MySQL 8.0
- Swagger/OpenAPI
- Session 管理（登录状态）

## 项目结构

```
DingDingApp/
├── Models/              # 数据模型
│   ├── User.cs
│   └── MessageLog.cs
├── DTOs/                # 数据传输对象
│   ├── UserDto.cs
│   ├── SendMessageRequest.cs
│   └── ApiResponse.cs
├── Data/                # 数据访问层
│   └── ApplicationDbContext.cs
├── Services/            # 业务服务层
│   ├── IDingTalkService.cs
│   ├── DingTalkService.cs
│   ├── IUserService.cs
│   ├── UserService.cs
│   ├── IMessageService.cs
│   ├── MessageService.cs
│   └── ApiService.cs    # 前端 API 调用服务
├── Pages/               # Blazor 页面
│   ├── Index.razor      # 登录页面
│   ├── Users.razor      # 用户管理页面
│   ├── Messages.razor   # 消息管理页面
│   ├── CreateUserDialog.razor
│   ├── EditUserDialog.razor
│   └── UserDetailsDialog.razor
├── Shared/              # 共享组件
│   ├── MainLayoutSimple.razor
│   └── MainLayout.razor
├── Options/             # 配置选项
│   └── DingTalkOptions.cs
├── Program.cs           # Minimal API 端点定义和应用配置
├── App.razor            # Blazor 应用根组件
├── _Imports.razor       # 全局 using 指令
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
    "DefaultConnection": "Server=localhost;Port=3306;Database=DingDingDb;User=root;Password=YourPassword123!;"
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

应用将在 `http://localhost:5000` 运行，Swagger 文档页面为 `/swagger`

### 方式二：本地运行

1. 确保已安装 .NET 8 SDK
2. 确保已安装 MySQL 8.0 或使用 Docker 运行 MySQL
3. 修改 `appsettings.json` 中的配置
4. 运行以下命令：

```bash
dotnet restore
dotnet build
dotnet run
```

应用将在 `http://localhost:5000` 运行

## API 端点说明

项目使用 Minimal API 架构，提供以下 RESTful API 端点：

### 认证相关
- `GET /api/auth/qrcode` - 获取钉钉扫码登录二维码
- `GET /api/auth/callback` - 钉钉登录回调
- `GET /api/auth/status` - 检查当前登录状态
- `POST /api/auth/logout` - 退出登录
- `POST /api/auth/dev-login` - 开发模式登录（仅开发环境可用）

### 用户管理
- `GET /api/users` - 获取所有用户
- `GET /api/users/{id}` - 获取单个用户信息
- `POST /api/users` - 创建新用户
- `PUT /api/users/{id}` - 更新用户信息
- `DELETE /api/users/{id}` - 删除用户

### 消息管理
- `GET /api/messages` - 获取消息日志
- `POST /api/messages/send-all` - 发送全体消息
- `POST /api/messages/send-user` - 发送给特定用户

**详细的 API 文档请访问运行后的 Swagger UI 页面：`http://localhost:5000/swagger`**

## 开发模式登录

在开发环境下，可以使用开发模式登录功能跳过钉钉扫码登录，直接进入后台进行功能测试：

1. 访问应用首页 `http://localhost:5000/`
2. 点击页面上的"跳过登录，直接进入后台"按钮
3. 系统会自动设置开发测试用户并跳转到用户管理页面

**注意：** 开发模式登录功能仅在开发环境（`ASPNETCORE_ENVIRONMENT=Development`）下可用，生产环境会自动禁用。

## 钉钉API说明

本项目直接使用钉钉开放平台的 HTTP API，不使用官方 SDK。主要使用的API包括：

1. **获取Access Token**: `GET /gettoken`
2. **扫码登录**: `GET /connect/qrconnect` 和 `GET /connect/sns/gettoken_bycode`
3. **获取用户信息**: `GET /connect/sns/getuserinfo`
4. **发送工作通知**: `POST /topapi/message/corpconversation/asyncsend_v2`

## 注意事项

1. 钉钉应用的回调地址需要配置为：`http://your-domain/api/auth/callback`
2. 确保钉钉应用已开通相应的权限（扫码登录、发送工作通知等）
3. 数据库首次运行会自动创建表结构（Code First）
4. 生产环境请修改默认密码和连接字符串
5. Swagger UI 在开发环境下自动启用，访问 `/swagger` 即可查看
6. 开发模式登录功能仅用于开发测试，生产环境会自动禁用
7. 在 Blazor Server 模式下，Session Cookie 会自动在服务器端请求中传递

## 开发说明

### 架构特点

- **Blazor Server** - 使用 Blazor Server 模式，提供实时交互的用户界面
- **Minimal API** - 使用 Minimal API 架构，代码简洁高效
- **Entity Framework Core** - 使用 Code First 方式管理数据库
- **Session 管理** - 使用 Session 管理用户登录状态
- **Radzen Blazor** - 使用 Radzen Blazor 组件库提供现代化 UI 组件
- **DTOs** - 使用数据传输对象规范 API 请求和响应
- **统一响应格式** - 使用 `ApiResponse` 统一 API 响应格式

### 服务说明

- **DingTalkService** - 处理所有钉钉 API 调用
- **UserService** - 处理用户相关的业务逻辑
- **MessageService** - 处理消息发送和日志记录
- **ApiService** - 前端 Blazor 组件调用后端 API 的服务类

### 关键技术点

- 消息发送记录保存在 `MessageLog` 表中
- 在 Blazor Server 中，`ApiService` 会自动从 `HttpContext` 获取 Session Cookie 并传递给 API 请求
- 开发模式登录使用 JavaScript `fetch` API 设置 Session，确保 Cookie 正确传递
- 使用 Radzen Dialog 和 Notification 组件提供用户交互反馈

## 使用指南

### Web 界面使用

1. **访问应用**：启动应用后访问 `http://localhost:5000`
2. **登录**：
   - 生产环境：使用钉钉 APP 扫描二维码登录
   - 开发环境：点击"跳过登录"按钮直接进入后台
3. **功能使用**：
   - 人员管理：查看、添加、编辑、删除用户信息
   - 消息管理：发送全体消息或指定用户消息，查看消息发送记录

### API 测试示例

#### 使用 cURL 测试

```bash
# 获取登录二维码
curl http://localhost:5000/api/auth/qrcode

# 开发模式登录（仅开发环境）
curl -X POST http://localhost:5000/api/auth/dev-login

# 检查登录状态
curl http://localhost:5000/api/auth/status

# 获取所有用户（需要先登录，需要传递 Session Cookie）
curl http://localhost:5000/api/users \
  --cookie "your-session-cookie"

# 创建用户（需要先登录）
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  --cookie "your-session-cookie" \
  -d '{"userId":"user001","name":"张三","mobile":"13800138000"}'

# 发送全体消息（需要先登录）
curl -X POST http://localhost:5000/api/messages/send-all \
  -H "Content-Type: application/json" \
  --cookie "your-session-cookie" \
  -d '{"content":"这是一条测试消息"}'
```

#### 使用 Swagger UI 测试

1. 启动应用后访问 `http://localhost:5000/swagger`
2. 在 Swagger UI 页面可以直接测试所有 API 端点
3. 支持在线查看请求/响应格式和参数说明
4. **注意**：需要先通过 Web 界面登录获取 Session Cookie，然后在 Swagger UI 中设置 Cookie 才能测试需要认证的 API

## 许可证

MIT License

