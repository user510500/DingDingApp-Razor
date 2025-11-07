# 钉钉管理系统

基于 ASP.NET 8、Minimal API、Entity Framework Core、Docker 技术栈开发的钉钉集成管理系统。

## 功能特性

1. **钉钉扫码登录** - 使用钉钉开放平台API实现扫码登录功能
2. **人员管理** - 实现人员的增删改查（CRUD）功能
3. **消息发送** - 支持发送全体消息和特定人员消息
4. **RESTful API** - 使用 Minimal API 架构，提供标准的 RESTful 接口
5. **Swagger 文档** - 自动生成 API 文档，方便测试和集成

## 技术栈

- ASP.NET 8 Minimal API
- Entity Framework Core (Code First)
- Docker & Docker Compose
- MySQL 8.0
- Swagger/OpenAPI

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
│   └── MessageService.cs
├── Options/             # 配置选项
│   └── DingTalkOptions.cs
├── Program.cs           # Minimal API 端点定义
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

应用将在 `http://localhost:5678` 运行，Swagger 文档页面为根路径 `/`

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

## API 端点说明

项目使用 Minimal API 架构，提供以下 RESTful API 端点：

### 认证相关
- `GET /api/auth/qrcode` - 获取钉钉扫码登录二维码
- `GET /api/auth/callback` - 钉钉登录回调
- `GET /api/auth/status` - 检查当前登录状态
- `POST /api/auth/logout` - 退出登录

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

**详细的 API 文档请访问运行后的 Swagger UI 页面**

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
5. Swagger UI 在开发环境下自动启用，访问根路径 `/` 即可查看

## 开发说明

- 使用 ASP.NET 8 Minimal API 架构，代码简洁高效
- 使用 Entity Framework Core Code First 方式管理数据库
- 使用 Session 管理用户登录状态
- 所有钉钉API调用都通过 `DingTalkService` 服务类实现
- 消息发送记录保存在 `MessageLog` 表中
- 使用 DTOs（数据传输对象）规范 API 请求和响应
- 统一的 `ApiResponse` 格式，方便前端处理

## API 测试示例

### 使用 cURL 测试

```bash
# 获取登录二维码
curl http://localhost:5678/api/auth/qrcode

# 检查登录状态
curl http://localhost:5678/api/auth/status

# 获取所有用户（需要先登录）
curl http://localhost:5678/api/users

# 创建用户
curl -X POST http://localhost:5678/api/users \
  -H "Content-Type: application/json" \
  -d '{"userId":"user001","name":"张三","mobile":"13800138000"}'

# 发送全体消息
curl -X POST http://localhost:5678/api/messages/send-all \
  -H "Content-Type: application/json" \
  -d '{"content":"这是一条测试消息"}'
```

### 使用 Swagger UI 测试

1. 启动应用后访问 `http://localhost:5678`
2. 在 Swagger UI 页面可以直接测试所有 API 端点
3. 支持在线查看请求/响应格式和参数说明

## 许可证

MIT License

