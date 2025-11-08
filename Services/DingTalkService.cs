using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using DingDingApp.Options;

namespace DingDingApp.Services
{
    public class DingTalkService : IDingTalkService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DingTalkOptions _options;
        private readonly ILogger<DingTalkService> _logger;
        private string? _cachedAccessToken;
        private DateTime _tokenExpireTime;

        private const string DINGTALK_API_BASE = "https://oapi.dingtalk.com";
        private const string DINGTALK_OAUTH_BASE = "https://oapi.dingtalk.com/connect";
        private const string DINGTALK_SNS_BASE = "https://oapi.dingtalk.com/sns"; // 扫码登录专用 API 基础路径

        public DingTalkService(
            IHttpClientFactory httpClientFactory,
            IOptions<DingTalkOptions> options,
            ILogger<DingTalkService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            // 如果token未过期，直接返回缓存的token
            if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.Now < _tokenExpireTime)
            {
                return _cachedAccessToken;
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"{DINGTALK_API_BASE}/gettoken?appkey={_options.AppKey}&appsecret={_options.AppSecret}";

            try
            {
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(content);

                if (result.TryGetProperty("errcode", out var errcode) && errcode.GetInt32() == 0)
                {
                    _cachedAccessToken = result.GetProperty("access_token").GetString();
                    _tokenExpireTime = DateTime.Now.AddHours(1.5); // token有效期2小时，提前30分钟刷新
                    return _cachedAccessToken ?? string.Empty;
                }
                else
                {
                    var errmsg = result.TryGetProperty("errmsg", out var msg) ? msg.GetString() : "未知错误";
                    _logger.LogError("获取access_token失败: {Error}", errmsg);
                    throw new Exception($"获取access_token失败: {errmsg}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取access_token异常");
                throw;
            }
        }

        public async Task<string> GetQrCodeUrlAsync(string? baseUrl = null)
        {
            // 生成扫码登录URL
            // 注意：redirect_uri 需要与钉钉应用后台配置的回调地址完全一致
            // 如果未提供 baseUrl，使用默认值（但建议传入正确的 baseUrl）
            var callbackUrl = string.IsNullOrEmpty(baseUrl) 
                ? "http://localhost:54507/api/auth/callback" 
                : $"{baseUrl.TrimEnd('/')}/api/auth/callback";
            
            var redirectUri = Uri.EscapeDataString(callbackUrl);
            var url = $"{DINGTALK_OAUTH_BASE}/qrconnect?appid={_options.AppKey}&response_type=code&scope=snsapi_login&state=STATE&redirect_uri={redirectUri}";
            
            _logger.LogInformation("生成二维码登录URL: {Url}, 回调地址: {CallbackUrl}", url, callbackUrl);
            return url;
        }

        public async Task<Dictionary<string, object>?> GetUserInfoByCodeAsync(string code)
        {
            try
            {
                _logger.LogInformation("开始获取用户信息，code: {Code}", code);
                
                // 钉钉扫码登录流程：
                // 1. 用户扫码后，钉钉会回调redirect_uri，并带上code参数（临时授权码）
                // 2. 使用临时授权码（tmp_auth_code）和 appid、appsecret 获取 sns_token
                // 3. 使用 sns_token 获取用户信息

                var client = _httpClientFactory.CreateClient();
                
                // 第一步：使用临时授权码获取sns_token
                // 根据日志分析，正确的调用方式是：
                // - GET 请求
                // - 端点：/sns/gettoken
                // - 参数：tmp_auth_code, appid, appsecret
                // 
                // 但是返回 40001 错误（Secret错误），可能的原因：
                // 1. AppSecret 配置错误
                // 2. 需要使用扫码登录应用对应的 AppSecret（扫码登录应用和普通应用可能使用不同的 AppSecret）
                // 3. 或者需要使用 access_token 而不是 AppSecret
                //
                // 根据钉钉文档，扫码登录可能需要使用 access_token 作为参数
                // 尝试：先获取 access_token，然后使用 access_token 获取 sns_token
                
                HttpResponseMessage tokenResponse;
                string tokenContent;
                
                // 尝试方式1：直接使用 AppKey 和 AppSecret
                var tokenUrl1 = $"{DINGTALK_SNS_BASE}/gettoken?tmp_auth_code={Uri.EscapeDataString(code)}&appid={Uri.EscapeDataString(_options.AppKey)}&appsecret={Uri.EscapeDataString(_options.AppSecret)}";
                _logger.LogInformation("尝试方式1 - 直接使用 AppKey/AppSecret: {Url}", tokenUrl1.Replace(_options.AppSecret, "***"));
                
                tokenResponse = await client.GetAsync(tokenUrl1);
                tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("方式1响应: {Content}", tokenContent);
                
                var result1 = JsonSerializer.Deserialize<JsonElement>(tokenContent);
                bool success = false;
                
                if (result1.TryGetProperty("errcode", out var errcode1) && errcode1.GetInt32() == 0)
                {
                    _logger.LogInformation("方式1成功！");
                    success = true;
                }
                else if (result1.TryGetProperty("errcode", out var err1) && err1.GetInt32() == 40001)
                {
                    // 40001 错误：Secret错误
                    // 说明：普通API的access_token可以获取成功，但扫码登录API需要不同的认证方式
                    // 可能的原因：扫码登录应用需要使用单独的 AppSecret（不同于普通应用）
                    _logger.LogInformation("方式1返回40001 (Secret错误)，尝试方式2：使用 access_token + appid + appsecret");
                    
                    // 方式2：先获取 access_token，然后使用 access_token + appid + appsecret 获取 sns_token
                    try
                    {
                        var accessToken = await GetAccessTokenAsync();
                        _logger.LogInformation("成功获取 access_token: {Token}", accessToken.Substring(0, Math.Min(20, accessToken.Length)) + "...");
                        
                        // 尝试同时使用 access_token、appid 和 appsecret
                        var tokenUrl2 = $"{DINGTALK_SNS_BASE}/gettoken?tmp_auth_code={Uri.EscapeDataString(code)}&access_token={Uri.EscapeDataString(accessToken)}&appid={Uri.EscapeDataString(_options.AppKey)}&appsecret={Uri.EscapeDataString(_options.AppSecret)}";
                        _logger.LogInformation("尝试方式2 - 使用 access_token + appid + appsecret: {Url}", tokenUrl2.Replace(accessToken, "***").Replace(_options.AppSecret, "***"));
                        
                        tokenResponse = await client.GetAsync(tokenUrl2);
                        tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                        _logger.LogInformation("方式2响应: {Content}", tokenContent);
                        
                        var result2 = JsonSerializer.Deserialize<JsonElement>(tokenContent);
                        if (result2.TryGetProperty("errcode", out var errcode2) && errcode2.GetInt32() == 0)
                        {
                            _logger.LogInformation("方式2成功！");
                            success = true;
                            result1 = result2; // 使用方式2的结果
                        }
                        else
                        {
                            // 如果方式2也失败，使用方式2的结果作为最终错误信息
                            var errcode2Value = result2.TryGetProperty("errcode", out var err2) ? err2.GetInt32() : -1;
                            var errmsg2 = result2.TryGetProperty("errmsg", out var msg2) ? msg2.GetString() : "未知错误";
                            _logger.LogWarning("方式2失败: errcode={Errcode}, errmsg={Errmsg}", errcode2Value, errmsg2);
                            
                            // 使用方式2的结果作为最终错误信息（更新 result1 和 tokenContent）
                            result1 = result2;
                            // tokenContent 已经是方式2的响应了
                            
                            // 如果方式2仍然返回 40001 或 40035，说明 AppSecret 确实有问题
                            // 此时应该明确告诉用户：需要使用扫码登录应用的 AppSecret
                            if (errcode2Value == 40001 || errcode2Value == 40035)
                            {
                                _logger.LogError("方式2仍然失败，确认问题：扫码登录应用需要使用单独的 AppSecret");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "方式2失败: {Error}", ex.Message);
                    }
                }
                
                // 解析最终响应（使用最新的响应）
                var tokenResult = result1;

                // 成功获取 sns_token
                var errcode = tokenResult.GetProperty("errcode");
                var snsToken = tokenResult.GetProperty("sns_token").GetString();
                if (string.IsNullOrEmpty(snsToken))
                {
                    _logger.LogWarning("sns_token为空，响应内容: {Content}", tokenContent);
                    throw new Exception("获取sns_token失败: sns_token为空");
                }

                _logger.LogInformation("成功获取sns_token");

                // 第二步：使用sns_token获取用户信息
                var userInfoUrl = $"{DINGTALK_SNS_BASE}/getuserinfo?sns_token={Uri.EscapeDataString(snsToken)}";
                _logger.LogInformation("请求用户信息 URL: {Url}", userInfoUrl);
                
                var userInfoResponse = await client.GetAsync(userInfoUrl);
                var userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("获取用户信息响应状态: {StatusCode}, 内容: {Content}", 
                    userInfoResponse.StatusCode, userInfoContent);
                
                var userInfoResult = JsonSerializer.Deserialize<JsonElement>(userInfoContent);

                if (userInfoResult.TryGetProperty("errcode", out var userErrcode))
                {
                    var userErrcodeValue = userErrcode.GetInt32();
                    if (userErrcodeValue == 0)
                    {
                        var userInfo = new Dictionary<string, object>();
                        if (userInfoResult.TryGetProperty("user_info", out var userInfoObj))
                        {
                            userInfo["nick"] = userInfoObj.TryGetProperty("nick", out var nick) ? nick.GetString() ?? "" : "";
                            userInfo["openid"] = userInfoObj.TryGetProperty("openid", out var openid) ? openid.GetString() ?? "" : "";
                            userInfo["unionid"] = userInfoObj.TryGetProperty("unionid", out var unionid) ? unionid.GetString() ?? "" : "";
                            
                            _logger.LogInformation("成功获取用户信息: nick={Nick}, openid={OpenId}, unionid={UnionId}", 
                                userInfo["nick"], userInfo["openid"], userInfo["unionid"]);
                        }
                        else
                        {
                            _logger.LogWarning("响应中没有 user_info 字段，完整响应: {Content}", userInfoContent);
                        }
                        return userInfo;
                    }
                    else
                    {
                        var errmsg = userInfoResult.TryGetProperty("errmsg", out var msg) ? msg.GetString() : "未知错误";
                        _logger.LogError("获取用户信息失败: errcode={Errcode}, errmsg={Errmsg}, 完整响应: {Content}", 
                            userErrcodeValue, errmsg, userInfoContent);
                        throw new Exception($"获取用户信息失败: errcode={userErrcodeValue}, errmsg={errmsg}");
                    }
                }
                else
                {
                    _logger.LogError("响应中没有 errcode 字段，完整响应: {Content}", userInfoContent);
                    throw new Exception($"获取用户信息失败: 响应格式错误，内容: {userInfoContent}");
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户信息异常");
                return null;
            }
        }

        public async Task<bool> SendMessageToAllAsync(string content)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                var client = _httpClientFactory.CreateClient();

                // 构建消息体 - 发送工作通知给所有人
                var messageBody = new
                {
                    agent_id = _options.AgentId,
                    to_all_user = true,
                    msg = new
                    {
                        msgtype = "text",
                        text = new
                        {
                            content = content
                        }
                    }
                };

                var json = JsonSerializer.Serialize(messageBody);
                var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{DINGTALK_API_BASE}/topapi/message/corpconversation/asyncsend_v2?access_token={accessToken}";

                var response = await client.PostAsync(url, requestContent);
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (result.TryGetProperty("errcode", out var errcode) && errcode.GetInt32() == 0)
                {
                    return true;
                }
                else
                {
                    var errmsg = result.TryGetProperty("errmsg", out var msg) ? msg.GetString() : "未知错误";
                    _logger.LogError("发送全体消息失败: {Error}", errmsg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送全体消息异常");
                return false;
            }
        }

        public async Task<bool> SendMessageToUserAsync(string userId, string content)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                var client = _httpClientFactory.CreateClient();

                // 构建消息体 - 发送工作通知给特定用户
                var messageBody = new
                {
                    agent_id = _options.AgentId,
                    userid_list = userId,
                    msg = new
                    {
                        msgtype = "text",
                        text = new
                        {
                            content = content
                        }
                    }
                };

                var json = JsonSerializer.Serialize(messageBody);
                var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{DINGTALK_API_BASE}/topapi/message/corpconversation/asyncsend_v2?access_token={accessToken}";

                var response = await client.PostAsync(url, requestContent);
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (result.TryGetProperty("errcode", out var errcode) && errcode.GetInt32() == 0)
                {
                    return true;
                }
                else
                {
                    var errmsg = result.TryGetProperty("errmsg", out var msg) ? msg.GetString() : "未知错误";
                    _logger.LogError("发送用户消息失败: {Error}", errmsg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送用户消息异常");
                return false;
            }
        }
    }
}

