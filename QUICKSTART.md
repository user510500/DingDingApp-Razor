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
- 开发环境：`http://localhost:8080/Home/Callback`
- 生产环境：`https://your-domain.com/Home/Callback`

### 3. 修改配置文件

编辑 `appsettings.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=DingDingDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;"
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

访问：http://localhost:8080

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

### 1. 登录

1. 访问首页，会显示钉钉扫码登录二维码
2. 使用钉钉APP扫描二维码
3. 确认登录后，自动跳转到人员管理页面

### 2. 人员管理

- **查看列表**：在人员管理页面查看所有人员
- **新增人员**：点击"新增人员"按钮，填写信息后保存
- **编辑人员**：点击"编辑"按钮修改人员信息
- **删除人员**：点击"删除"按钮删除人员（需确认）
- **查看详情**：点击"详情"按钮查看完整信息

### 3. 消息发送

- **发送全体消息**：
  1. 进入消息管理页面
  2. 点击"发送全体消息"
  3. 输入消息内容
  4. 点击"发送"

- **发送特定人员消息**：
  1. 进入消息管理页面
  2. 点击"发送特定人员消息"
  3. 选择目标用户
  4. 输入消息内容
  5. 点击"发送"

### 4. 查看消息记录

在消息管理页面可以查看所有消息发送记录，包括：
- 消息类型（全体/特定人员）
- 目标用户
- 消息内容
- 发送时间
- 发送状态
- 错误信息（如果有）

## 常见问题

### Q: 扫码登录失败？

A: 检查以下几点：
1. 钉钉应用是否已配置正确的回调地址
2. AppKey和AppSecret是否正确
3. 应用是否已开通"扫码登录"权限

### Q: 消息发送失败？

A: 检查以下几点：
1. AgentId是否正确
2. 应用是否已开通"发送工作通知"权限
3. 目标用户ID是否正确（发送特定人员消息时）
4. 查看消息记录中的错误信息

### Q: 数据库连接失败？

A: 检查以下几点：
1. SQL Server是否正在运行
2. 连接字符串中的服务器地址、端口、用户名、密码是否正确
3. 数据库是否存在（不存在会自动创建）

## 注意事项

1. **生产环境部署**：
   - 修改默认密码
   - 使用HTTPS
   - 配置正确的回调地址
   - 使用环境变量存储敏感信息

2. **数据库备份**：
   - 定期备份数据库
   - 生产环境建议使用云数据库服务

3. **安全性**：
   - 不要在代码中硬编码敏感信息
   - 使用环境变量或密钥管理服务
   - 启用HTTPS

## 技术支持

如有问题，请查看：
- 钉钉开放平台文档：https://open.dingtalk.com/document/
- ASP.NET Core 文档：https://docs.microsoft.com/aspnet/core/

