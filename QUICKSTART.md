# 快速启动指南

## 前置要求

1. .NET 8 SDK
2. Docker 和 Docker Compose（如果使用Docker方式运行）
3. 钉钉开放平台应用（需要AppKey、AppSecret、AgentId、CorpId）

## 配置步骤

### 1. 配置钉钉应用

1. 登录钉钉开放平台：https://open.dingtalk.com
2. 创建企业内部应用或第三方企业应用
3. 获取以下信息：
   - AppKey
   - AppSecret
   - AgentId（工作通知需要）
   - CorpId

### 2. 配置回调地址

在钉钉开放平台中配置OAuth回调地址：
- 开发环境：`http://localhost:5678/api/auth/callback`
- 生产环境：`https://your-domain.com/api/auth/callback`

### 3. 修改配置文件

编辑 `appsettings.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=DingDingDb;User=root;Password=YourPassword123!;"
  },
  "DingTalk": {
    "AppKey": "你的AppKey",
    "AppSecret": "你的AppSecret",
    "AgentId": "你的AgentId",
    "CorpId": "你的CorpId"
  }
}
```

## 运行项目

### 方式一：Docker Compose（推荐）

```bash
# 启动所有服务（包括数据库）
docker-compose up -d

# 查看日志
docker-compose logs -f

# 停止服务
docker-compose down
```

访问：http://localhost:5678 （Swagger UI 文档页面）

### 方式二：本地开发

```bash
# 恢复依赖
dotnet restore

# 构建项目
dotnet build

# 运行项目
dotnet run
```

访问：http://localhost:5000 或 https://localhost:5001

## 使用说明

本项目使用 Minimal API 架构，所有操作都通过 RESTful API 完成。

### 1. 访问 Swagger 文档

启动应用后，访问 `http://localhost:5678` 或 `http://localhost:5000`（本地运行），会自动打开 Swagger UI 文档页面，可以：
- 查看所有 API 端点
- 在线测试 API
- 查看请求/响应格式

### 2. 登录流程

**方式一：使用 API 直接调用**
```bash
# 1. 获取二维码
curl http://localhost:5678/api/auth/qrcode

# 2. 扫码后会触发回调（钉钉自动调用）
# GET /api/auth/callback?code=xxx

# 3. 检查登录状态
curl http://localhost:5678/api/auth/status
```

**方式二：使用 Swagger UI**
1. 打开 Swagger UI
2. 找到 `GET /api/auth/qrcode` 端点
3. 点击 "Try it out" 和 "Execute"
4. 复制返回的二维码URL，使用钉钉扫描

### 3. 人员管理

**获取所有用户**
```bash
curl http://localhost:5678/api/users
```

**创建用户**
```bash
curl -X POST http://localhost:5678/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user001",
    "name": "张三",
    "mobile": "13800138000",
    "email": "zhangsan@example.com",
    "department": "技术部",
    "position": "工程师"
  }'
```

**更新用户**
```bash
curl -X PUT http://localhost:5678/api/users/1 \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user001",
    "name": "张三（已修改）",
    "mobile": "13800138000"
  }'
```

**删除用户**
```bash
curl -X DELETE http://localhost:5678/api/users/1
```

### 4. 消息发送

**发送全体消息**
```bash
curl -X POST http://localhost:5678/api/messages/send-all \
  -H "Content-Type: application/json" \
  -d '{"content": "这是一条全体消息"}'
```

**发送给特定用户**
```bash
curl -X POST http://localhost:5678/api/messages/send-user \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user001",
    "content": "这是给你的消息"
  }'
```

**查看消息记录**
```bash
curl http://localhost:5678/api/messages
```

### 5. API 响应格式

所有 API 都返回统一的格式：

**成功响应**
```json
{
  "success": true,
  "message": "操作成功",
  "data": {
    // 具体数据
  }
}
```

**失败响应**
```json
{
  "success": false,
  "message": "错误信息",
  "data": null
}
```

## 常见问题

### Q: 无法访问 Swagger UI？

A: 检查以下几点：
1. 确保应用运行在开发环境（ASPNETCORE_ENVIRONMENT=Development）
2. 检查端口是否被占用（默认 5678 或 5000）
3. 确认 Swagger 相关 NuGet 包已正确安装

### Q: 扫码登录失败？

A: 检查以下几点：
1. 钉钉应用是否已配置正确的回调地址（`/api/auth/callback`）
2. AppKey和AppSecret是否正确
3. 应用是否已开通"扫码登录"权限
4. 回调地址需要是公网可访问的地址（开发时可使用 ngrok 等内网穿透工具）

### Q: 消息发送失败？

A: 检查以下几点：
1. AgentId是否正确
2. 应用是否已开通"发送工作通知"权限
3. 目标用户ID是否正确（发送特定人员消息时）
4. 查看消息记录中的错误信息（调用 `GET /api/messages`）
5. 检查 Access Token 是否正确获取

### Q: API 返回 401 Unauthorized？

A: 大部分 API 需要先登录才能访问。请先调用登录接口获取 Session。

### Q: 数据库连接失败？

A: 检查以下几点：
1. MySQL是否正在运行
2. 连接字符串中的服务器地址、端口、用户名、密码是否正确
3. 数据库是否存在（不存在会自动创建）

## 注意事项

1. **生产环境部署**：
   - 修改默认密码
   - 使用HTTPS
   - 配置正确的回调地址
   - 使用环境变量存储敏感信息
   - 根据需要调整 CORS 策略（默认允许所有来源）
   - 生产环境建议关闭 Swagger UI 或加上访问限制

2. **数据库备份**：
   - 定期备份数据库
   - 生产环境建议使用云数据库服务

3. **安全性**：
   - 不要在代码中硬编码敏感信息
   - 使用环境变量或密钥管理服务
   - 启用HTTPS
   - 考虑添加 API 限流和身份验证机制
   - Session 默认30分钟超时，可根据需要调整

4. **API 开发建议**：
   - 所有 API 都遵循 RESTful 规范
   - 使用统一的 `ApiResponse` 返回格式
   - 错误处理通过 try-catch 并返回友好的错误信息
   - 使用 DTOs 而不是直接暴露 Entity 模型

## 技术支持

如有问题，请查看：
- 钉钉开放平台文档：https://open.dingtalk.com/document/
- ASP.NET Core Minimal API 文档：https://docs.microsoft.com/aspnet/core/fundamentals/minimal-apis
- Swagger 文档：https://swagger.io/

