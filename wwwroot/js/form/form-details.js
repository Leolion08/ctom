document.addEventListener('DOMContentLoaded', () => {
    'use strict';

    // Kiểm tra sự tồn tại của formUtils
    if (!window.CTOM || !window.CTOM.formUtils) {
        console.error('form-common.js is required and was not found.');
        alert('Lỗi khởi tạo trang. Vui lòng thử lại.');
        return;
    }
    const utils = window.CTOM.formUtils;
    const baseUrl = window.appBaseUrl || '/'; // fallback nếu chưa được gán

    // --- KHAI BÁO BIẾN VÀ DOM ELEMENTS ---
    const htmlPreview = document.getElementById('html-preview');
    const dynamicFieldsContainer = document.getElementById('dynamicFieldsContainer');
    const btnReview = document.getElementById('btnReview');
    const btnDownload = document.getElementById('btnDownload');

    // Lấy dữ liệu từ các thuộc tính data-* trên nút bấm
    const templateId = btnReview.dataset.templateId;
    const formDataId = btnReview.dataset.formId;

    // Dữ liệu ban đầu được truyền từ server
    const initialFormData = window.initialFormData || {};

    /**
     * PHẦN THAY ĐỔI: Sửa lại hoàn toàn logic khởi tạo trang Details.
     * Trang Details giờ sẽ hiển thị trực tiếp tài liệu đã được trộn dữ liệu.
     */
    async function initializePage() {
        try {
            // 1. Lấy cấu trúc các trường và render form chỉ đọc bên trái
            const fieldsRes = await fetch(`${baseUrl}Form/GetTemplateFields/${templateId}`);
            if (!fieldsRes.ok) throw new Error('Không thể tải cấu trúc form.');
            const fields = await fieldsRes.json();
            utils.renderDynamicFields(fields, dynamicFieldsContainer, initialFormData, true);

            // 2. Xử lý vùng preview bên phải
            if (htmlPreview) {
                // THAY ĐỔI CỐT LÕI: Gọi API để lấy thẳng HTML đã được trộn dữ liệu và highlight.
                // Điều này đảm bảo trang Details hiển thị đúng như trong modal "Xem tài liệu đã lưu".
                const res = await fetch(`${baseUrl}Form/GetMergedHtmlPreview/${formDataId}`);
                const data = await res.json();
                if (res.ok && data.success) {
                    htmlPreview.innerHTML = data.htmlContent;
                } else {
                    // Hiển thị lỗi nếu không lấy được preview
                    htmlPreview.innerHTML = `<div class="alert alert-danger p-3">${data.message || 'Lỗi tải bản xem trước.'}</div>`;
                }
            }

            // 3. Điều chỉnh giao diện sau khi đã có nội dung
            setTimeout(() => {
                utils.setPreviewHeight();
                utils.fitHtmlToPane();
            }, 100);

        } catch (error) {
            console.error("Lỗi khởi tạo trang Details:", error);
            dynamicFieldsContainer.innerHTML = `<div class="alert alert-danger">${error.message}</div>`;
        }
    }

    // --- GẮN KẾT SỰ KIỆN ---

    // Sự kiện cho nút "Xem tài liệu đã lưu"
    btnReview.addEventListener('click', async () => {
        const modalBody = document.getElementById('mergePreviewBody');
        const modalDownloadBtn = document.getElementById('modalDownloadBtn');
        const mergePreviewModal = new bootstrap.Modal(document.getElementById('mergePreviewModal'));

        modalDownloadBtn.disabled = true;
        modalBody.innerHTML = '<div class="text-center p-5"><div class="spinner-border"></div><p class="mt-2">Đang xử lý...</p></div>';
        mergePreviewModal.show();

        try {
            const res = await fetch(`${baseUrl}Form/GetMergedHtmlPreview/${formDataId}`);
            const data = await res.json();
            if (res.ok && data.success) {
                modalBody.innerHTML = `<div class="document-page">${data.htmlContent}</div>`;
                modalDownloadBtn.disabled = false;
            } else {
                throw new Error(data.message || 'Không thể tạo bản xem trước.');
            }
        } catch (error) {
            modalBody.innerHTML = `<div class="alert alert-danger">${error.message}</div>`;
        }
    });

    // Sự kiện cho nút "Tải file (docx)"
    btnDownload.addEventListener('click', async (e) => {
        const button = e.currentTarget;
        const originalHtml = button.innerHTML;
        button.disabled = true;
        button.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status"></span>Đang xử lý...';

        const blob = await utils.processMerge(formDataId, templateId);
        if (blob) {
            saveAs(blob, utils.generateFilename(formDataId));
        }

        button.disabled = false;
        button.innerHTML = originalHtml;
    });

    // Sự kiện cho nút tải về trong modal
    document.getElementById('modalDownloadBtn').addEventListener('click', async () => {
        const blob = await utils.processMerge(formDataId, templateId);
        if (blob) {
            saveAs(blob, utils.generateFilename(formDataId));
        }
    });

    // Điều chỉnh layout khi resize
    window.addEventListener('resize', () => {
        utils.setPreviewHeight();
        utils.fitHtmlToPane();
    });

    // Khởi chạy
    initializePage();
});
