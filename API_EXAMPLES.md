# API 调用示例

本文档提供了钉钉管理系统 Minimal API 的详细调用示例。

## 基础信息

- **基础URL**：`http://localhost:5678` (Docker) 或 `http://localhost:5000` (本地运行)
- **API文档**：访问根路径 `/` 可查看 Swagger UI 文档
- **响应格式**：所有 API 返回统一的 JSON 格式

## 统一响应格式

### 成功响应
```json
{
  "success": true,
  "message": "操作成功",
  "data": {
    // 具体数据
  }
}
```

### 失败响应
```json
{
  "success": false,
  "message": "错误描述信息",
  "data": null
}
```

---

## 1. 认证相关 API

### 1.1 获取登录二维码

**请求**
```bash
GET /api/auth/qrcode
```

**cURL 示例**
```bash
curl http://localhost:5678/api/auth/qrcode
```

**响应示例**
```json
{
  "success": true,
  "message": "获取二维码成功",
  "data": {
    "qrCodeUrl": "https://oapi.dingtalk.com/connect/qrconnect?..."
  }
}
```

### 1.2 登录回调

**请求**
```bash
GET /api/auth/callback?code={授权码}&state={状态码}
```

**说明**：此接口由钉钉扫码后自动调用，无需手动调用。

**响应示例**
```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "isLoggedIn": true,
    "userId": "xxxxx",
    "userName": "张三"
  }
}
```

### 1.3 检查登录状态

**请求**
```bash
GET /api/auth/status
```

**cURL 示例**
```bash
curl http://localhost:5678/api/auth/status
```

**响应示例（已登录）**
```json
{
  "success": true,
  "message": null,
  "data": {
    "isLoggedIn": true,
    "userId": "xxxxx",
    "userName": "张三"
  }
}
```

**响应示例（未登录）**
```json
{
  "success": true,
  "message": null,
  "data": {
    "isLoggedIn": false,
    "userId": null,
    "userName": null
  }
}
```

### 1.4 退出登录

**请求**
```bash
POST /api/auth/logout
```

**cURL 示例**
```bash
curl -X POST http://localhost:5678/api/auth/logout
```

**响应示例**
```json
{
  "success": true,
  "message": "登出成功"
}
```

---

## 2. 用户管理 API

### 2.1 获取所有用户

**请求**
```bash
GET /api/users
```

**cURL 示例**
```bash
curl http://localhost:5678/api/users
```

**响应示例**
```json
{
  "success": true,
  "message": null,
  "data": [
    {
      "id": 1,
      "userId": "user001",
      "name": "张三",
      "mobile": "13800138000",
      "email": "zhangsan@example.com",
      "department": "技术部",
      "position": "工程师",
      "createdAt": "2024-11-07T10:00:00",
      "updatedAt": "2024-11-07T10:00:00"
    },
    {
      "id": 2,
      "userId": "user002",
      "name": "李四",
      "mobile": "13800138001",
      "email": "lisi@example.com",
      "department": "产品部",
      "position": "产品经理",
      "createdAt": "2024-11-07T11:00:00",
      "updatedAt": "2024-11-07T11:00:00"
    }
  ]
}
```

### 2.2 获取单个用户

**请求**
```bash
GET /api/users/{id}
```

**cURL 示例**
```bash
curl http://localhost:5678/api/users/1
```

**响应示例**
```json
{
  "success": true,
  "message": null,
  "data": {
    "id": 1,
    "userId": "user001",
    "name": "张三",
    "mobile": "13800138000",
    "email": "zhangsan@example.com",
    "department": "技术部",
    "position": "工程师",
    "createdAt": "2024-11-07T10:00:00",
    "updatedAt": "2024-11-07T10:00:00"
  }
}
```

### 2.3 创建用户

**请求**
```bash
POST /api/users
Content-Type: application/json
```

**请求体**
```json
{
  "userId": "user003",
  "name": "王五",
  "mobile": "13800138002",
  "email": "wangwu@example.com",
  "department": "市场部",
  "position": "市场专员"
}
```

**必填字段**：
- `userId`：用户ID（唯一）
- `name`：姓名

**可选字段**：
- `mobile`：手机号
- `email`：邮箱
- `department`：部门
- `position`：职位

**cURL 示例**
```bash
curl -X POST http://localhost:5678/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user003",
    "name": "王五",
    "mobile": "13800138002",
    "email": "wangwu@example.com",
    "department": "市场部",
    "position": "市场专员"
  }'
```

**响应示例**
```json
{
  "success": true,
  "message": "创建用户成功",
  "data": {
    "id": 3,
    "userId": "user003",
    "name": "王五",
    "mobile": "13800138002",
    "email": "wangwu@example.com",
    "department": "市场部",
    "position": "市场专员",
    "createdAt": "2024-11-07T12:00:00",
    "updatedAt": "2024-11-07T12:00:00"
  }
}
```

### 2.4 更新用户

**请求**
```bash
PUT /api/users/{id}
Content-Type: application/json
```

**请求体**
```json
{
  "userId": "user003",
  "name": "王五（已修改）",
  "mobile": "13800138002",
  "email": "wangwu_new@example.com",
  "department": "市场部",
  "position": "高级市场专员"
}
```

**cURL 示例**
```bash
curl -X PUT http://localhost:5678/api/users/3 \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user003",
    "name": "王五（已修改）",
    "mobile": "13800138002",
    "email": "wangwu_new@example.com",
    "department": "市场部",
    "position": "高级市场专员"
  }'
```

**响应示例**
```json
{
  "success": true,
  "message": "更新用户成功",
  "data": {
    "id": 3,
    "userId": "user003",
    "name": "王五（已修改）",
    "mobile": "13800138002",
    "email": "wangwu_new@example.com",
    "department": "市场部",
    "position": "高级市场专员",
    "createdAt": "2024-11-07T12:00:00",
    "updatedAt": "2024-11-07T13:00:00"
  }
}
```

### 2.5 删除用户

**请求**
```bash
DELETE /api/users/{id}
```

**cURL 示例**
```bash
curl -X DELETE http://localhost:5678/api/users/3
```

**响应示例**
```json
{
  "success": true,
  "message": "删除用户成功"
}
```

---

## 3. 消息管理 API

### 3.1 获取消息日志

**请求**
```bash
GET /api/messages
```

**cURL 示例**
```bash
curl http://localhost:5678/api/messages
```

**响应示例**
```json
{
  "success": true,
  "message": null,
  "data": [
    {
      "id": 1,
      "messageType": "all",
      "targetUserId": null,
      "content": "这是一条全体消息",
      "sentAt": "2024-11-07T14:00:00",
      "isSuccess": true,
      "errorMessage": null
    },
    {
      "id": 2,
      "messageType": "specific",
      "targetUserId": "user001",
      "content": "这是给张三的消息",
      "sentAt": "2024-11-07T14:30:00",
      "isSuccess": true,
      "errorMessage": null
    }
  ]
}
```

### 3.2 发送全体消息

**请求**
```bash
POST /api/messages/send-all
Content-Type: application/json
```

**请求体**
```json
{
  "content": "这是一条全体通知消息"
}
```

**cURL 示例**
```bash
curl -X POST http://localhost:5678/api/messages/send-all \
  -H "Content-Type: application/json" \
  -d '{
    "content": "这是一条全体通知消息"
  }'
```

**响应示例（成功）**
```json
{
  "success": true,
  "message": "消息发送成功"
}
```

**响应示例（失败）**
```json
{
  "success": false,
  "message": "消息发送失败"
}
```

### 3.3 发送给特定用户

**请求**
```bash
POST /api/messages/send-user
Content-Type: application/json
```

**请求体**
```json
{
  "userId": "user001",
  "content": "这是发给你的消息"
}
```

**cURL 示例**
```bash
curl -X POST http://localhost:5678/api/messages/send-user \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user001",
    "content": "这是发给你的消息"
  }'
```

**响应示例（成功）**
```json
{
  "success": true,
  "message": "消息发送成功"
}
```

**响应示例（失败）**
```json
{
  "success": false,
  "message": "消息发送失败"
}
```

---

## 4. 错误处理

### 4.1 未授权（401）

当访问需要登录的接口时，如果未登录会返回 401 状态码。

**示例**
```bash
curl http://localhost:5678/api/users
```

**响应**
```
HTTP/1.1 401 Unauthorized
```

### 4.2 未找到（404）

当访问不存在的资源时，返回 404 状态码。

**示例**
```bash
curl http://localhost:5678/api/users/999
```

**响应**
```json
{
  "success": false,
  "message": "用户不存在",
  "data": null
}
```

### 4.3 请求错误（400）

当请求参数不正确时，返回 400 状态码。

**示例**
```bash
curl -X POST http://localhost:5678/api/users \
  -H "Content-Type: application/json" \
  -d '{"name": "张三"}'  # 缺少必填的 userId
```

**响应**
```json
{
  "success": false,
  "message": "创建用户失败: 用户ID不能为空",
  "data": null
}
```

---

## 5. 完整工作流示例

### 场景：创建用户并发送消息

```bash
# 1. 检查登录状态
curl http://localhost:5678/api/auth/status

# 2. 如果未登录，获取二维码
curl http://localhost:5678/api/auth/qrcode

# 3. 扫码登录后，创建用户
curl -X POST http://localhost:5678/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "newuser",
    "name": "新用户",
    "mobile": "13900000000"
  }'

# 4. 查看所有用户
curl http://localhost:5678/api/users

# 5. 给新用户发送消息
curl -X POST http://localhost:5678/api/messages/send-user \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "newuser",
    "content": "欢迎加入！"
  }'

# 6. 查看消息记录
curl http://localhost:5678/api/messages
```

---

## 6. 前端集成示例

### JavaScript / Fetch API

```javascript
// 获取所有用户
async function getAllUsers() {
  const response = await fetch('http://localhost:5678/api/users');
  const result = await response.json();
  
  if (result.success) {
    console.log('用户列表:', result.data);
  } else {
    console.error('获取失败:', result.message);
  }
}

// 创建用户
async function createUser(userData) {
  const response = await fetch('http://localhost:5678/api/users', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(userData)
  });
  
  const result = await response.json();
  
  if (result.success) {
    console.log('创建成功:', result.data);
  } else {
    console.error('创建失败:', result.message);
  }
}

// 发送消息
async function sendMessage(content) {
  const response = await fetch('http://localhost:5678/api/messages/send-all', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ content })
  });
  
  const result = await response.json();
  
  if (result.success) {
    console.log('发送成功');
  } else {
    console.error('发送失败:', result.message);
  }
}
```

### Axios

```javascript
import axios from 'axios';

const apiClient = axios.create({
  baseURL: 'http://localhost:5678/api',
  headers: {
    'Content-Type': 'application/json'
  }
});

// 获取所有用户
apiClient.get('/users')
  .then(response => {
    if (response.data.success) {
      console.log('用户列表:', response.data.data);
    }
  })
  .catch(error => console.error('请求失败:', error));

// 创建用户
apiClient.post('/users', {
  userId: 'user123',
  name: '测试用户',
  mobile: '13800138000'
})
  .then(response => {
    if (response.data.success) {
      console.log('创建成功:', response.data.data);
    }
  })
  .catch(error => console.error('请求失败:', error));
```

---

## 7. 注意事项

1. **Session 管理**：大部分 API 需要先登录才能访问，Session 默认有效期 30 分钟
2. **CORS**：项目默认配置了允许所有来源的 CORS，生产环境需要根据实际情况调整
3. **错误处理**：所有 API 都会返回统一格式，通过 `success` 字段判断是否成功
4. **数据验证**：创建和更新操作会验证必填字段，缺少必填字段会返回 400 错误
5. **Swagger UI**：推荐使用 Swagger UI 进行 API 测试，访问根路径即可

---

## 8. 环境变量配置

在生产环境中，建议使用环境变量配置敏感信息：

```bash
export ConnectionStrings__DefaultConnection="Server=your-server;Port=3306;Database=DingDingDb;User=root;Password=your-password;"
export DingTalk__AppKey="your-app-key"
export DingTalk__AppSecret="your-app-secret"
export DingTalk__AgentId="your-agent-id"
export DingTalk__CorpId="your-corp-id"
```

或在 Docker 中：

```yaml
environment:
  - ConnectionStrings__DefaultConnection=Server=db;Port=3306;Database=DingDingDb;User=root;Password=your-password;
  - DingTalk__AppKey=your-app-key
  - DingTalk__AppSecret=your-app-secret
```

