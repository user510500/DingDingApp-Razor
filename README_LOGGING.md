# 日志查看指南

## 日志输出位置

### 1. 控制台输出（实时查看）
如果你是从命令行运行应用，日志会直接输出到控制台：

```bash
dotnet run
```

**查看日志的方法：**
- 如果使用 `dotnet run`，日志会显示在运行应用的终端窗口中
- 如果使用 Visual Studio，日志会显示在"输出"窗口中（视图 → 输出 → 显示输出来源：调试）

### 2. 文件日志（推荐）
日志文件会保存在项目根目录下的 `logs` 文件夹中：

- **日志文件路径：** `logs/dingding-YYYY-MM-DD.log`
- **示例：** `logs/dingding-2024-01-15.log`

**查看日志文件的方法：**
1. 打开项目根目录
2. 进入 `logs` 文件夹
3. 找到最新日期的日志文件
4. 使用文本编辑器打开查看

### 3. 在代码中查看
如果你在调试，可以在以下位置设置断点查看日志：
- `Program.cs` - 登录回调处理
- `Services/DingTalkService.cs` - 钉钉 API 调用

## 重要日志信息

当登录失败时，请查找以下日志：

### 获取 sns_token 相关日志
```
开始获取用户信息，code: {Code}
请求sns_token URL: {Url}, 请求体: {Body}
获取sns_token响应状态: {StatusCode}, 内容: {Content}
```

### 获取用户信息相关日志
```
请求用户信息 URL: {Url}
获取用户信息响应状态: {StatusCode}, 内容: {Content}
```

### 错误日志
```
获取sns_token失败: errcode={Errcode}, errmsg={Errmsg}
获取用户信息失败: errcode={Errcode}, errmsg={Errmsg}
```

## 快速查看日志的方法

### Windows PowerShell
```powershell
# 查看最新的日志文件
Get-Content logs\dingding-*.log -Tail 50

# 实时查看日志（类似 tail -f）
Get-Content logs\dingding-*.log -Wait -Tail 20
```

### Windows CMD
```cmd
# 查看最新的日志文件
type logs\dingding-*.log | more
```

### 使用文本编辑器
直接打开 `logs` 文件夹中的最新日志文件，使用 Ctrl+F 搜索关键字：
- "获取sns_token"
- "获取用户信息"
- "errmsg"
- "errcode"

## 调试登录问题

如果登录失败，请按以下步骤检查日志：

1. **查看是否有 API 调用日志**
   - 搜索 "请求sns_token"
   - 搜索 "获取用户信息"

2. **查看错误信息**
   - 搜索 "errmsg" 或 "errcode"
   - 查看完整的错误响应内容

3. **检查响应内容**
   - 查看 "获取sns_token响应" 的完整内容
   - 查看 "获取用户信息响应" 的完整内容

4. **常见错误码**
   - `40001`: 不合法的 accessKey
   - `40002`: 不合法的 secretKey
   - `40014`: 不合法的临时授权码
   - `40003`: 不合法的请求参数

## 临时调试方法

如果无法查看日志文件，可以在代码中临时添加以下代码来输出日志到页面：

在 `Program.cs` 的登录回调处理中，可以临时返回错误详情：

```csharp
catch (Exception ex)
{
    logger.LogError(ex, "登录回调处理失败");
    // 临时：返回详细错误信息（生产环境请移除）
    var errorDetails = $"{ex.Message}\n\n堆栈跟踪:\n{ex.StackTrace}";
    var errorMsg = Uri.EscapeDataString($"登录失败：{errorDetails}");
    return Results.Redirect($"/?error={errorMsg}");
}
```

**注意：** 生产环境请移除详细错误信息，避免泄露敏感信息。

