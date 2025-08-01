/**
 * Validation cho form Khách hàng doanh nghiệp
 * @module khachhangdnValidation
 */

// Đảm bảo các thư viện cần thiết đã được tải
const requiredLibs = {
    'jQuery': typeof $ !== 'undefined',
    'jQuery Validation': typeof $.fn.validate !== 'undefined',
    'appValidation': typeof appValidation !== 'undefined',
    'validationHelper': typeof validationHelper !== 'undefined'
};

// Kiểm tra và thông báo nếu thiếu thư viện
const missingLibs = Object.entries(requiredLibs)
    .filter(([_, loaded]) => !loaded)
    .map(([lib]) => lib);

if (missingLibs.length > 0) {
    console.error(`Thiếu các thư viện bắt buộc: ${missingLibs.join(', ')}`);
}

// Biến lưu trữ cấu hình
let config = {};

/**
 * Khởi tạo validation cho form khách hàng doanh nghiệp
 * @param {string|jQuery|HTMLElement} form - Form cần validate
 * @param {Object} [options={}] - Các tùy chọn cấu hình
 * @param {boolean} [options.enableAjaxValidation=true] - Bật kiểm tra CIF trùng lặp qua AJAX
 * @param {Object} [options.customRules={}] - Các rule validation tùy chỉnh
 * @param {Object} [options.messages={}] - Các thông báo lỗi tùy chỉnh
 * @param {Function} [options.onSubmit] - Hàm xử lý khi form hợp lệ
 * @returns {jQuery} Đối tượng jQuery của form
 */
function initKhachHangDNValidation(form, options = {}) {
    // Kiểm tra các thư viện cần thiết
    if (missingLibs.length > 0) {
        console.error('Không thể khởi tạo validation do thiếu thư viện');
        return null;
    }

    const $form = $(form);
    if ($form.length === 0) {
        console.error('Không tìm thấy form cần validate');
        return null;
    }

    // Cấu hình mặc định
    const defaults = {
        enableAjaxValidation: true,
        customRules: {},
        messages: {}
    };

    // Merge options với defaults
    config = { ...defaults, ...options };

    // Xóa validation cũ nếu có
    $form.validate()?.destroy();

    // Thêm các rule tùy chỉnh
    $.validator.addMethod("vietnamesePhoneNumber", function(value, element) {
        return this.optional(element) || appValidation.isValidVietnamesePhoneNumber(value);
    }, "Số điện thoại không hợp lệ");

    $.validator.addMethod("dateAfter", function(value, element, params) {
        if (!value) return true; // Bỏ qua nếu không có giá trị
        const target = $(params).val();
        if (!target) return true; // Bỏ qua nếu không có ngày so sánh
        return appValidation.isDateAfter(value, target);
    }, "Ngày kết thúc phải sau ngày bắt đầu");

    // Thêm các rule tùy chỉnh từ config
    Object.entries(config.customRules).forEach(([name, rule]) => {
        if (typeof rule === 'function') {
            $.validator.addMethod(name, rule);
        } else if (rule.validator && typeof rule.validator === 'function') {
            $.validator.addMethod(name, rule.validator, rule.message || '');
        }
    });

    // Cấu hình validation
    $form.validate({
        rules: getValidationRules(),
        messages: getValidationMessages(),
        errorElement: 'div',
        errorPlacement: handleErrorPlacement,
        highlight: highlightError,
        unhighlight: unhighlightError,
        submitHandler: handleFormSubmit,
        invalidHandler: handleInvalidForm,
        showErrors: showValidationErrors
    });

    // Xử lý sự kiện
    setupFormEvents($form);
    
    return $form;
}

/**
 * Lấy các rule validation
 * @returns {Object} Đối tượng chứa các rule
 */
function getValidationRules() {
    return {
        'SoCif': {
            required: true,
            minlength: 3,
            maxlength: 20,
            remote: config.enableAjaxValidation ? {
                url: $('form').data('check-cif-url') || '/KhachHangDN/CheckCifExists',
                type: 'post',
                data: {
                    id: () => $('#Id').val() || '0',
                    cif: () => $('#SoCif').val()
                }
            } : undefined
        },
        'TenKhachHang': {
            required: true,
            minlength: 3,
            maxlength: 255
        },
        'DienThoai': {
            required: true,
            vietnamesePhoneNumber: true
        },
        'Email': {
            required: true,
            email: true
        },
        'NgayCapGpkd': {
            required: true,
            date: true
        },
        'NgayHhdGpkd': {
            required: true,
            date: true,
            dateAfter: '#NgayCapGpkd'
        },
        'DiaChi': {
            required: true,
            minlength: 5,
            maxlength: 500
        }
    };
}

/**
 * Lấy các thông báo lỗi
 * @returns {Object} Đối tượng chứa các thông báo lỗi
 */
function getValidationMessages() {
    return {
        'SoCif': {
            required: 'Vui lòng nhập số CIF',
            minlength: 'Số CIF phải có ít nhất 3 ký tự',
            maxlength: 'Số CIF không được vượt quá 20 ký tự',
            remote: 'Số CIF đã tồn tại'
        },
        'TenKhachHang': {
            required: 'Vui lòng nhập tên khách hàng',
            minlength: 'Tên khách hàng phải có ít nhất 3 ký tự',
            maxlength: 'Tên khách hàng không được vượt quá 255 ký tự'
        },
        'DienThoai': {
            required: 'Vui lòng nhập số điện thoại',
            vietnamesePhoneNumber: 'Số điện thoại không hợp lệ'
        },
        'Email': {
            required: 'Vui lòng nhập email',
            email: 'Email không hợp lệ'
        },
        'NgayCapGpkd': {
            required: 'Vui lòng nhập ngày cấp GPKD',
            date: 'Ngày không hợp lệ'
        },
        'NgayHhdGpkd': {
            required: 'Vui lòng nhập ngày hết hạn GPKD',
            date: 'Ngày không hợp lệ',
            dateAfter: 'Ngày hết hạn phải sau ngày cấp'
        },
        'DiaChi': {
            required: 'Vui lòng nhập địa chỉ',
            minlength: 'Địa chỉ phải có ít nhất 5 ký tự',
            maxlength: 'Địa chỉ không được vượt quá 500 ký tự'
        },
        ...config.messages // Ghi đè thông báo lỗi tùy chỉnh
    };
}

/**
 * Xử lý vị trí hiển thị thông báo lỗi
 */
function handleErrorPlacement(error, element) {
    error.addClass('invalid-feedback');
    
    if (element.prop('type') === 'radio' || element.prop('type') === 'checkbox') {
        error.insertAfter(element.closest('.form-check'));
    } else {
        error.insertAfter(element);
    }
}

/**
 * Đánh dấu trường lỗi
 */
function highlightError(element, errorClass, validClass) {
    const $element = $(element);
    $element.addClass('is-invalid').removeClass('is-valid');
    
    if ($element.parents('.input-group').length) {
        $element.parents('.input-group').addClass('has-validation');
    }
}

/**
 * Bỏ đánh dấu trường lỗi
 */
function unhighlightError(element, errorClass, validClass) {
    const $element = $(element);
    $element.removeClass('is-invalid').addClass('is-valid');
    
    if ($element.parents('.input-group').length && !$element.hasClass('is-invalid')) {
        $element.parents('.input-group').removeClass('has-validation');
    }
}

/**
 * Xử lý khi form hợp lệ
 */
function handleFormSubmit(form) {
    if (typeof config.onSubmit === 'function') {
        return config.onSubmit(form);
    } else {
        form.submit();
    }
}

/**
 * Xử lý khi form không hợp lệ
 */
function handleInvalidForm(event, validator) {
    validationHelper.scrollToFirstError();
    $(document).trigger('validation:invalid', [validator]);
}

/**
 * Hiển thị thông báo lỗi
 */
function showValidationErrors(errorMap, errorList) {
    this.defaultShowErrors();
    
    $('.is-invalid').each(function() {
        const $this = $(this);
        const errorText = $this.siblings('.invalid-feedback').text();
        
        if (errorText && !$this.attr('title')) {
            $this.attr('title', errorText)
                 .tooltip({
                     placement: 'top',
                     trigger: 'hover focus',
                     container: 'body'
                 });
        }
    });
}

/**
 * Khởi tạo sự kiện và xử lý form
 * @param {jQuery} $form - Form cần xử lý
 */
function setupFormEvents($form) {
    // Xử lý sự kiện khi thay đổi giá trị của các trường ngày
    $form.on('change', '.date-field', function() {
        $form.validate().element(this);
    });

    // Xử lý sự kiện khi nhập số điện thoại
    $form.on('blur', 'input[type="tel"]', function() {
        const $this = $(this);
        const formatted = validationHelper.formatPhoneNumber($this.val());
        if (formatted) {
            $this.val(formatted);
        }
    });

    // Xử lý sự kiện khi submit form
    $form.on('submit', function(e) {
        e.preventDefault();
        
        if ($form.valid()) {
            if (typeof config.onSubmit === 'function') {
                config.onSubmit(this);
            } else {
                this.submit();
            }
        } else {
            validationHelper.scrollToFirstError();
        }
    });
}

// Tự động khởi tạo validation khi DOM sẵn sàng
$(document).ready(function() {
    $('form.khachhangdn-form').each(function() {
        initKhachHangDNValidation(this);
    });
});

// Xuất các hàm để sử dụng trong các module khác
window.khachHangDNValidation = {
    init: initKhachHangDNValidation
};
