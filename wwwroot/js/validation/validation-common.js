/**
 * Các hàm tiện ích chung cho validation và hiển thị thông báo
 * Hỗ trợ cả SweetAlert2 và alert thông thường
 */
const utils = {
    // Kiểm tra xem SweetAlert2 có sẵn sàng không
    get isSwalAvailable() {
        return typeof Swal !== 'undefined' && typeof Swal.fire === 'function';
    },

    // Hiển thị thông báo lỗi
    showError: function(message, container = '#validationSummary') {
        if (this.isSwalAvailable) {
            return window.showError(message);
        }

        const $container = $(container);
        const alert = `
            <div class="alert alert-danger alert-dismissible fade show" role="alert">
                <i class="ti ti-alert-circle me-2"></i>
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>`;
        $container.html(alert).show();
    },

    // Hiển thị thông báo thành công
    showSuccess: function(message, container = '#validationSummary') {
        if (this.isSwalAvailable) {
            return window.showSuccess(message);
        }

        const $container = $(container);
        const alert = `
            <div class="alert alert-success alert-dismissible fade show" role="alert">
                <i class="ti ti-circle-check me-2"></i>
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>`;
        $container.html(alert).show();
    },

    // Hiển thị thông báo lỗi cho trường cụ thể
    showFieldError: function(fieldId, message) {
        const $field = $(`#${fieldId}`);
        if ($field.length === 0) return;
        $field.addClass('is-invalid').removeClass('is-valid');
        let $feedback = $field.next('.invalid-feedback');
        if (!$feedback.length) {
            $field.after('<div class="invalid-feedback"></div>');
            $feedback = $field.next('.invalid-feedback');
        }
        $feedback.text(message).show();
    },

    // Xóa thông báo lỗi của trường
    clearFieldError: function(fieldId) {
        const $field = $(`#${fieldId}`);
        $field.removeClass('is-invalid is-valid');
        let $feedback = $field.next('.invalid-feedback');
        if ($feedback.length) {
            $feedback.text('').hide();
        }
    },

    // Xóa tất cả thông báo lỗi
    clearAllErrors: function(formId) {
        const $form = formId ? $(`#${formId}`) : $('form');
        $form.find('.is-invalid').removeClass('is-invalid');
        $form.find('.invalid-feedback').remove();
        $('#validationSummary').empty().hide();
    },

    // Kiểm tra email hợp lệ
    isValidEmail: function(email) {
        const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return re.test(String(email).toLowerCase());
    },

    // Kiểm tra số điện thoại hợp lệ
    isValidPhone: function(phone) {
        const re = /^[0-9+\-\s()]*$/;
        return re.test(phone);
    },

    // Kiểm tra mật khẩu mạnh
    isStrongPassword: function(password) {
        // Ít nhất 8 ký tự, 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt
        const re = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;
        return re.test(password);
    },

    // Định dạng số
    formatNumber: function(num) {
        return new Intl.NumberFormat('vi-VN').format(num);
    },

    // Định dạng ngày tháng
    formatDate: function(date, format = 'dd/MM/yyyy') {
        if (!date) return '';

        const d = new Date(date);
        if (isNaN(d.getTime())) return '';

        const day = d.getDate().toString().padStart(2, '0');
        const month = (d.getMonth() + 1).toString().padStart(2, '0');
        const year = d.getFullYear();
        const hours = d.getHours().toString().padStart(2, '0');
        const minutes = d.getMinutes().toString().padStart(2, '0');
        const seconds = d.getSeconds().toString().padStart(2, '0');

        return format
            .replace('dd', day)
            .replace('MM', month)
            .replace('yyyy', year)
            .replace('HH', hours)
            .replace('mm', minutes)
            .replace('ss', seconds);
    },

    // Kiểm tra username tồn tại
    checkUserNameExists: async function(userName, url) {
        try {
            const response = await $.get(url, { userName: userName });
            // Xử lý cả PascalCase và camelCase
            const result = {
                exists: response.exists || response.Exists || false,
                isAvailable: response.isAvailable || response.IsAvailable || false,
                message: response.message || response.Message || '',
                error: response.error || response.Error
            };

            console.log('Check username response:', result);
            return result;
        } catch (error) {
            console.error('Lỗi khi kiểm tra tên đăng nhập:', error);
            const errorData = error.responseJSON || {};
            return {
                exists: true,
                isAvailable: false,
                message: errorData.message || errorData.Message || 'Lỗi khi kiểm tra tên đăng nhập. Vui lòng thử lại.',
                error: errorData.error || errorData.Error || error.statusText
            };
        }
    },

    // Khởi tạo datepicker
    initDatePicker: function(selector, options = {}) {
        const defaults = {
            format: 'dd/mm/yyyy',
            autoclose: true,
            language: 'vi',
            todayHighlight: true,
            orientation: 'bottom auto'
        };

        $(selector).datepicker({ ...defaults, ...options });
    },

    // Khởi tạo select2
    initSelect2: function(selector, options = {}) {
        const defaults = {
            theme: 'bootstrap-5',
            width: '100%',
            placeholder: 'Chọn một tùy chọn',
            allowClear: true,
            language: 'vi'
        };

        $(selector).select2({ ...defaults, ...options });
    },

    // Khởi tạo DataTable
    initDataTable: function(selector, options = {}) {
        const defaults = {
            // language: {
            //     url: '//cdn.datatables.net/plug-ins/1.10.25/i18n/Vietnamese.json'
            // },
            responsive: true,
            autoWidth: false,
            pageLength: 10,
            lengthMenu: [5, 10, 25, 50, 100]
        };

        return $(selector).DataTable({ ...defaults, ...options });
    },

    // Validate form với các rule tùy chỉnh
    validateForm: function(formId, rules) {
        let isValid = true;

        // Reset lỗi
        utils.clearAllErrors(formId);

        // Kiểm tra từng rule
        for (const [fieldId, fieldRules] of Object.entries(rules)) {
            const $field = $(`#${fieldId}`);
            if ($field.length === 0) continue;

            const value = $field.val()?.trim() || '';
            const isCheckboxOrRadio = $field.is(':checkbox, :radio');
            const isChecked = isCheckboxOrRadio ? $field.is(':checked') : false;

            // Kiểm tra required
            if (fieldRules.required && ((!isCheckboxOrRadio && !value) || (isCheckboxOrRadio && !isChecked))) {
                utils.showFieldError(fieldId, fieldRules.required);
                isValid = false;
                continue;
            }

            // Kiểm tra email
            if (fieldRules.email && value && !utils.isValidEmail(value)) {
                utils.showFieldError(fieldId, fieldRules.email);
                isValid = false;
                continue;
            }

            // Kiểm tra độ dài tối thiểu
            if (fieldRules.minLength && value.length < fieldRules.minLength.value) {
                utils.showFieldError(fieldId, fieldRules.minLength.message);
                isValid = false;
                continue;
            }

            // Kiểm tra khớp mật khẩu
            if (fieldRules.matches) {
                const $matchField = $(`#${fieldRules.matches.fieldId}`);
                if ($matchField.length && value !== $matchField.val()) {
                    utils.showFieldError(fieldId, fieldRules.matches.message);
                    isValid = false;
                }
            }

            // Kiểm tra regex tùy chỉnh
            if (fieldRules.pattern && value) {
                const regex = new RegExp(fieldRules.pattern.regex);
                if (!regex.test(value)) {
                    utils.showFieldError(fieldId, fieldRules.pattern.message);
                    isValid = false;
                }
            }
        }

        return isValid;
    },

    // Thiết lập validation cho form
    setupFormValidation: function(formId, rules, onSubmit) {
        const $form = $(`#${formId}`);
        if ($form.length === 0) return;

        // Xử lý sự kiện input để xóa thông báo lỗi
        $form.on('input change', 'input, select, textarea', function() {
            const fieldId = $(this).attr('id');
            if (fieldId) {
                utils.clearFieldError(fieldId);
            }
        });

        // Xử lý submit form
        $form.on('submit', function(e) {
            e.preventDefault();

            // Xóa tất cả thông báo lỗi cũ
            utils.clearAllErrors(formId);

            // Validate form
            const isValid = utils.validateForm(formId, rules);

            if (isValid && typeof onSubmit === 'function') {
                onSubmit($form);
            }

            return false;
        });
    }
};

// Xuất các hàm ra global scope
window.showError = utils.showError;
window.showSuccess = utils.showSuccess;
window.setupFormValidation = utils.setupFormValidation;
window.validateForm = utils.validateForm;
window.showFieldError = utils.showFieldError;
window.clearFieldError = utils.clearFieldError;
window.clearAllErrors = utils.clearAllErrors;
window.isValidEmail = utils.isValidEmail;
window.isValidPhone = utils.isValidPhone;
window.isStrongPassword = utils.isStrongPassword;
window.formatNumber = utils.formatNumber;
window.formatDate = utils.formatDate;
window.checkUserNameExists = utils.checkUserNameExists;
