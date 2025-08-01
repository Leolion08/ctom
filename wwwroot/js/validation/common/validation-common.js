/**
 * Các hàm validation dùng chung cho toàn bộ ứng dụng
 * @namespace appValidation
 */

/**
 * Kiểm tra số điện thoại Việt Nam hợp lệ
 * @param {string} phoneNumber - Số điện thoại cần kiểm tra
 * @returns {boolean}
 */
function isValidVietnamesePhoneNumber(phoneNumber) {
    if (!phoneNumber) return false;
    // Loại bỏ khoảng trắng và dấu cách
    const cleaned = phoneNumber.replace(/\s+/g, '');
    // Định dạng: 84 hoặc 0 đứng đầu, theo sau là 3|5|7|8|9, và 8 hoặc 9 chữ số
    const regex = /^(\+?84|0)(3[2-9]|5[2689]|7[06-9]|8[1-9]|9\d)(\d{7})$/;
    return regex.test(cleaned);
}

/**
 * Kiểm tra email hợp lệ
 * @param {string} email - Email cần kiểm tra
 * @returns {boolean}
 */
function isValidEmail(email) {
    if (!email) return false;
    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return regex.test(email);
}

/**
 * Kiểm tra ngày có hợp lệ không
 * @param {string} dateString - Chuỗi ngày tháng (dd/MM/yyyy)
 * @returns {boolean}
 */
function isValidDate(dateString) {
    if (!dateString) return false;
    
    // Kiểm tra định dạng dd/MM/yyyy
    const dateRegex = /^\d{2}\/\d{2}\/\d{4}$/;
    if (!dateRegex.test(dateString)) return false;
    
    // Tách ngày, tháng, năm
    const parts = dateString.split('/');
    const day = parseInt(parts[0], 10);
    const month = parseInt(parts[1], 10) - 1; // Tháng trong JavaScript bắt đầu từ 0
    const year = parseInt(parts[2], 10);
    
    // Kiểm tra giá trị hợp lệ
    if (day < 1 || day > 31) return false;
    if (month < 0 || month > 11) return false;
    if (year < 1900 || year > 2100) return false;
    
    // Tạo đối tượng Date và kiểm tra tính hợp lệ
    const date = new Date(year, month, day);
    return date.getFullYear() === year && 
           date.getMonth() === month && 
           date.getDate() === day;
}

/**
 * Định dạng lại số điện thoại theo chuẩn Việt Nam
 * @param {string} phoneNumber - Số điện thoại cần định dạng
 * @returns {string} Số điện thoại đã được định dạng
 */
function formatVietnamesePhoneNumber(phoneNumber) {
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
 * Kiểm tra xem một ngày có sau một ngày khác không
 * @param {string} date1 - Ngày thứ nhất (dd/MM/yyyy)
 * @param {string} date2 - Ngày thứ hai (dd/MM/yyyy)
 * @returns {boolean} Trả về true nếu date1 > date2
 */
function isDateAfter(date1, date2) {
    if (!date1 || !date2) return false;
    
    const parseDate = (dateStr) => {
        if (!dateStr) return null;
        const [day, month, year] = dateStr.split('/').map(Number);
        return new Date(year, month - 1, day);
    };
    
    const d1 = parseDate(date1);
    const d2 = parseDate(date2);
    
    if (!d1 || !d2) return false;
    
    return d1 > d2;
}

/**
 * Kiểm tra xem một chuỗi có phải là số không
 * @param {string} value - Giá trị cần kiểm tra
 * @returns {boolean}
 */
function isNumeric(value) {
    if (typeof value === 'number') return true;
    if (typeof value !== 'string') return false;
    return /^\d+$/.test(value.trim());
}

/**
 * Kiểm tra độ dài tối thiểu và tối đa của chuỗi
 * @param {string} value - Chuỗi cần kiểm tra
 * @param {number} min - Độ dài tối thiểu
 * @param {number} max - Độ dài tối đa
 * @returns {boolean}
 */
function isLength(value, min, max) {
    if (value === undefined || value === null) return false;
    const length = String(value).trim().length;
    return length >= min && length <= max;
}

// Xuất các hàm để sử dụng trong các module khác
window.appValidation = {
    isValidVietnamesePhoneNumber,
    isValidEmail,
    isValidDate,
    formatVietnamesePhoneNumber,
    isDateAfter,
    isNumeric,
    isLength
};
