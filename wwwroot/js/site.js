// 生成二维码的函数
window.generateQRCode = function(url) {
    // 清空之前的二维码
    const container = document.getElementById('qrcode');
    if (container) {
        container.innerHTML = '';
        
        // 使用 QRCode.js 生成二维码
        // 由于我们没有引入 QRCode 库，先使用在线二维码 API
        const qrUrl = `https://api.qrserver.com/v1/create-qr-code/?size=256x256&data=${encodeURIComponent(url)}`;
        const img = document.createElement('img');
        img.src = qrUrl;
        img.alt = '二维码';
        img.style.width = '256px';
        img.style.height = '256px';
        container.appendChild(img);
    }
};

// 初始化钉钉登录二维码
window.initDingTalkQRCode = function(qrCodeUrl) {
    console.log('开始初始化钉钉登录二维码，URL: ', qrCodeUrl);
    
    try {
        // 查找登录容器
        var container = document.getElementById('login_container');
        
        if (!container) {
            console.error('找不到登录容器元素 (login_container)');
            return;
        }
        
        // 检查 DDLogin 函数是否已加载
        if (typeof DDLogin === 'undefined') {
            console.error('钉钉登录SDK未加载，请检查网络连接');
            container.innerHTML = '<p style="color: #f00; padding: 20px;">二维码加载失败：SDK未加载，请刷新页面重试</p>';
            return;
        }
        
        // 清空容器内容
        container.innerHTML = '';
        
        // 使用钉钉官方 SDK 生成二维码
        var obj = DDLogin({
            id: 'login_container',
            goto: encodeURIComponent(qrCodeUrl),
            style: "border:none;background-color:#FFFFFF;",
            width: "270",
            height: "270"
        });
        
        console.log('钉钉官方二维码已加载');
        
        // 监听来自钉钉登录页面的消息（当用户扫描二维码后）
        if (!window.dingTalkMessageHandlerAdded) {
            var handleMessage = function (event) {
                var origin = event.origin;
                if (origin == "https://login.dingtalk.com" || origin.includes("dingtalk.com")) {
                    var loginTmpCode = event.data;
                    console.log('收到钉钉登录消息:', loginTmpCode);
                    
                    if (loginTmpCode && loginTmpCode.length > 0) {
                        console.log('用户已扫描二维码，等待登录结果...');
                    }
                }
            };
            
            if (typeof window.addEventListener != 'undefined') {
                window.addEventListener('message', handleMessage, false);
            } else if (typeof window.attachEvent != 'undefined') {
                window.attachEvent('onmessage', handleMessage);
            }
            
            window.dingTalkMessageHandlerAdded = true;
        }
    } catch (e) {
        console.error('初始化钉钉登录二维码失败:', e);
        var container = document.getElementById('login_container');
        if (container) {
            container.innerHTML = '<p style="color: #f00; padding: 20px;">二维码加载失败: ' + e.message + '</p>';
        }
    }
};

// 加载钉钉 SDK
window.loadDingTalkSDK = function() {
    return new Promise(function(resolve, reject) {
        // 检查 SDK 是否已加载
        if (typeof DDLogin !== 'undefined') {
            console.log('钉钉 SDK 已加载');
            resolve();
            return;
        }
        
        // 检查是否已经在加载中
        if (document.getElementById('dingtalk-login-sdk')) {
            console.log('钉钉 SDK 正在加载中...');
            // 等待加载完成
            var checkInterval = setInterval(function() {
                if (typeof DDLogin !== 'undefined') {
                    clearInterval(checkInterval);
                    console.log('钉钉 SDK 加载完成');
                    resolve();
                }
            }, 100);
            
            // 超时处理（5秒）
            setTimeout(function() {
                clearInterval(checkInterval);
                if (typeof DDLogin === 'undefined') {
                    reject(new Error('钉钉 SDK 加载超时'));
                }
            }, 5000);
            return;
        }
        
        // 创建 script 标签
        var script = document.createElement('script');
        script.id = 'dingtalk-login-sdk';
        script.src = 'https://g.alicdn.com/dingding/dinglogin/0.0.5/ddLogin.js';
        script.async = true;
        
        script.onload = function() {
            console.log('钉钉登录 SDK 脚本加载成功');
            // 等待一下，确保 DDLogin 函数已定义
            setTimeout(function() {
                if (typeof DDLogin !== 'undefined') {
                    resolve();
                } else {
                    reject(new Error('钉钉 SDK 加载完成，但 DDLogin 函数未定义'));
                }
            }, 100);
        };
        
        script.onerror = function() {
            console.error('钉钉登录 SDK 脚本加载失败');
            reject(new Error('钉钉 SDK 脚本加载失败，请检查网络连接'));
        };
        
        document.head.appendChild(script);
    });
};
