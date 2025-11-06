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

        public async Task<string> GetQrCodeUrlAsync()
        {
            // 生成扫码登录URL
            var redirectUri = Uri.EscapeDataString("http://localhost:8080/Home/Callback");
            var url = $"{DINGTALK_OAUTH_BASE}/qrconnect?appid={_options.AppKey}&response_type=code&scope=snsapi_login&state=STATE&redirect_uri={redirectUri}";
            return url;
        }

        public async Task<Dictionary<string, object>?> GetUserInfoByCodeAsync(string code)
        {
            try
            {
                // 第一步：通过code获取临时授权码
                var client = _httpClientFactory.CreateClient();
                var tempCodeUrl = $"{DINGTALK_OAUTH_BASE}/oauth2/sns_authorize?appid={_options.AppKey}&response_type=code&scope=snsapi_login&state=STATE&redirect_uri={Uri.EscapeDataString("http://localhost:8080/Home/Callback")}";
                
                // 实际上，钉钉的扫码登录流程是：
                // 1. 用户扫码后，钉钉会回调redirect_uri，并带上code参数
                // 2. 使用code换取access_token
                // 3. 使用access_token获取用户信息

                // 使用code获取sns_token
                var tokenUrl = $"{DINGTALK_OAUTH_BASE}/sns/gettoken_bycode?tmp_auth_code={code}";
                var tokenResponse = await client.GetAsync(tokenUrl);
                var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                var tokenResult = JsonSerializer.Deserialize<JsonElement>(tokenContent);

                if (tokenResult.TryGetProperty("errcode", out var errcode) && errcode.GetInt32() == 0)
                {
                    var snsToken = tokenResult.GetProperty("sns_token").GetString();
                    if (string.IsNullOrEmpty(snsToken))
                    {
                        return null;
                    }

                    // 使用sns_token获取用户信息
                    var userInfoUrl = $"{DINGTALK_OAUTH_BASE}/sns/getuserinfo?sns_token={snsToken}";
                    var userInfoResponse = await client.GetAsync(userInfoUrl);
                    var userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
                    var userInfoResult = JsonSerializer.Deserialize<JsonElement>(userInfoContent);

                    if (userInfoResult.TryGetProperty("errcode", out var userErrcode) && userErrcode.GetInt32() == 0)
                    {
                        var userInfo = new Dictionary<string, object>();
                        if (userInfoResult.TryGetProperty("user_info", out var userInfoObj))
                        {
                            userInfo["nick"] = userInfoObj.TryGetProperty("nick", out var nick) ? nick.GetString() ?? "" : "";
                            userInfo["openid"] = userInfoObj.TryGetProperty("openid", out var openid) ? openid.GetString() ?? "" : "";
                            userInfo["unionid"] = userInfoObj.TryGetProperty("unionid", out var unionid) ? unionid.GetString() ?? "" : "";
                        }
                        return userInfo;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户信息失败");
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

