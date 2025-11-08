using System.Net.Http.Json;
using DingDingApp.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace DingDingApp.Services
{
    public class ApiService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly NavigationManager _navigationManager;
        private readonly ILogger<ApiService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiService(IHttpClientFactory clientFactory,
                          NavigationManager navigationManager,
                          ILogger<ApiService> logger,
                          IHttpContextAccessor httpContextAccessor)
        {
            _clientFactory = clientFactory;
            _navigationManager = navigationManager;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetBaseUrl()
        {
            var baseUri = _navigationManager.BaseUri;
            return string.IsNullOrWhiteSpace(baseUri) ? string.Empty : baseUri.TrimEnd('/');
        }

        private Uri BuildUri(string uri)
        {
            if (Uri.TryCreate(uri, UriKind.Absolute, out var absolute))
            {
                return absolute;
            }

            return _navigationManager.ToAbsoluteUri(uri);
        }

        private string? GetCookieHeader()
        {
            // 在 Blazor Server 中，从 HttpContext 获取 Cookie
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null && httpContext.Request.Headers.ContainsKey("Cookie"))
            {
                return httpContext.Request.Headers["Cookie"].ToString();
            }
            return null;
        }

        private async Task<T?> GetJsonAsync<T>(string uri)
        {
            try
            {
                var requestUri = BuildUri(uri);
                var client = _clientFactory.CreateClient();
                
                // 使用 HttpRequestMessage 来设置 Cookie（因为 Cookie 是受限制的头部）
                var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                var cookieHeader = GetCookieHeader();
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    request.Headers.Add("Cookie", cookieHeader);
                }
                
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP请求失败: {Uri}", uri);
                throw;
            }
        }

        private async Task<T?> PostJsonAsync<T>(string uri, object content)
        {
            try
            {
                var requestUri = BuildUri(uri);
                var client = _clientFactory.CreateClient();
                
                var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = JsonContent.Create(content)
                };
                
                var cookieHeader = GetCookieHeader();
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    request.Headers.Add("Cookie", cookieHeader);
                }
                
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP POST请求失败: {Uri}", uri);
                throw;
            }
        }

        private async Task<T?> PutJsonAsync<T>(string uri, object content)
        {
            try
            {
                var requestUri = BuildUri(uri);
                var client = _clientFactory.CreateClient();
                
                var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
                {
                    Content = JsonContent.Create(content)
                };
                
                var cookieHeader = GetCookieHeader();
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    request.Headers.Add("Cookie", cookieHeader);
                }
                
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP PUT请求失败: {Uri}", uri);
                throw;
            }
        }

        private async Task<T?> DeleteJsonAsync<T>(string uri)
        {
            try
            {
                var requestUri = BuildUri(uri);
                var client = _clientFactory.CreateClient();
                
                var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
                var cookieHeader = GetCookieHeader();
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    request.Headers.Add("Cookie", cookieHeader);
                }
                
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP DELETE请求失败: {Uri}", uri);
                throw;
            }
        }

        private async Task<T?> PostAsync<T>(string uri)
        {
            try
            {
                var requestUri = BuildUri(uri);
                var client = _clientFactory.CreateClient();
                
                var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
                var cookieHeader = GetCookieHeader();
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    request.Headers.Add("Cookie", cookieHeader);
                }
                
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP POST请求失败: {Uri}", uri);
                throw;
            }
        }

        // 认证相关
        public async Task<ApiResponse<AuthResponse>> GetQrCodeAsync()
        {
            try
            {
                var response = await GetJsonAsync<ApiResponse<AuthResponse>>("/api/auth/qrcode");
                return response ?? ApiResponse<AuthResponse>.FailResult("获取二维码失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取二维码失败");
                return ApiResponse<AuthResponse>.FailResult($"获取二维码失败: {ex.Message}");
            }
        }

        public async Task<ApiResponse<LoginStatusResponse>> GetLoginStatusAsync()
        {
            try
            {
                var response = await GetJsonAsync<ApiResponse<LoginStatusResponse>>("/api/auth/status");
                return response ?? ApiResponse<LoginStatusResponse>.FailResult("获取登录状态失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取登录状态失败");
                return ApiResponse<LoginStatusResponse>.FailResult($"获取登录状态失败: {ex.Message}");
            }
        }

        public async Task<ApiResponse> LogoutAsync()
        {
            try
            {
                var result = await PostAsync<ApiResponse>("/api/auth/logout");
                return result ?? ApiResponse.SuccessResult("登出成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登出失败");
                return ApiResponse.FailResult($"登出失败: {ex.Message}");
            }
        }

        public async Task<ApiResponse<LoginStatusResponse>> DevLoginAsync()
        {
            try
            {
                _logger.LogInformation("调用开发模式登录 API");
                var requestUri = BuildUri("/api/auth/dev-login");
                var client = _clientFactory.CreateClient();
                
                // 发送 POST 请求
                var response = await client.PostAsync(requestUri, null);
                
                _logger.LogInformation("开发模式登录响应状态: {StatusCode}", response.StatusCode);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginStatusResponse>>();
                    return result ?? ApiResponse<LoginStatusResponse>.FailResult("开发模式登录失败：响应数据为空");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("开发模式登录失败 - 状态码: {StatusCode}, 内容: {Content}", 
                        response.StatusCode, errorContent);
                    
                    // 尝试解析错误响应
                    try
                    {
                        var errorResult = await response.Content.ReadFromJsonAsync<ApiResponse>();
                        return ApiResponse<LoginStatusResponse>.FailResult(
                            errorResult?.Message ?? $"开发模式登录失败: {response.StatusCode}");
                    }
                    catch
                    {
                        return ApiResponse<LoginStatusResponse>.FailResult(
                            $"开发模式登录失败: {response.StatusCode} - {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开发模式登录异常");
                return ApiResponse<LoginStatusResponse>.FailResult($"开发模式登录失败: {ex.Message}");
            }
        }

        // 用户管理
        public async Task<ApiResponse<List<UserResponse>>> GetUsersAsync()
        {
            try
            {
                var response = await GetJsonAsync<ApiResponse<List<UserResponse>>>("/api/users");
                return response ?? ApiResponse<List<UserResponse>>.FailResult("获取用户列表失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户列表失败");
                return ApiResponse<List<UserResponse>>.FailResult($"获取用户列表失败: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UserResponse>> GetUserAsync(int id)
        {
            try
            {
                var response = await GetJsonAsync<ApiResponse<UserResponse>>($"/api/users/{id}");
                return response ?? ApiResponse<UserResponse>.FailResult("获取用户失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户失败");
                return ApiResponse<UserResponse>.FailResult($"获取用户失败: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UserResponse>> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                var result = await PostJsonAsync<ApiResponse<UserResponse>>("/api/users", request);
                return result ?? ApiResponse<UserResponse>.FailResult("创建用户失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户失败");
                return ApiResponse<UserResponse>.FailResult($"创建用户失败: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UserResponse>> UpdateUserAsync(int id, UpdateUserRequest request)
        {
            try
            {
                var result = await PutJsonAsync<ApiResponse<UserResponse>>($"/api/users/{id}", request);
                return result ?? ApiResponse<UserResponse>.FailResult("更新用户失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户失败");
                return ApiResponse<UserResponse>.FailResult($"更新用户失败: {ex.Message}");
            }
        }

        public async Task<ApiResponse> DeleteUserAsync(int id)
        {
            try
            {
                var result = await DeleteJsonAsync<ApiResponse>($"/api/users/{id}");
                return result ?? ApiResponse.FailResult("删除用户失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败");
                return ApiResponse.FailResult($"删除用户失败: {ex.Message}");
            }
        }

        // 消息管理
        public async Task<ApiResponse<List<DingDingApp.Models.MessageLog>>> GetMessageLogsAsync()
        {
            try
            {
                var response = await GetJsonAsync<ApiResponse<List<DingDingApp.Models.MessageLog>>>("/api/messages");
                return response ?? ApiResponse<List<DingDingApp.Models.MessageLog>>.FailResult("获取消息日志失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取消息日志失败");
                return ApiResponse<List<DingDingApp.Models.MessageLog>>.FailResult($"获取消息日志失败: {ex.Message}");
            }
        }

        public async Task<ApiResponse> SendMessageToAllAsync(SendMessageRequest request)
        {
            try
            {
                var result = await PostJsonAsync<ApiResponse>("/api/messages/send-all", request);
                return result ?? ApiResponse.FailResult("发送消息失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送全体消息失败");
                return ApiResponse.FailResult($"发送消息失败: {ex.Message}");
            }
        }

        public async Task<ApiResponse> SendMessageToUserAsync(SendMessageToUserRequest request)
        {
            try
            {
                var result = await PostJsonAsync<ApiResponse>("/api/messages/send-user", request);
                return result ?? ApiResponse.FailResult("发送消息失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送用户消息失败");
                return ApiResponse.FailResult($"发送消息失败: {ex.Message}");
            }
        }
    }
}

