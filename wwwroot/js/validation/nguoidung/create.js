// Sử dụng IIFE để tránh xung đột biến toàn cục
console.log('=== create.js loaded ===');
(function() {
    'use strict';

    // --- Hàm hiển thị lỗi cho trường input ---
    function showFieldError(fieldId, message) {
        const $field = $(`#${fieldId}`);
        $field.addClass('is-invalid').removeClass('is-valid');
        let $feedback = $field.next('.invalid-feedback');
        if (!$feedback.length) {
            $field.after('<div class="invalid-feedback"></div>');
            $feedback = $field.next('.invalid-feedback');
        }
        $feedback.text(message).show();
    }

    // --- Hàm xóa lỗi cho trường input ---
    function clearFieldError(fieldId) {
        const $field = $(`#${fieldId}`);
        $field.removeClass('is-invalid is-valid');
        let $feedback = $field.next('.invalid-feedback');
        if ($feedback.length) $feedback.text('').hide();
    }

    // --- Hàm hiển thị trạng thái thành công cho trường input ---
    function showFieldSuccess(fieldId) {
        const $field = $(`#${fieldId}`);
        $field.addClass('is-valid').removeClass('is-invalid');
        let $feedback = $field.next('.invalid-feedback');
        if ($feedback.length) $feedback.hide();
    }

    // --- Hàm xóa tất cả lỗi trong form ---
    function clearAllErrors(formId) {
        const $form = $(`#${formId}`);
        $form.find('.is-invalid, .is-valid').removeClass('is-invalid is-valid');
        $form.find('.invalid-feedback').text('').hide();
        $('#validationSummary').empty().hide();
    }

    // --- Hàm validate toàn bộ form ---
    function isFormValid() {
        debugger;
        let isValid = false;// true;
        // Validate từng trường bắt buộc
        const requiredFields = [
            { id: 'UserName', msg: 'Vui lòng nhập Tên đăng nhập.' },
            { id: 'TenUser', msg: 'Vui lòng nhập Họ và tên.' },
            { id: 'Password', msg: 'Vui lòng nhập Mật khẩu.' },
            { id: 'ConfirmPassword', msg: 'Vui lòng nhập Xác nhận mật khẩu.' },
            { id: 'MaPhong', msg: 'Vui lòng chọn Phòng ban.' },
            { id: 'TrangThai', msg: 'Vui lòng chọn Trạng thái.' }
        ];
        requiredFields.forEach(f => {
            const $field = $(`#${f.id}`);
            const val = $field.val();
            if (!val || !val.toString().trim()) {
                showFieldError(f.id, f.msg);
                isValid = false;
            } else {
                clearFieldError(f.id);
                showFieldSuccess(f.id);
            }
        });
        // Kiểm tra mật khẩu xác nhận
        const password = $('#Password').val();
        const confirm = $('#ConfirmPassword').val();
        if (password && confirm && password !== confirm) {
            showFieldError('ConfirmPassword', 'Xác nhận mật khẩu không khớp.');
            isValid = false;
        }
        // Có thể bổ sung thêm các rule khác nếu cần
        isValid = true;
        return isValid;
    }

    // --- Hàm hiển thị lỗi tổng hợp ---
    function showError(message) {
        const $alert = $('<div class="alert alert-danger alert-dismissible fade show" role="alert">' +
            message +
            '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>' +
            '</div>');
        $('#validationSummary').empty().append($alert).show();
    }

    // --- Xử lý submit form ---
    $(document).on('submit', '#createUserForm', function(e) {
        debugger;
        e.preventDefault();
        e.stopPropagation();
        clearAllErrors('createUserForm');
        const valid = isFormValid();
        if (!valid) {
            showError('Vui lòng sửa các lỗi bên dưới.');
            // Focus vào trường đầu tiên bị lỗi
            const $firstInvalid = $('#createUserForm .is-invalid').first();
            if ($firstInvalid.length) {
                $('html, body').animate({ scrollTop: $firstInvalid.offset().top - 100 }, 500);
                setTimeout(() => $firstInvalid.focus(), 500);
            }
            return false;
        }
        // Nếu hợp lệ, submit thật
        this.submit();
        return false;
    });

    
    // Khai báo biến toàn cục để lưu hàm khởi tạo
    window.initNguoiDungCreate = function() {
        // Bỏ mọi handler submit cũ để tránh xung đột
        $('#createUserForm').off('submit');
        // Gán lại handler submit mới
        $('#createUserForm').on('submit', function(e) {
            debugger;
            e.preventDefault();
            e.stopPropagation();
            clearAllErrors('createUserForm');
            
            // Kiểm tra form có hợp lệ không
            const valid = isFormValid();
            if (!valid) {
                showError('Vui lòng sửa các lỗi bên dưới.');
                const $firstInvalid = $('#createUserForm .is-invalid').first();
                if ($firstInvalid.length) {
                    $('html, body').animate({ scrollTop: $firstInvalid.offset().top - 100 }, 500);
                    setTimeout(() => $firstInvalid.focus(), 500);
                }
                return false; // Chặn submit nếu có lỗi
            }
            
            // Nếu hợp lệ, submit thật
            this.submit();
            return false; // Ngăn chặn submit mặc định
        });
    };
    
    // Khởi tạo debug
    const debug = window.appDebug && window.appDebug.validation || {
        log: console.log.bind(console, '[Validation]'),
        error: console.error.bind(console, '[Validation Error]')
    };
    
    // Các biến cục bộ
    let isUserNameAvailable = false;
    let isCheckingUserName = false;
    let checkUserTimeout;

    // Hàm khởi tạo form tạo người dùng
    window.initNguoiDungCreate = function() {
        const formId = 'createUserForm';
        const $form = $(`#${formId}`);
        const $userNameField = $('#UserName');
        const $checkUserNameBtn = $('#checkUserNameBtn');
        const $userNameResult = $('#userNameCheckResult');
        const $userNameFeedback = $('#userNameCheckFeedback');
        
        if (!$form.length) {
            console.error('Không tìm thấy form có ID:', formId);
            return;
        }
        
        // Thêm class needs-validation nếu chưa có
        if (!$form.hasClass('needs-validation')) {
            $form.addClass('needs-validation');
        }
        
        // Thiết lập các sự kiện
        setupEventHandlers();
        setupFormValidation();
        
        // Hàm thiết lập các sự kiện
        function setupEventHandlers() {
            // Xử lý sự kiện input của username
            $userNameField.on('input', handleUsernameInput);
            
            // Xử lý sự kiện click nút kiểm tra
            $checkUserNameBtn.on('click', handleCheckUsernameClick);
            
            // Xử lý sự kiện submit form
            $form.on('submit', handleFormSubmit);
        }
        
        // Xử lý khi người dùng nhập tên đăng nhập
        function handleUsernameInput() {
            const userName = $(this).val().trim();
            
            clearTimeout(checkUserTimeout);
            isUserNameAvailable = false;
            
            // Reset UI
            $userNameResult.text('').removeClass('text-success text-danger');
            $userNameFeedback.text('').removeClass('invalid-feedback d-block');
            $userNameField.removeClass('is-invalid is-valid');
            
            // Kích hoạt nút kiểm tra nếu đủ độ dài
            $checkUserNameBtn.prop('disabled', userName.length < 3);

            if (userName.length < 3) {
                $userNameField.removeClass('is-valid is-invalid');
                return;
            }

            // Tự động kiểm tra sau khi ngừng gõ 500ms
            checkUserTimeout = setTimeout(async () => {
                await checkUserName(userName, $userNameField, $userNameResult, $userNameFeedback);
            }, 500);
        }
        
        // Xử lý khi click nút kiểm tra tên đăng nhập
        async function handleCheckUsernameClick() {
            const userName = $userNameField.val().trim();
            if (userName.length < 3) return;
            
            await checkUserName(userName, $userNameField, $userNameResult, $userNameFeedback);
        }
        
        // Thiết lập validation cho form
        function setupFormValidation() {
            // Các rule validation - Đồng bộ với CreateUserViewModel
            const rules = {
                'UserName': {
                    required: 'Tên đăng nhập là bắt buộc.',
                    maxLength: {
                        value: 256,
                        message: 'Tên đăng nhập không được vượt quá 256 ký tự.'
                    },
                    minLength: {
                        value: 3,
                        message: 'Tên đăng nhập phải dài tối thiểu 3 ký tự.'
                    }
                },
                /*
                'Email': {
                    email: 'Địa chỉ email không hợp lệ.',
                    maxLength: 256,
                    message: 'Email không được vượt quá 256 ký tự.'
                },
                */
                'TenUser': {
                    required: 'Họ và Tên người dùng là bắt buộc.',
                    maxLength: 50,
                    message: 'Họ và Tên không được vượt quá 50 ký tự.'
                },
                'Password': {
                    required: 'Mật khẩu là bắt buộc.',
                    minLength: {
                        value: 6,
                        message: 'Mật khẩu phải dài tối thiểu 6 ký tự.'
                    },
                    maxLength: {
                        value: 100,
                        message: 'Mật khẩu không được vượt quá 100 ký tự.'
                    }
                },
                'ConfirmPassword': {
                    required: 'Vui lòng xác nhận mật khẩu.',
                    matches: 'Password',
                    message: 'Mật khẩu xác nhận không khớp.'
                }
            };
            
            // Khởi tạo validation
            if (typeof initFormValidation === 'function') {
                initFormValidation(formId, rules);
            }
        }
        
        // Xử lý khi submit form
        function handleFormSubmit(e) {
            // Ngăn chặn submit mặc định
            e.preventDefault();
            e.stopPropagation();
            
            // Xóa các thông báo lỗi cũ
            if (window.utils && typeof window.utils.clearAllErrors === 'function') {
                window.utils.clearAllErrors(formId);
            } else {
                $form.find('.is-invalid').removeClass('is-invalid');
                $form.find('.invalid-feedback').remove();
                $('#validationSummary').empty().hide();
            }
            
            // Thêm class was-validated để hiển thị validation message
            $form.addClass('was-validated');
            
            // Kiểm tra form có hợp lệ không
            const isValid = isFormValid.call(this);
            
            if (!isValid) {
                // Cuộn đến trường đầu tiên bị lỗi
                const $firstInvalid = $form.find('.is-invalid').first();
                if ($firstInvalid.length) {
                    $('html, body').animate({
                        scrollTop: $firstInvalid.offset().top - 100
                    }, 500);
                    
                    // Focus vào trường đầu tiên bị lỗi
                    setTimeout(() => {
                        $firstInvalid.focus();
                    }, 500);
                }
                
                // Hiển thị thông báo lỗi tổng hợp nếu có
                if (window.utils && typeof window.utils.showError === 'function') {
                    window.utils.showError('Vui lòng kiểm tra lại thông tin đã nhập');
                }
                
                return false; // KHÔNG submit nếu chưa hợp lệ
            }
            
            // Nếu hợp lệ, thực hiện submit form
            try {
                // Vô hiệu hóa nút submit để tránh submit nhiều lần
                const $submitButton = $form.find('button[type="submit"]');
                const originalText = $submitButton.html();
                $submitButton.prop('disabled', true)
                    .html('<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Đang xử lý...');
                
                // Gửi form
                this.submit();
                
            } catch (error) {
                console.error('Lỗi khi submit form:', error);
                if (window.utils && typeof window.utils.showError === 'function') {
                    window.utils.showError('Có lỗi xảy ra khi gửi dữ liệu. Vui lòng thử lại.');
                }
                $form.find('button[type="submit"]').prop('disabled', false);
            }
            
            return false; // Ngăn không cho form submit lại
        }
        
        // Kiểm tra form có hợp lệ không
        function isFormValid() {
            let isValid = true;
            const userName = $userNameField.val().trim();
            
            // 1. Kiểm tra nếu đang trong quá trình kiểm tra username
            if (isCheckingUserName) {
                if (utils.showError) {
                    utils.showError('Vui lòng chờ kiểm tra tên đăng nhập xong!');
                } else {
                    console.error('Vui lòng chờ kiểm tra tên đăng nhập xong!');
                }
                $userNameField.focus();
                return false;
            }
            
            // 2. Kiểm tra form hợp lệ bằng HTML5 validation
            if (this.checkValidity && !this.checkValidity()) {
                isValid = false;
            }
            
            // 3. Kiểm tra các trường bắt buộc
            const requiredFields = $form.find('[required]');
            requiredFields.each(function() {
                const $field = $(this);
                const value = $field.val();
                const fieldName = $field.attr('name') || 'này';
                
                if (!value || !value.toString().trim()) {
                    if (utils.showFieldError) {
                        utils.showFieldError($field.attr('id'), `Vui lòng nhập thông tin cho trường ${fieldName}`);
                    } else {
                        $field.addClass('is-invalid').removeClass('is-valid');
                        let $feedback = $field.next('.invalid-feedback');
                        if (!$feedback.length) {
                            $field.after(`<div class="invalid-feedback">Vui lòng nhập thông tin cho trường ${fieldName}</div>`);
                            $feedback = $field.next('.invalid-feedback');
                        }
                        $feedback.show();
                    }
                    isValid = false;
                } else {
                    if (utils.clearFieldError) {
                        utils.clearFieldError($field.attr('id'));
                    } else {
                        $field.removeClass('is-invalid').addClass('is-valid');
                        let $feedback = $field.next('.invalid-feedback');
                        if ($feedback.length) $feedback.hide();
                    }
                }
            });
            
            // 4. Kiểm tra username đã được kiểm tra chưa
            if (userName.length >= 3 && !isUserNameAvailable) {
                if (utils.showFieldError) {
                    utils.showFieldError('UserName', 'Vui lòng kiểm tra tên đăng nhập trước khi tiếp tục');
                } else {
                    $userNameField.addClass('is-invalid');
                }
                isValid = false;
            }
            
            // Không có code nào sau return!
            return isValid;
        }
    }
    
    // Hàm kiểm tra tên đăng nhập tồn tại qua AJAX
    async function checkUserNameExists(userName, url) {
        try {
            const response = await fetch(url + '?userName=' + encodeURIComponent(userName), {
                method: 'GET',
                headers: { 'Accept': 'application/json' }
            });
            if (!response.ok) throw new Error('Lỗi mạng');
            return await response.json();
        } catch (error) {
            return { error: true, message: error.message };
        }
    }

    // Hàm kiểm tra tên đăng nhập
    async function checkUserName(userName, $userNameField, $userNameResult, $userNameFeedback) {
        isCheckingUserName = true;
        $userNameResult.text('Đang kiểm tra...').addClass('text-info');
        $userNameField.removeClass('is-invalid is-valid');
        
        try {
            const response = await checkUserNameExists(
                userName,
                '/NguoiSuDung/CheckUserNameExists'
            );

            if (response.error) {
                console.error('Lỗi từ server:', response.error);
                showUserNameError($userNameField, $userNameResult, $userNameFeedback, 
                    response.message || 'Lỗi khi kiểm tra tên đăng nhập');
                isUserNameAvailable = false;
                return;
            }

            if (response.isAvailable) {
                showUserNameSuccess($userNameField, $userNameResult, 
                    response.message || 'Tên đăng nhập có thể sử dụng');
                isUserNameAvailable = true;
            } else {
                showUserNameError($userNameField, $userNameResult, $userNameFeedback, 
                    response.message || 'Tên đăng nhập đã tồn tại');
                isUserNameAvailable = false;
            }
        } catch (error) {
            console.error('Lỗi khi kiểm tra tên đăng nhập:', error);
            showUserNameError($userNameField, $userNameResult, $userNameFeedback, 
                'Lỗi khi kiểm tra tên đăng nhập');
            isUserNameAvailable = false;
        } finally {
            isCheckingUserName = false;
        }
    }
    
    // Sử dụng hàm từ validation-common.js
    const utils = window.utils || {};
    
    // Hiển thị thông báo lỗi cho username
    function showUserNameError($field, $result, $feedback, message) {
        $result.text('').removeClass('text-success text-info');
        if (typeof utils.showFieldError === 'function') {
            utils.showFieldError($field.attr('id'), message);
        } else {
            $field.addClass('is-invalid');
            $feedback.text(message).addClass('d-block');
        }
    }
    
    // Hiển thị thông báo thành công cho username
    function showUserNameSuccess($field, $result, message) {
        $result.text(message)
            .removeClass('text-danger text-info')
            .addClass('text-success');
        $field.addClass('is-valid');
        $field.removeClass('is-invalid');
        
        if (typeof utils.clearFieldError === 'function') {
            utils.clearFieldError($field.attr('id'));
        } else {
            $field.removeClass('is-invalid');
            $field.next('.invalid-feedback').remove();
        }
        
        $('#userNameCheckFeedback').text('').removeClass('d-block');
    }
    
    // Lắng nghe sự kiện khi script được load xong
    document.addEventListener('validation:loaded', function(event) {
        if (event.detail && event.detail.action.toLowerCase() === 'create') {
            try {
                debug.log('Initializing NguoiDung Create validation');
                initNguoiDungCreate();
            } catch (e) {
                debug.error('Failed to initialize NguoiDung Create', e);
            }
        }
    });
    
    // Khởi tạo validation khi vào đúng trang
    $(document).ready(function() {
        try {
            const currentPath = window.location.pathname.toLowerCase();
            if (currentPath.includes('/nguoidung/create') || currentPath.includes('/nguoisudung/create')) {
                if (typeof window.initNguoiDungCreate === 'function') {
                    window.initNguoiDungCreate();
                }
            }
        } catch (error) {
            console.error('Lỗi khi khởi tạo form tạo người dùng:', error);
        }
    });
})();
