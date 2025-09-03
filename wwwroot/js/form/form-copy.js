document.addEventListener('DOMContentLoaded', () => {
    'use strict';

    if (!window.CTOM || !window.CTOM.formUtils) {
        console.error('form-common.js is required and was not found.');
        alert('Lỗi khởi tạo trang. Vui lòng thử lại.');
        return;
    }
    const utils = window.CTOM.formUtils;
    const baseUrl = window.appBaseUrl || '/';

    // --- KHAI BÁO BIẾN VÀ DOM ELEMENTS ---
    const copyForm = document.getElementById('copyForm');
    const dynamicFieldsContainer = document.getElementById('dynamicFieldsContainer');
    const htmlPreviewContainer = document.getElementById('htmlPreviewContainer');
    const btnSave = document.getElementById('btnSave');


    const templateId = window.templateId;
    const initialFormData = window.initialFormData || {};

    /**
     * Khởi tạo trang Copy.
     */
    async function initializePage() {
        try {
            // 1. Lấy cấu trúc các trường từ template
            const res = await fetch(`${baseUrl}Form/GetTemplateFields/${templateId}`);
            if (!res.ok) throw new Error('Không thể tải cấu trúc form.');
            const fields = await res.json();

            // 2. Render các trường và điền dữ liệu đã sao chép
            // Hàm này trong form-common.js đã tự động gắn các event listener cần thiết
            utils.renderDynamicFields(fields, dynamicFieldsContainer, initialFormData);

            // 3. Khởi tạo placeholder tương tác cho preview (nếu có nội dung)
            const htmlPreview = document.getElementById('html-preview');
            if (htmlPreview && typeof CTOM.formUtils.setupInteractivePlaceholders === 'function') {
                setTimeout(() => {
                    CTOM.formUtils.setupInteractivePlaceholders(htmlPreview);
                }, 50);
            }

            // 4. Điều chỉnh giao diện
            setTimeout(() => {
                utils.fitHtmlToPane();
                utils.setPreviewHeight();
            }, 100);

        } catch (err) {
            console.error('Lỗi khởi tạo trang Copy:', err);
            await utils.showConfirmation({
                title: 'Lỗi', message: `Không thể tải trang: ${err.message}`,
                showCancel: false, status: 'bg-danger', icon: 'ti ti-x text-danger'
            });
        }
    }

    // --- GẮN KẾT SỰ KIỆN ---

    // Xử lý submit form (đồng bộ với form-create.js)
    copyForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        // 1) Kiểm tra required theo HTML5
        if (!copyForm.checkValidity()) {
            await utils.showConfirmation({
                title: 'Dữ liệu không hợp lệ',
                message: 'Vui lòng điền đầy đủ các trường bắt buộc (*).',
                showCancel: false,
                status: 'bg-warning', icon: 'ti ti-alert-triangle text-warning',
                confirmText: 'Đã hiểu', confirmIcon: 'ti ti-check', confirmBtnClass: 'btn btn-warning w-100'
            });
            return;
        }

        // 2) Cảnh báo khi có trường bỏ trống (không chỉ required)
        const currentFields = utils.getCurrentFields();
        const missingFields = currentFields
            .map(field => {
                const input = copyForm.querySelector(`[name="FormValues[${field.fieldName}]"]`);
                return (input && !input.value.trim()) ? field.displayName : null;
            })
            .filter(Boolean);

        let userConfirmed = false;
        if (missingFields.length > 0) {
            userConfirmed = await utils.showConfirmation({
                title: 'Cảnh báo',
                message: `<p>Các trường sau chưa được nhập dữ liệu:</p><ul class="missing-fields-list">${missingFields.map(name => `<li>${name}</li>`).join('')}</ul><p class="mt-3">Bạn chắc chắn muốn lưu dữ liệu này?</p>`,
                status: 'bg-warning', icon: 'ti ti-alert-triangle text-warning',
                confirmText: 'Lưu', confirmIcon: 'ti ti-check', confirmBtnClass: 'btn btn-warning w-100',
                cancelText: 'Hủy', cancelIcon: 'ti ti-x', cancelBtnClass: 'btn btn-secondary w-100'
            });
        } else {
            userConfirmed = await utils.showConfirmation({
                title: 'Xác nhận lưu',
                message: 'Bạn chắc chắn muốn tạo một giao dịch mới với các thông tin này?',
                status: 'bg-primary', icon: 'ti ti-help-circle text-primary',
                confirmText: 'Lưu', confirmIcon: 'ti ti-check', confirmBtnClass: 'btn btn-primary w-100',
                cancelText: 'Hủy', cancelIcon: 'ti ti-x', cancelBtnClass: 'btn btn-secondary w-100'
            });
        }

        if (userConfirmed) {
            utils.setProcessingState(true, btnSave, { text: 'Đang lưu...' });
            copyForm.submit(); // Submit form đến action /Form/Create
            return;
        }
        // Nếu người dùng hủy ở bước xác nhận, đảm bảo nút vẫn ở trạng thái bình thường
        utils.setProcessingState(false, btnSave);
    });

    window.addEventListener('resize', () => {
        utils.setPreviewHeight();
        utils.fitHtmlToPane();
    });

    // --- CHẠY HÀM KHỞI TẠO ---
    initializePage();
});
