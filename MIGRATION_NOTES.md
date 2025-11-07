# MVC 到 Minimal API 迁移说明

本文档记录了项目从 ASP.NET MVC 架构迁移到 Minimal API 架构的详细信息。

## 迁移日期
2024-11-07

## 迁移概述

项目已从传统的 MVC 架构成功迁移到 ASP.NET 8 Minimal API 架构，同时保留了所有原有功能。

---

## 主要变更

### 1. 架构变更

**之前（MVC）**
- 使用 Controllers + Views + Models 架构
- 通过 Razor 视图渲染 HTML 页面
- 传统的服务器端渲染模式

**之后（Minimal API）**
- 使用 Minimal API 端点定义
- 纯 RESTful API，返回 JSON 数据
- 前后端分离架构，更适合现代 Web 开发

### 2. 文件结构变更

#### 删除的目录/文件
- `Controllers/` - MVC 控制器（功能已迁移到 Program.cs）
- `Views/` - Razor 视图（已不需要）
- `wwwroot/` - 静态资源（已不需要）

#### 新增的目录/文件
- `DTOs/` - 数据传输对象
  - `UserDto.cs` - 用户相关 DTO
  - `SendMessageRequest.cs` - 消息请求 DTO
  - `ApiResponse.cs` - 统一响应格式
- `API_EXAMPLES.md` - API 调用示例文档
- `MIGRATION_NOTES.md` - 迁移说明文档

#### 保留的文件（功能不变）
- `Models/` - 实体模型
- `Data/` - 数据访问层
- `Services/` - 业务服务层
- `Options/` - 配置选项

### 3. 代码变更

#### Program.cs
完全重写，主要变更：
- 移除 `AddControllersWithViews()` 和 `AddRazorPages()`
- 添加 `AddEndpointsApiExplorer()` 和 `AddSwaggerGen()`
- 添加 CORS 配置
- 定义所有 API 端点（认证、用户管理、消息管理）
- 使用依赖注入直接在端点中注入服务

#### DingDingApp.csproj
- 添加 `Swashbuckle.AspNetCore` - Swagger UI 支持
- 添加 `Microsoft.AspNetCore.OpenApi` - OpenAPI 支持
- 移除了对 MVC 相关包的依赖（如果有的话）

---

## API 端点映射

### 认证相关

| MVC 路由 | Minimal API 端点 | HTTP 方法 |
|---------|-----------------|----------|
| `/Home/Index` | `/api/auth/qrcode` | GET |
| `/Home/Callback` | `/api/auth/callback` | GET |
| `/Home/Logout` | `/api/auth/logout` | POST |
| - | `/api/auth/status` | GET |

### 用户管理

| MVC 路由 | Minimal API 端点 | HTTP 方法 |
|---------|-----------------|----------|
| `/User/Index` | `/api/users` | GET |
| `/User/Details/{id}` | `/api/users/{id}` | GET |
| `/User/Create` (GET) | - | - |
| `/User/Create` (POST) | `/api/users` | POST |
| `/User/Edit/{id}` (GET) | `/api/users/{id}` | GET |
| `/User/Edit/{id}` (POST) | `/api/users/{id}` | PUT |
| `/User/Delete/{id}` (GET) | - | - |
| `/User/Delete` (POST) | `/api/users/{id}` | DELETE |

### 消息管理

| MVC 路由 | Minimal API 端点 | HTTP 方法 |
|---------|-----------------|----------|
| `/Message/Index` | `/api/messages` | GET |
| `/Message/SendToAll` (GET) | - | - |
| `/Message/SendToAll` (POST) | `/api/messages/send-all` | POST |
| `/Message/SendToUser` (GET) | - | - |
| `/Message/SendToUser` (POST) | `/api/messages/send-user` | POST |

---

## 功能对比

### 保留的功能
✅ 钉钉扫码登录  
✅ 用户增删改查（CRUD）  
✅ 发送全体消息  
✅ 发送特定用户消息  
✅ 消息记录查询  
✅ Session 管理  
✅ 数据库持久化  

### 新增的功能
✨ Swagger UI 自动生成 API 文档  
✨ 统一的 API 响应格式  
✨ RESTful API 规范  
✨ CORS 支持  
✨ 更好的前后端分离支持  
✨ 登录状态查询接口  

### 移除的功能
❌ 服务器端 HTML 渲染  
❌ Razor 视图  
❌ 表单验证（改为 API 验证）  
❌ TempData 传递消息（改为 API 响应）  

---

## 数据传输对象（DTOs）

为了规范 API 请求和响应，新增了以下 DTOs：

### 请求 DTOs
- `CreateUserRequest` - 创建用户请求
- `UpdateUserRequest` - 更新用户请求
- `SendMessageRequest` - 发送消息请求
- `SendMessageToUserRequest` - 发送用户消息请求

### 响应 DTOs
- `UserResponse` - 用户响应
- `ApiResponse<T>` - 通用响应包装
- `AuthResponse` - 认证响应
- `LoginStatusResponse` - 登录状态响应

---

## Swagger UI

### 访问地址
- 开发环境：`http://localhost:5678/` 或 `http://localhost:5000/`
- 自动显示在根路径

### 功能特性
- 查看所有 API 端点
- 在线测试 API
- 查看请求/响应模型
- 支持 Try it out 功能
- 自动生成 API 文档

### 配置说明
Swagger UI 仅在开发环境启用，生产环境自动关闭。如需在生产环境启用，需修改 `Program.cs` 中的配置。

---

## Session 管理

Session 管理保持不变：
- 使用内存缓存存储 Session
- 超时时间：30 分钟
- Cookie HttpOnly：已启用
- 存储内容：UserId 和 UserName

---

## CORS 配置

默认 CORS 策略（AllowAll）：
```csharp
policy.AllowAnyOrigin()
      .AllowAnyMethod()
      .AllowAnyHeader();
```

⚠️ **生产环境警告**：建议根据实际需求限制允许的来源。

---

## 测试建议

### 1. 使用 Swagger UI
最简单的测试方式，直接在浏览器中测试所有 API。

### 2. 使用 cURL
```bash
# 测试获取用户列表
curl http://localhost:5678/api/users

# 测试创建用户
curl -X POST http://localhost:5678/api/users \
  -H "Content-Type: application/json" \
  -d '{"userId":"test","name":"测试用户"}'
```

### 3. 使用 Postman
导入 Swagger JSON 自动生成 Postman Collection：
1. 访问 `http://localhost:5678/swagger/v1/swagger.json`
2. 复制 JSON
3. 在 Postman 中导入

### 4. 使用前端框架
可以使用 React、Vue、Angular 等前端框架调用 API。

---

## 部署注意事项

### 1. 环境配置
确保设置正确的环境变量：
```bash
ASPNETCORE_ENVIRONMENT=Development  # 开发环境
ASPNETCORE_ENVIRONMENT=Production   # 生产环境
```

### 2. 数据库连接
已从 SQL Server 迁移到 MySQL：
- 连接字符串格式已更新
- 使用 Pomelo.EntityFrameworkCore.MySql 驱动
- Docker Compose 已配置 MySQL 8.0 容器

### 3. 钉钉回调地址
需要更新钉钉应用的回调地址：
- 旧地址：`/Home/Callback`
- 新地址：`/api/auth/callback`

### 4. Swagger UI
生产环境建议：
- 关闭 Swagger UI，或
- 添加访问限制（如 IP 白名单、Basic Auth）

---

## 性能优势

Minimal API 相比传统 MVC 的性能优势：
1. **更少的内存占用** - 不需要加载 MVC 相关组件
2. **更快的启动时间** - 减少了中间件和服务注册
3. **更高的吞吐量** - 直接路由到端点处理器
4. **更小的部署包** - 移除了视图引擎等不需要的组件

---

## 后续计划

### 短期
- [ ] 添加 API 版本管理
- [ ] 添加 API 限流机制
- [ ] 添加 JWT 认证（替换 Session）
- [ ] 添加请求日志记录

### 长期
- [ ] 添加 GraphQL 支持
- [ ] 添加 gRPC 端点
- [ ] 实现缓存策略
- [ ] 添加性能监控

---

## 回滚说明

如果需要回滚到 MVC 版本：
1. 恢复 `Controllers/` 目录
2. 恢复 `Views/` 目录
3. 恢复旧的 `Program.cs`
4. 移除 DTOs 相关代码
5. 更新 NuGet 包引用

**建议**：在 Git 中创建分支保留 MVC 版本代码。

---

## 相关文档

- [README.md](README.md) - 项目说明
- [QUICKSTART.md](QUICKSTART.md) - 快速启动指南
- [API_EXAMPLES.md](API_EXAMPLES.md) - API 调用示例

---

## 技术支持

如有问题或建议，请查看：
- ASP.NET Core Minimal APIs: https://docs.microsoft.com/aspnet/core/fundamentals/minimal-apis
- Swagger/OpenAPI: https://swagger.io/
- 钉钉开放平台: https://open.dingtalk.com/

---

**迁移完成！** 🎉

