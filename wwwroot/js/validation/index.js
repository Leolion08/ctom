/**
 * File index.js - Quản lý các module validation
 * Được tải sau khi tất cả các file validation khác đã được tải
 */

// Khởi tạo validation khi DOM sẵn sàng
document.addEventListener('DOMContentLoaded', function() {
    console.log('Validation modules initialized');
    
    // Khởi tạo các validation chung
    if (typeof initCommonValidations === 'function') {
        initCommonValidations();
    }
    
    // Khởi tạo validation cho từng module
    const controller = document.body.getAttribute('data-controller') || '';
    switch(controller.toLowerCase()) {
        case 'khachhangdn':
            if (typeof initKhachHangDNValidations === 'function') {
                initKhachHangDNValidations();
            }
            break;
        // Thêm các controller khác tại đây nếu cần
    }
});
