/**
 * Xử lý validation cho chức năng import Excel
 */
var ExcelImportValidation = (function() {
    function ExcelImportValidation(options) {
        if (options === void 0) { options = {}; }
        this.maxFileSizeMB = options.maxFileSizeMB || 10;
        this.maxFileSizeBytes = this.maxFileSizeMB * 1024 * 1024;
        this.allowedExtensions = ['.xlsx'];
        this.initializeFileValidation();
    }

    /**
     * Khởi tạo sự kiện kiểm tra file
     */
    ExcelImportValidation.prototype.initializeFileValidation = function() {
        var _this = this;
        // Xử lý khi chọn file
        $(document).on('change', '#excelFile', function(e) {
            var file = e.target.files[0];
            if (file) {
                _this.validateFile(file);
            }
        });
    };

    /**
     * Validate file trước khi upload
     * @param {File} file - File cần validate
     * @returns {boolean} - Trả về true nếu file hợp lệ
     */
    ExcelImportValidation.prototype.validateFile = function(file) {
        var $fileInput = $('#excelFile');
        var $errorContainer = $('#fileError');
        var $submitButton = $('#importForm button[type="submit"]');

        // Reset trạng thái lỗi
        $fileInput.removeClass('is-invalid');
        $errorContainer.text('');
        $submitButton.prop('disabled', false);

        // Kiểm tra file có tồn tại không
        if (!file) {
            this.showError('Vui lòng chọn file Excel để import');
            return false;
        }

        // Kiểm tra định dạng file
        var fileExt = '.' + file.name.split('.').pop().toLowerCase();
        if (this.allowedExtensions.indexOf(fileExt) === -1) {
            this.showError('Chỉ chấp nhận file có định dạng: ' + this.allowedExtensions.join(', '));
            $submitButton.prop('disabled', true);
            return false;
        }

        // Kiểm tra kích thước file
        if (file.size > this.maxFileSizeBytes) {
            this.showError('Kích thước file vượt quá ' + this.maxFileSizeMB + 'MB (tối đa ' + this.formatFileSize(this.maxFileSizeBytes) + ')');
            $submitButton.prop('disabled', true);
            return false;
        }

        return true;
    };

    /**
     * Hiển thị thông báo lỗi
     */
    ExcelImportValidation.prototype.showError = function(message) {
        var $errorContainer = $('#fileError');
        var $fileInput = $('#excelFile');

        $fileInput.addClass('is-invalid');
        $errorContainer.text(message);

        // Cuộn đến vị trí lỗi
        $('html, body').animate({
            scrollTop: $fileInput.offset().top - 100
        }, 500);
    };

    /**
     * Định dạng kích thước file
     */
    ExcelImportValidation.prototype.formatFileSize = function(bytes) {
        if (bytes === 0) return '0 Bytes';
        var k = 1024;
        var sizes = ['Bytes', 'KB', 'MB', 'GB'];
        var i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    };

    return ExcelImportValidation;
}());

// Khởi tạo validation khi tài liệu đã sẵn sàng
$(document).ready(function() {
    // Lấy cấu hình từ data attributes hoặc sử dụng giá trị mặc định
    const options = {
        maxFileSizeMB: parseFloat($('#excelFile').data('max-size') || 10)
    };

    // Khởi tạo validation
    window.excelImportValidation = new ExcelImportValidation(options);
});
