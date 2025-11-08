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

