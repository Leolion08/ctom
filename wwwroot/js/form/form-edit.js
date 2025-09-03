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
    const editForm = document.getElementById('editForm');
    const templateId = document.querySelector('input[name="TemplateId"]').value;
    const formDataId = document.querySelector('input[name="FormDataID"]').value;
    const antiforgInput = document.querySelector('#editForm input[name="__RequestVerificationToken"]');
    const htmlPreview = document.getElementById('html-preview'); // Lấy trực tiếp vùng chứa HTML
    const dynamicFieldsContainer = document.getElementById('dynamicFieldsContainer');
    const btnSave = document.getElementById('btnSave');
    const btnReview = document.getElementById('btnReview');
    const btnDownload = document.getElementById('btnDownload');

    // Dữ liệu ban đầu được truyền từ server qua thẻ script trong Edit.cshtml
    let initialFormData = window.initialFormData || {};

    // Sử dụng utils.setProcessingState từ form-common.js cho trạng thái nút

    /**
     * SỬA LỖI: Cấu trúc lại hàm khởi tạo để giải quyết race condition.
     */
    async function initializePage() {
        try {
            // 1. Lấy cấu trúc các trường (fields) từ server
            const fieldsRes = await fetch(`${baseUrl}Form/GetTemplateFields/${templateId}`);
            if (!fieldsRes.ok) throw new Error('Không thể tải cấu trúc form.');
            const fields = await fieldsRes.json();

            // 2. Render các control nhập liệu và điền dữ liệu ban đầu
            // (Hàm này trong form-common.js đã được sửa để không trigger event nữa)
            utils.renderDynamicFields(fields, dynamicFieldsContainer, initialFormData);

            // 3. Xử lý vùng preview đã được render sẵn từ server
            if (htmlPreview) {
                // Kích hoạt các placeholder <<...>> để chúng có thể tương tác
                utils.setupInteractivePlaceholders(htmlPreview);

                // 4. SAU KHI placeholder đã sẵn sàng, lặp qua dữ liệu và cập nhật chúng
                // Đây là bước quan trọng để đảm bảo placeholder tồn tại trước khi được cập nhật
                for (const [key, val] of Object.entries(initialFormData)) {
                    if (val !== null && val !== undefined) {
                        const fieldInfo = fields.find(f => f.fieldName === key);
                        utils.updatePreviewField(key, val.toString(), fieldInfo?.dataType);
                    }
                }
            }

            // 5. Điều chỉnh giao diện sau khi mọi thứ đã sẵn sàng
            setTimeout(() => {
                utils.setPreviewHeight();
                utils.fitHtmlToPane();
            }, 100);

        } catch (error) {
            console.error("Lỗi khởi tạo trang Edit:", error);
            await utils.showConfirmation({
                title: 'Lỗi', message: `Không thể tải trang: ${error.message}`,
                showCancel: false, status: 'bg-danger', icon: 'ti ti-x text-danger'
            });
        }
    }

    // --- GẮN KẾT SỰ KIỆN ---

    // Xử lý submit form Edit (logic AJAX giữ nguyên, vẫn hoạt động tốt)
    editForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        if (!editForm.checkValidity()) {
            await utils.showConfirmation({
                title: 'Dữ liệu không hợp lệ',
                message: 'Vui lòng điền đầy đủ các trường bắt buộc (*).',
                showCancel: false,
                status: 'bg-warning', icon: 'ti ti-alert-triangle text-warning',
                confirmText: 'Đã hiểu', confirmIcon: 'ti ti-check', confirmBtnClass: 'btn btn-warning w-100'
            });
            return;
        }

        const currentFields = utils.getCurrentFields();
        const missingFields = currentFields
            .map(field => {
                const input = editForm.querySelector(`[name="FormValues[${field.fieldName}]"]`);
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
                message: 'Bạn chắc chắn muốn lưu các thay đổi này?',
                status: 'bg-primary', icon: 'ti ti-alert-triangle text-primary',
                confirmText: 'Lưu', confirmIcon: 'ti ti-check', confirmBtnClass: 'btn btn-primary w-100',
                cancelText: 'Hủy', cancelIcon: 'ti ti-x', cancelBtnClass: 'btn btn-secondary w-100'
            });
        }

        if (!userConfirmed) return;

        utils.setProcessingState(true, btnSave, { text: 'Đang lưu...' });

        const formData = new FormData(editForm);
        // Logic chuyển đổi ngày tháng trước khi gửi vẫn giữ nguyên
        document.querySelectorAll('input[data-bs-toggle="datepicker"]').forEach(el => {
            const name = el.getAttribute('name');
            const val = el.value;
            if (val && /^\d{2}\/\d{2}\/\d{4}$/.test(val)) {
                const parts = val.split('/');
                const isoDate = `${parts[2]}-${parts[1]}-${parts[0]}`;
                formData.set(name, isoDate);
            }
        });

        try {
            const res = await fetch(editForm.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': antiforgInput.value,
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });
            const json = await res.json();
            if (json.success) {
                // Cập nhật lại initialFormData sau khi lưu thành công
                const newFormValues = Array.from(new FormData(editForm).entries())
                    .reduce((acc, [key, value]) => {
                        const fieldName = key.match(/FormValues\[(.*?)\]/)?.[1];
                        if (fieldName) acc[fieldName] = value;
                        return acc;
                    }, {});
                initialFormData = newFormValues;

                await utils.showConfirmation({ title: 'Thành công', message: 'Cập nhật dữ liệu thành công!', showCancel: false, status: 'bg-success', icon: 'ti ti-circle-check', confirmText: 'OK' });
            } else {
                throw new Error(json.message || 'Lỗi không xác định từ server.');
            }
        } catch (error) {
            await utils.showConfirmation({ title: 'Lỗi', message: error.message, showCancel: false, status: 'bg-danger', icon: 'ti ti-x', confirmText: 'Đã hiểu' });
        } finally {
            utils.setProcessingState(false, btnSave);
        }
    });

    // Các event listener cho Review và Download không thay đổi vì chúng độc lập với cơ chế preview
    /**
     * PHẦN XỬ LÝ MỚI: Logic cho nút "Xem tài liệu đã lưu" (btnReview)
     * Thay vì xử lý blob và docxtemplater ở client, giờ đây sẽ gọi một API mới từ server.
     */
    btnReview.addEventListener('click', async () => {
        const modalBody = document.getElementById('mergePreviewBody');
        const modalDownloadBtn = document.getElementById('modalDownloadBtn');
        const mergePreviewModal = new bootstrap.Modal(document.getElementById('mergePreviewModal'));

        // Hiển thị trạng thái đang tải
        utils.setProcessingState(true, btnReview, { text: 'Đang xử lý...' });
        modalDownloadBtn.disabled = true;
        modalBody.innerHTML = '<div class="text-center p-5"><div class="spinner-border"></div><p class="mt-2">Đang xử lý...</p></div>';
        mergePreviewModal.show();

        try {
            // THAY ĐỔI CỐT LÕI: Gọi đến endpoint mới trên server để lấy HTML đã được trộn dữ liệu.
            const res = await fetch(`${baseUrl}Form/GetMergedHtmlPreview/${formDataId}`);
            const data = await res.json();

            if (res.ok && data.success) {
                // Hiển thị HTML đã được trộn trong cấu trúc wrapper giống vùng preview chính
                // để toàn bộ CSS ở form-edit.css áp dụng đồng nhất (document-preview -> #html-preview.document-page -> #document-preview)
                modalBody.innerHTML = `
                    <div class="document-preview">
                        <div class="document-page" id="html-preview">
                            <!--<div id="document-preview">-->
                            ${data.htmlContent}
                            <!--<</div>-->
                        </div>
                    </div>`;
                modalDownloadBtn.disabled = false; // Bật nút tải về
            } else {
                // Ném lỗi nếu API trả về thất bại
                throw new Error(data.message || 'Không thể tạo bản xem trước.');
            }
        } catch (error) {
            // Hiển thị thông báo lỗi nếu có vấn đề xảy ra
            modalBody.innerHTML = `<div class="alert alert-danger">${error.message}</div>`;
        } finally {
            utils.setProcessingState(false, btnReview);
        }
    });

    btnDownload.addEventListener('click', async (e) => {
        const button = e.currentTarget;
        utils.setProcessingState(true, button, { text: 'Đang xử lý...' });

        try {
            const blob = await utils.processMerge(formDataId, templateId, false);
            if (blob) {
                saveAs(blob, utils.generateFilename(formDataId));
            }
        } finally {
            utils.setProcessingState(false, button);
        }
    });

    document.getElementById('modalDownloadBtn').addEventListener('click', async (e) => {
        const button = e.currentTarget;
        utils.setProcessingState(true, button, { text: 'Đang xử lý...' });

        try {
            const blob = await utils.processMerge(formDataId, templateId, false);
            if (blob) {
                saveAs(blob, utils.generateFilename(formDataId));
            }
        } finally {
            utils.setProcessingState(false, button);
        }
    });

    // Cập nhật lại layout khi thay đổi kích thước cửa sổ
    window.addEventListener('resize', () => {
        utils.setPreviewHeight();
        utils.fitHtmlToPane(); // Sử dụng hàm mới
    });

    // Khởi chạy logic của trang
    initializePage();
});
