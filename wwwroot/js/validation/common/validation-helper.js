/**
 * Các hàm helper hỗ trợ validation
 */

/**
 * Định dạng lại số điện thoại khi nhập liệu
 * @param {string} phoneNumber - Số điện thoại cần định dạng
 * @returns {string} Số điện thoại đã được định dạng
 */
function formatPhoneNumber(phoneNumber) {
    if (!phoneNumber) return '';
    
    // Xóa tất cả ký tự không phải số
    const cleaned = phoneNumber.replace(/\D/g, '');
    
    // Định dạng lại số điện thoại
    if (cleaned.length >= 10) {
        if (cleaned.startsWith('84')) {
            return `+${cleaned.substring(0, 2)} ${cleaned.substring(2, 5)} ${cleaned.substring(5, 8)} ${cleaned.substring(8)}`;
        } else if (cleaned.startsWith('0')) {
            return `+84 ${cleaned.substring(1, 4)} ${cleaned.substring(4, 7)} ${cleaned.substring(7)}`;
        }
    }
    
    return phoneNumber; // Trả về nguyên bản nếu không thể định dạng
}

/**
 * Kiểm tra xem một phần tử có hiển thị trên màn hình không
 * @param {jQuery} $element - Phần tử cần kiểm tra
 * @returns {boolean} Trả về true nếu phần tử hiển thị
 */
function isElementVisible($element) {
    return $element.is(':visible') && 
           $element.css('visibility') !== 'hidden' && 
           $element.css('opacity') > 0;
}

/**
 * Cuộn đến trường bị lỗi đầu tiên
 */
function scrollToFirstError() {
    const $firstError = $('.is-invalid').first();
    if ($firstError.length) {
        $('html, body').animate({
            scrollTop: $firstError.offset().top - 100
        }, 500);
        
        // Focus vào trường bị lỗi
        $firstError.focus();
    }
}

/**
 * Khởi tạo datepicker với cấu hình mặc định
 * @param {string} selector - Selector của các phần tử datepicker
 * @param {Object} options - Các tùy chọn bổ sung
 */
function initDatepicker(selector, options = {}) {
    const defaultOptions = {
        format: 'dd/mm/yyyy',
        autoclose: true,
        todayHighlight: true,
        language: 'vi',
        orientation: 'bottom auto',
        templates: {
            leftArrow: '<i class="fas fa-chevron-left"></i>',
            rightArrow: '<i class="fas fa-chevron-right"></i>'
        }
    };
    
    $(selector).datepicker({ ...defaultOptions, ...options });
}

// Xuất các hàm để sử dụng trong các module khác
window.validationHelper = {
    formatPhoneNumber,
    isElementVisible,
    scrollToFirstError,
    initDatepicker
};
