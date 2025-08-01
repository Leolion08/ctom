/**
 * Hiển thị thông báo động
 * @param {string} message - Nội dung thông báo
 * @param {string} [type='success'] - Loại thông báo (success, danger, warning, info)
 * @param {string} [placeholderId='notificationPlaceholder'] - ID của phần tử chứa thông báo
 * @param {number} [autoHide=false] - Thời gian tự động ẩn (ms), 0 để không tự động ẩn
 * @returns {HTMLElement} - Phần tử thông báo đã tạo
 */
function showAppNotification(message, type = 'success', placeholderId = 'notificationPlaceholder', autoHide = false) {  // Đổi default thành false
    const placeholder = document.getElementById(placeholderId);
    if (!placeholder) {
        console.warn(`Không tìm thấy phần tử với ID '${placeholderId}' để hiển thị thông báo.`);
        alert(message);
        return null;
    }

    // Xóa các thông báo cũ để tránh hiển thị nhiều thông báo đồng thời
    placeholder.querySelectorAll('.alert').forEach(el => el.remove());
    // Tạo thông báo mới
    const notification = document.createElement('div');
    notification.className = `alert alert-${type} alert-dismissible bg-${type}-lt`; // Bỏ 'fade show'
    notification.role = 'alert';
    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    placeholder.appendChild(notification);

    // Chỉ thêm sự kiện auto-hide nếu được chỉ định rõ
    if (autoHide && autoHide > 0) {
        setTimeout(() => {
            notification.remove();
        }, autoHide);
    }

    return notification;
}

/**
 * Dọn dẹp modal backdrop và khôi phục trạng thái body
 * @param {boolean} [force=false] - Bắt buộc dọn dẹp ngay cả khi không tìm thấy backdrop
 */
function cleanupModalBackdrop(force = false) {
    const backdrops = document.querySelectorAll('.modal-backdrop');
    const modals = document.querySelectorAll('.modal');

    // Xóa tất cả backdrops
    backdrops.forEach(backdrop => backdrop.remove());

    // Đóng tất cả modals đang mở
    modals.forEach(modal => {
        const modalInstance = bootstrap.Modal.getInstance(modal);
        if (modalInstance) {
            modalInstance.hide();
        }
    });

    // Khôi phục trạng thái body
    const body = document.body;
    body.classList.remove('modal-open');
    body.style.overflow = '';
    body.style.paddingRight = '';

    // Xóa các class modal-open còn sót lại
    document.documentElement.classList.remove('modal-open');

    // Xóa padding-right nếu có
    const scrollbarWidth = window.innerWidth - document.documentElement.clientWidth;
    if (scrollbarWidth > 0) {
        body.style.paddingRight = `${scrollbarWidth}px`;
    }
}

// ==================== THEME MANAGEMENT ====================

/**
 * Khởi tạo chức năng chuyển đổi giao diện sáng/tối
 */
function initializeThemeSwitcher() {
    // Lấy các nút chuyển đổi theme
    const darkButton = document.querySelector('.hide-theme-dark');
    const lightButton = document.querySelector('.hide-theme-light');

    if (!darkButton || !lightButton) return;

    /**
     * Chuyển đổi theme
     * @param {string} theme - Tên theme ('light' hoặc 'dark')
     */
    const switchTheme = (theme) => {
        // Áp dụng theme cho cả html và body để đảm bảo nhất quán
        const root = document.documentElement;
        const body = document.body;

        // Cập nhật thuộc tính data-bs-theme
        root.setAttribute('data-bs-theme', theme);
        body.setAttribute('data-bs-theme', theme);

        // Cập nhật biến CSS tùy chỉnh nếu cần
        root.style.setProperty('--bs-body-color-scheme', theme);

        // Cập nhật hiển thị nút chuyển đổi
        if (theme === 'dark') {
            darkButton.style.display = 'none';
            lightButton.style.display = 'block';
        } else {
            darkButton.style.display = 'block';
            lightButton.style.display = 'none';
        }

        // Lưu tùy chọn vào localStorage
        try {
            localStorage.setItem('theme', theme);

            // Đồng bộ với hệ thống (nếu cần)
            if (theme === 'dark') {
                document.documentElement.classList.add('dark-mode');
            } else {
                document.documentElement.classList.remove('dark-mode');
            }
        } catch (e) {
            console.error('Không thể lưu tùy chọn theme vào localStorage:', e);
        }

        // Kích hoạt sự kiện tùy chỉnh khi đổi theme
        document.dispatchEvent(new CustomEvent('themeChanged', { detail: { theme } }));
    };

    // Kiểm tra theme ưa thích của hệ thống
    const prefersDarkScheme = window.matchMedia('(prefers-color-scheme: dark)');

    // Lắng nghe sự thay đổi theme hệ thống
    prefersDarkScheme.addEventListener('change', (e) => {
        const newTheme = e.matches ? 'dark' : 'light';
        switchTheme(newTheme);
    });

    // Xử lý sự kiện click cho nút chuyển đổi
    darkButton.addEventListener('click', (e) => {
        e.preventDefault();
        switchTheme('dark');
    });

    lightButton.addEventListener('click', (e) => {
        e.preventDefault();
        switchTheme('light');
    });

    // Khôi phục theme đã lưu hoặc sử dụng theme hệ thống
    try {
        const savedTheme = localStorage.getItem('theme');
        const systemTheme = prefersDarkScheme.matches ? 'dark' : 'light';
        const initialTheme = savedTheme || systemTheme;

        switchTheme(initialTheme);
    } catch (e) {
        console.error('Không thể khôi phục theme:', e);
        switchTheme('light'); // Mặc định về light theme nếu có lỗi
    }
}

// Khởi tạo theme khi DOM đã tải xong
document.addEventListener('DOMContentLoaded', initializeThemeSwitcher);

// ==================== COMMON UTILITIES ====================

/**
 * Tạo một ID ngẫu nhiên
 * @param {number} [length=8] - Độ dài của ID
 * @returns {string} - ID ngẫu nhiên
 */
function generateId(length = 8) {
    return Array.from(
        { length },
        () => 'abcdefghijklmnopqrstuvwxyz0123456789'[Math.floor(Math.random() * 36)]
    ).join('');
}

/**
 * Định dạng số tiền
 * @param {number} amount - Số tiền cần định dạng
 * @param {string} [currency='VND'] - Đơn vị tiền tệ
 * @returns {string} - Chuỗi đã định dạng
 */
function formatCurrency(amount, currency = 'VND') {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: currency,
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    }).format(amount);
}

// ==================== MODAL HELPERS ====================

/**
 * Mở một modal bằng ID
 * @param {string} modalId - ID của modal cần mở
 * @param {Object} [options={}] - Các tùy chọn bổ sung
 */
function openModal(modalId, options = {}) {
    const modalElement = document.getElementById(modalId);
    if (!modalElement) {
        console.error(`Không tìm thấy modal với ID: ${modalId}`);
        return null;
    }

    const modal = new bootstrap.Modal(modalElement, options);
    modal.show();

    // Trả về instance của modal để có thể tương tác thêm
    return modal;
}

/**
 * Đóng modal đang mở
 * @param {string} [modalId] - ID của modal cần đóng, nếu không chỉ định sẽ đóng tất cả
 */
function closeModal(modalId) {
    if (modalId) {
        const modalElement = document.getElementById(modalId);
        if (modalElement) {
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) {
                modal.hide();
            }
        }
    } else {
        // Đóng tất cả modals
        const modals = document.querySelectorAll('.modal.show');
        modals.forEach(modal => {
            const modalInstance = bootstrap.Modal.getInstance(modal);
            if (modalInstance) {
                modalInstance.hide();
            }
        });
    }
}

// ==================== FORM HELPERS ====================

/**
 * Vô hiệu hóa tất cả các nút submit trong form
 * @param {HTMLFormElement} form - Form cần xử lý
 * @param {boolean} disable - Vô hiệu hóa hoặc kích hoạt
 */
function toggleFormButtons(form, disable = true) {
    const buttons = form.querySelectorAll('button[type="submit"], input[type="submit"]');
    buttons.forEach(button => {
        button.disabled = disable;

        // Thêm hiệu ứng loading nếu vô hiệu hóa
        if (disable) {
            const originalHtml = button.innerHTML;
            button.dataset.originalHtml = originalHtml;
            button.innerHTML = `
                <span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
                ${button.textContent.trim()}
            `;
        } else if (button.dataset.originalHtml) {
            // Khôi phục nội dung gốc nếu có
            button.innerHTML = button.dataset.originalHtml;
            delete button.dataset.originalHtml;
        }
    });
}

/**
 * Xóa tất cả thông báo lỗi trong form
 * @param {HTMLFormElement} form - Form cần xử lý
 */
function clearFormErrors(form) {
    // Xóa các thông báo lỗi của validation
    form.querySelectorAll('.is-invalid').forEach(el => {
        el.classList.remove('is-invalid');
    });

    // Xóa các thông báo lỗi tùy chỉnh
    form.querySelectorAll('.invalid-feedback').forEach(el => {
        el.remove();
    });

    // Xóa thông báo lỗi tổng hợp nếu có
    const summary = form.querySelector('.validation-summary-errors');
    if (summary) {
        summary.remove();
    }
}

// ==================== INITIALIZATION ====================

/**
 * Khởi tạo các thành phần giao diện
 */
function initializeUI() {
    // Khởi tạo tooltip
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(tooltipTriggerEl => {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Khởi tạo popover
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.forEach(popoverTriggerEl => {
        return new bootstrap.Popover(popoverTriggerEl);
    });

    // Tự động ẩn các thông báo sau 5 giây
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(alert => {
        const closeButton = alert.querySelector('.btn-close');
        if (closeButton) {
            setTimeout(() => {
                const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
                if (bsAlert) {
                    bsAlert.close();
                }
            }, 5000);
        }
    });
}

/**
 * Đặt trạng thái active cho menu chính dựa trên controller hiện tại.
 */
function initializeActiveMenu() {
    const currentController = document.body.dataset.controller;
    if (!currentController) return;

    const controllerNameLower = currentController.toLowerCase();

    // Tìm liên kết khớp với controller hiện tại
    // Duyệt qua cả nav-link (menu chính) và dropdown-item (menu con)
    const navLinks = document.querySelectorAll('#navbar-menu .nav-link, #navbar-menu .dropdown-item');
    let activeLink = null;

    navLinks.forEach(link => {
        const href = link.getAttribute('href');
        if (!href || href === '#') return;

        // Phân tích URL để lấy các phần của đường dẫn
        try {
            const pathSegments = new URL(href, window.location.origin).pathname.split('/').filter(Boolean);
            
            // Kiểm tra xem có phần nào trong đường dẫn khớp với tên controller không
            if (pathSegments.some(segment => segment.toLowerCase() === controllerNameLower)) {
                activeLink = link;
            }
        } catch (e) {
            // Bỏ qua các href không hợp lệ
            console.warn(`Could not parse href: ${href}`);
        }
    });

    if (activeLink) {
        activeLink.classList.add('active');

        // Nếu liên kết nằm trong dropdown, kích hoạt cả dropdown cha
        const dropdownMenu = activeLink.closest('.dropdown-menu');
        if (dropdownMenu) {
            const dropdownToggle = dropdownMenu.parentElement.querySelector('.dropdown-toggle');
            if (dropdownToggle) {
                dropdownToggle.classList.add('active');
            }
        }
    }
}

// Khởi tạo khi DOM đã tải xong
document.addEventListener('DOMContentLoaded', () => {
    initializeUI();
    initializeThemeSwitcher();
    initializeActiveMenu();
});

// Hiển thị thông báo lỗi
function showError(message, container = '#validationSummary') {
    const $container = $(container);
    const alert = `
        <div class="alert alert-danger alert-dismissible fade show" role="alert">
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>`;
    $container.html(alert).show();
}

// Xóa thông báo lỗi
function clearError(container = '#validationSummary') {
    $(container).empty().hide();
}

// Hiển thị lỗi validation cho field
function showFieldError(fieldId, message) {
    const $field = $(`#${fieldId}`);
    $field.addClass('is-invalid');

    // Xóa thông báo lỗi cũ nếu có
    $(`#${fieldId}Feedback`).remove();

    // Thêm thông báo lỗi mới
    $field.after(`<div id="${fieldId}Feedback" class="invalid-feedback">${message}</div>`);
}

// Xóa lỗi khi người dùng nhập liệu
function clearFieldError(fieldId) {
    $(`#${fieldId}`).removeClass('is-invalid');
    $(`#${fieldId}Feedback`).remove();
}

// Kiểm tra email hợp lệ
function isValidEmail(email) {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(String(email).toLowerCase());
}
