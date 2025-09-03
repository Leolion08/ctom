document.addEventListener('DOMContentLoaded', () => {
    'use strict';

    // Đảm bảo formUtils đã được load
    if (!window.CTOM || !window.CTOM.formUtils) {
        console.error('form-common.js is required and was not found.');
        alert('Lỗi khởi tạo trang. Vui lòng thử lại.');
        return;
    }
    const utils = window.CTOM.formUtils;
    const baseUrl = window.appBaseUrl || '/'; // fallback nếu chưa được gán

    // Khởi tạo chiều cao preview ngay khi trang được load
    setTimeout(() => utils.setPreviewHeight(), 100);

    // --- KHAI BÁO BIẾN VÀ DOM ELEMENTS ---
    const step1Form = document.getElementById('step1Form');
    const step2Section = document.getElementById('step2Section');
    const step2Form = document.getElementById('step2Form');
    const antiforgInput = document.querySelector('#step1Form input[name="__RequestVerificationToken"]');
    const htmlPreviewContainer = document.getElementById('htmlPreviewContainer');
    const templateNameDisplay = document.getElementById('templateNameDisplay');
    const dynamicFieldsContainer = document.getElementById('dynamicFieldsContainer');
    const btnSave = document.getElementById('btnSave');
    const btnBack = document.getElementById('btnBack');


    /**
     * Thiết lập và hiển thị giao diện cho bước 2.
     * @param {Array} fields - Mảng định nghĩa các trường động.
     * @param {string} templateId - ID của template.
     * @param {string} templateName - Tên của template.
     */
    async function setupStep2(fields, templateId, templateName) {
        document.getElementById('stepIndicator1').classList.remove('active');
        document.getElementById('stepIndicator2').classList.add('active');
        step1Form.classList.add('d-none');
        step2Section.classList.remove('d-none');

        step2Form.querySelector('input[name="TemplateId"]').value = templateId;
        templateNameDisplay.value = templateName;
        document.getElementById('Note').value = '';

        try {
            const htmlPreview = document.getElementById('html-preview');
            if (htmlPreview) {
                htmlPreview.innerHTML = '<div class="text-center p-5"><div class="spinner-border"></div><p class="mt-2">Đang tải preview...</p></div>';
            } else {
                const htmlPreviewContainer = document.getElementById('htmlPreviewContainer');
                if (htmlPreviewContainer) {
                    const newHtmlPreview = document.createElement('div');
                    newHtmlPreview.id = 'html-preview';
                    newHtmlPreview.className = 'document-page';
                    newHtmlPreview.innerHTML = '<div class="text-center p-5"><div class="spinner-border"></div><p class="mt-2">Đang tải preview...</p></div>';
                    htmlPreviewContainer.appendChild(newHtmlPreview);
                }
            }

            const response = await fetch(`${baseUrl}Form/GetHtmlPreview?templateId=${templateId}`, {
                method: 'GET',
                headers: {
                    'RequestVerificationToken': antiforgInput.value,
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error(`Lỗi khi tải preview: ${response.statusText}`);
            }

            const data = await response.json();

            if (data.success && data.htmlContent) {
                setTimeout(() => {
                    let htmlPreview = document.getElementById('html-preview');
                    const htmlPreviewContainer = document.getElementById('htmlPreviewContainer');

                    if (htmlPreview) {
                        htmlPreview.innerHTML = data.htmlContent;
                    } else if (htmlPreviewContainer) {
                        htmlPreviewContainer.innerHTML = '';
                        htmlPreview = document.createElement('div');
                        htmlPreview.id = 'html-preview';
                        htmlPreview.className = 'document-page';
                        htmlPreview.innerHTML = data.htmlContent;
                        htmlPreviewContainer.appendChild(htmlPreview);
                    }

                    if (htmlPreview && typeof CTOM.formUtils.setupInteractivePlaceholders === 'function') {
                        setTimeout(() => {
                            CTOM.formUtils.setupInteractivePlaceholders(htmlPreview);
                        }, 100);
                    }

                    if (typeof CTOM.formUtils.fitHtmlToPane === 'function') {
                        setTimeout(() => {
                            CTOM.formUtils.fitHtmlToPane();
                            if (typeof utils.setPreviewHeight === 'function') {
                                utils.setPreviewHeight();
                            }
                        }, 200);
                    }

                }, 100);
            } else {
                console.error('Không có HTML content trả về từ server');
                const htmlPreviewContainer = document.getElementById('htmlPreviewContainer');
                if (htmlPreviewContainer) {
                    htmlPreviewContainer.innerHTML = '<div class="text-center p-5"><p>Không có nội dung preview</p></div>';
                }
            }

            const previewArea = document.querySelector('.preview-area');
            if (previewArea) {
                previewArea.style.display = 'block';
            }

        } catch (error) {
            console.error('Lỗi khi tải HTML preview:', error);
            const htmlPreviewContainer = document.getElementById('htmlPreviewContainer');
            if (htmlPreviewContainer) {
                htmlPreviewContainer.innerHTML = `<div class="text-center p-5"><p class="text-danger">Lỗi khi tải preview: ${error.message}</p></div>`;
            }
        }

        utils.renderDynamicFields(fields, dynamicFieldsContainer);
        btnSave.disabled = false;
        utils.setPreviewHeight();
    }


    // --- GẮN KẾT SỰ KIỆN ---

    step1Form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const form = e.currentTarget;
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const templateSelectEl = form.querySelector('#templateSelect');
        const selectedOption = templateSelectEl.options[templateSelectEl.selectedIndex];
        if (!selectedOption || !selectedOption.value) {
            utils.showConfirmation({ //alert
                title: 'Thông báo', message: 'Vui lòng chọn một template.', showCancel: false,
                status: 'bg-info', icon: 'ti ti-alert-triangle text-info',
                confirmText: 'Đã hiểu', confirmIcon: 'ti ti-check', confirmBtnClass: 'btn btn-info w-100'
            });
            templateSelectEl.focus();
            return;
        }

        const templateName = selectedOption.text;
        const templateId = selectedOption.value;
        const formData = new FormData(form);

        try {
            const res = await fetch(form.action, {
                method: 'POST',
                headers: { 'RequestVerificationToken': antiforgInput.value },
                body: formData
            });

            if (!res.ok) throw new Error(`Lỗi từ server: ${res.statusText}`);

            const response = await res.json();
            const fields = response.fields || [];
            const redirectUrl = response.redirectUrl;
            await setupStep2(fields, templateId, templateName);

        } catch (error) {
            console.error('Lỗi khi xử lý bước 1:', error);
            await utils.showConfirmation({
                title: 'Lỗi', message: `Không thể lấy cấu trúc form: ${error.message}`, showCancel: false,
                status: 'bg-danger', icon: 'ti ti-x text-danger',
                confirmText: 'OK', confirmIcon: 'ti ti-check', confirmBtnClass: 'btn btn-danger w-100'
            });
        }
    });

    // UPDATE: Xử lý submit form bước 2 với logic cảnh báo
    step2Form.addEventListener('submit', async (e) => {
        e.preventDefault(); // Luôn chặn submit mặc định để xử lý

        // 1. Kiểm tra các trường bắt buộc
        if (!step2Form.checkValidity()) {
            await utils.showConfirmation({
                title: 'Dữ liệu không hợp lệ',
                message: 'Vui lòng điền đầy đủ các trường bắt buộc (*).',
                showCancel: false,
                status: 'bg-warning', icon: 'ti ti-alert-triangle text-warning',
                confirmText: 'Đã hiểu', confirmIcon: 'ti ti-check', confirmBtnClass: 'btn btn-warning w-100'
            });
            return; // Dừng lại nếu không hợp lệ
        }

        // 2. Kiểm tra các trường không bắt buộc nhưng bị bỏ trống
        const currentFields = utils.getCurrentFields();
        const missingFields = currentFields
            .map(field => {
                const input = step2Form.querySelector(`[name="FormValues[${field.fieldName}]"]`);
                // Một trường được coi là "thiếu" nếu input tồn tại nhưng giá trị là rỗng
                return (input && !input.value.trim()) ? field.displayName : null;
            })
            .filter(Boolean); // Lọc ra để lấy danh sách tên các trường bị thiếu

        let userConfirmed = false;
        if (missingFields.length > 0) {
            // Hỏi xác nhận nếu có trường bị bỏ trống
            userConfirmed = await utils.showConfirmation({
                title: 'Cảnh báo',
                message: `<p>Các trường sau chưa được nhập dữ liệu:</p><ul class="missing-fields-list">${missingFields.map(name => `<li>${name}</li>`).join('')}</ul><p class="mt-3">Bạn chắc chắn muốn lưu dữ liệu này?</p>`,
                status: 'bg-warning', icon: 'ti ti-alert-triangle text-warning',
                confirmText: 'Lưu', confirmIcon: 'ti ti-check', confirmBtnClass: 'btn btn-warning w-100',
                cancelText: 'Hủy', cancelIcon: 'ti ti-x', cancelBtnClass: 'btn btn-secondary w-100'
            });
        } else {
            // Nếu tất cả các trường đã được điền, chỉ cần hỏi xác nhận lưu
            userConfirmed = await utils.showConfirmation({
                title: 'Xác nhận lưu',
                message: 'Bạn chắc chắn muốn lưu các thay đổi này?',
                status: 'bg-primary', icon: 'ti ti-alert-triangle text-primary',
                confirmText: 'Lưu', confirmIcon: 'ti ti-check', confirmBtnClass: 'btn btn-primary w-100',
                cancelText: 'Hủy', cancelIcon: 'ti ti-x', cancelBtnClass: 'btn btn-secondary w-100'
            });
        }

        // 3. Nếu người dùng xác nhận, tiến hành submit form
        if (userConfirmed) {
            // DEBUG: Thu thập và log dữ liệu FormValues gửi lên để chẩn đoán binding
            try {
                const fvInputs = Array.from(step2Form.querySelectorAll('input[name^="FormValues["], textarea[name^="FormValues["]'))
                    .filter(el => !el.disabled);
                const formValues = {};
                fvInputs.forEach(el => {
                    const m = el.name.match(/^FormValues\[(.+)\]$/);
                    if (m) formValues[m[1]] = el.value;
                });

                const expectedFields = (utils.getCurrentFields?.() || []).map(f => f.fieldName);
                const missingInputs = expectedFields.filter(fn => !fvInputs.some(el => el.name === `FormValues[${fn}]`));

                // Log gọn gàng để dễ đọc trên console
                console.debug('[Create Step2] FormValues object:', formValues);
                if (missingInputs.length > 0) {
                    console.warn('[Create Step2] Thiếu input cho các trường:', missingInputs);
                }

                // Tuỳ chọn: log toàn bộ FormData entries để so sánh
                const fd = new FormData(step2Form);
                const entries = {};
                for (const [k, v] of fd.entries()) { entries[k] = v; }
                console.debug('[Create Step2] Toàn bộ FormData entries:', entries);
            } catch (dbgErr) {
                console.warn('[Create Step2] Lỗi khi log debug FormValues:', dbgErr);
            }

            utils.setProcessingState(true, btnSave, { text: 'Đang lưu...' });
            step2Form.submit(); // Thực hiện submit form một cách tự nhiên
            return;
        }
        // Nếu người dùng hủy ở modal xác nhận, đảm bảo nút vẫn ở trạng thái bình thường
        utils.setProcessingState(false, btnSave);
    });

    btnBack.addEventListener('click', () => {
        step2Section.classList.add('d-none');
        step1Form.classList.remove('d-none');
        document.getElementById('stepIndicator2').classList.remove('active');
        document.getElementById('stepIndicator1').classList.add('active');
    });

    window.addEventListener('resize', () => {
        utils.setPreviewHeight();
    });
});
