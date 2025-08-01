// Đảm bảo namespace CTOM tồn tại
window.CTOM = window.CTOM || {};

/**
 * IIFE (Immediately Invoked Function Expression) để tạo một module 'formUtils'.
 * Module này chứa các hàm dùng chung cho các trang form (Create, Edit, Details).
 * @param {object} utils - Namespace để gắn các hàm tiện ích vào.
 */
(function(utils) {
    'use strict';

    // --- Đường dẫn gốc ---
    const baseUrl = window.appBaseUrl || '/'; // fallback nếu chưa được gán

    // --- BIẾN CỦA MODULE ---
    let currentFields = [];
    let lastMergedData = {};

    /**
     * Getter để lấy danh sách các trường hiện tại.
     * @returns {Array} Mảng định nghĩa các trường.
     */
    utils.getCurrentFields = function() {
        return currentFields;
    };

    // --- CÁC HÀM TIỆN ÍCH (HELPER FUNCTIONS) ---

    utils.initDatepicker = function($element) {
        if (typeof $().datepicker === 'function') {
            $element.datepicker({
                format: 'dd/mm/yyyy',
                autoclose: true,
                todayHighlight: true,
                language: 'vi',
                orientation: 'bottom auto'
            }).on('changeDate', function(e) {
                // Khi người dùng chọn ngày, cập nhật placeholder trong preview
                const fieldName = e.target.id;
                const formattedDate = e.format('dd/mm/yyyy');
                console.log(`Đã chọn ngày: ${formattedDate} cho trường ${fieldName}`);

                // Cập nhật preview nếu hàm updatePreviewField tồn tại
                if (typeof utils.updatePreviewField === 'function') {
                    utils.updatePreviewField(fieldName, formattedDate, 'DATE');
                }
            });
        }
    };

    utils.setPreviewHeight = function() {
        // Lấy các element cần thiết
        const previewArea = document.querySelector('.preview-area');
        const formInputArea = document.querySelector('.form-input-area');

        // Nếu không tìm thấy các element cần thiết, thoát
        if (!previewArea || !formInputArea) return;

        // Lấy chiều cao tổng thể của form input area (bao gồm cả footer)
        const formHeight = formInputArea.offsetHeight;

        // Tính toán chiều cao tối thiểu dựa trên viewport
        const topPos = previewArea.getBoundingClientRect().top;
        const minHeight = window.innerHeight - topPos - 24;

        // Sử dụng chiều cao lớn hơn giữa form và viewport
        const optimalHeight = Math.max(formHeight, minHeight);

        // Áp dụng chiều cao cho toàn bộ preview area
        previewArea.style.height = `${optimalHeight}px`;

        // Điều chỉnh chiều cao của preview-pane-body nếu có
        const previewPaneBody = document.getElementById('preview-pane-body');
        if (previewPaneBody) {
            // Tính chiều cao của các phần khác trong preview area (header, footer nếu có)
            const previewAreaChildren = Array.from(previewArea.children);
            let otherElementsHeight = 0;
            previewAreaChildren.forEach(child => {
                if (child !== previewPaneBody && child.offsetParent !== null) { // kiểm tra element có hiển thị
                    otherElementsHeight += child.offsetHeight;
                }
            });

            // Điều chỉnh chiều cao của preview-pane-body
            const previewBodyHeight = optimalHeight - otherElementsHeight;
            previewPaneBody.style.height = `${previewBodyHeight}px`;
            console.log(`Đã điều chỉnh chiều cao preview-pane-body: ${previewBodyHeight}px`);
        }

        console.log(`Đã đồng bộ chiều cao: ${optimalHeight}px (form: ${formHeight}px, min: ${minHeight}px)`);
    };

    /**
     * Tối ưu hiển thị HTML preview để phù hợp với không gian hiển thị
     * Dựa trên logic của hàm fitDocxToPane gốc nhưng được điều chỉnh cho HTML preview
     */
    utils.fitHtmlToPane = function() {
        const wrapper = document.getElementById('htmlPreviewContainer');
        const htmlPreview = document.getElementById('html-preview');
        if (!wrapper || !htmlPreview) return;

        // Đặt lại transform về 1 để đo kích thước thực
        htmlPreview.style.transform = 'scale(1)';

        // Tính toán tỷ lệ scale dựa trên chiều rộng của container và nội dung
        // Sử dụng scrollWidth thay vì clientWidth để lấy kích thước thực của nội dung
        const wrapperWidth = wrapper.clientWidth;
        const contentWidth = htmlPreview.scrollWidth;
        const ratio = wrapperWidth / contentWidth;

        // Thêm padding cho nội dung
        //htmlPreview.style.paddingTop = '10pt';
        //htmlPreview.style.paddingBottom = '10pt';

        // Chỉ scale nếu nội dung lớn hơn container
        if (ratio < 0.95) { // Sử dụng ngưỡng 0.95 thay vì 1 để tránh scale khi chỉ hơi lớn hơn
            htmlPreview.style.transformOrigin = 'top center';
            htmlPreview.style.transform = `scale(${ratio * 0.95})`; // Giảm thêm 5% để có khoảng trống
            console.log(`Đã scale HTML preview với tỷ lệ: ${ratio * 0.95}`);
        } else {
            // Nếu nội dung nhỏ hơn container, có thể căn giữa nếu muốn
            htmlPreview.style.margin = '0 auto';
        }
    };

    utils.showConfirmation = function(config) {
        return new Promise(resolve => {
            const customModalEl = document.getElementById('customModal');
            if (!customModalEl) {
                console.error("Modal element #customModal not found.");
                resolve(false);
                return;
            }

            const bsCustomModal = bootstrap.Modal.getOrCreateInstance(customModalEl);
            const modal = {
                title: document.getElementById('modalTitle'),
                message: document.getElementById('modalMessage'),
                icon: document.getElementById('modalIcon'),
                status: document.getElementById('modalStatus'),
                confirmBtn: document.getElementById('modalConfirmBtn'),
                cancelBtn: document.getElementById('modalCancelBtn')
            };

            modal.title.textContent = config.title || 'Xác nhận';
            modal.message.innerHTML = config.message || '';
            modal.icon.className = config.icon || 'ti ti-help-circle';
            modal.status.className = `modal-status ${config.status || 'bg-primary'}`;

            //confirmBtn
            modal.confirmBtn.innerHTML = `<i class="${config.confirmIcon || 'ti ti-check'} me-2"></i> ${config.confirmText || 'Đồng ý'}`;
            modal.confirmBtn.className = config.confirmBtnClass || 'btn btn-primary';

            //cancelBtn
            modal.cancelBtn.innerHTML = `<i class="${config.cancelIcon || 'ti ti-x'} me-2"></i> ${config.cancelText || 'Hủy'}`;
            modal.cancelBtn.className = config.cancelBtnClass || 'btn btn-secondary';
            modal.cancelBtn.style.display = config.showCancel === false ? 'none' : 'block';

            const colConfirm = modal.confirmBtn.parentElement;
            const colCancel = modal.cancelBtn.parentElement;
            if (config.showCancel === false) {
                colConfirm.classList.remove('col');
                //colConfirm.classList.add('col-12');
                colConfirm.classList.add('col-12', 'd-flex', 'justify-content-center');

                colCancel.style.display = 'none';
            } else {
                colConfirm.classList.add('col');
                colConfirm.classList.remove('col-12');
                colCancel.style.display = 'block';
            }

            let result = false;
            const onConfirm = () => { result = true; bsCustomModal.hide(); };
            const onHidden = () => {
                modal.confirmBtn.removeEventListener('click', onConfirm);
                customModalEl.removeEventListener('hidden.bs.modal', onHidden);
                resolve(result);
            };

            modal.confirmBtn.addEventListener('click', onConfirm, { once: true });
            customModalEl.addEventListener('hidden.bs.modal', onHidden, { once: true });
            bsCustomModal.show();
        });
    };

    // --- CÁC HÀM XỬ LÝ DOCX ---

    function formatDataForMerge(data, fields) {
        const formattedData = {};
        for (const key in data) {
            if (Object.prototype.hasOwnProperty.call(data, key)) {
                const value = data[key];
                const fieldInfo = fields.find(f => f.fieldName === key);

                if (value === null || value === undefined) {
                    formattedData[key] = '';
                    continue;
                }
                if (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}/.test(value)) {
                    const date = new Date(value);
                    if (!isNaN(date.getTime())) {
                        formattedData[key] = date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
                        continue;
                    }
                }
                if (fieldInfo && fieldInfo.dataType === 'NUMBER') {
                    const num = Number(value);
                    if (!isNaN(num)) {
                        //formattedData[key] = num.toLocaleString('vi-VN');
                        // Sử dụng 'en-US' để có định dạng 12,345.67
                        formattedData[key] = num.toLocaleString('en-US', { maximumFractionDigits: 6 });
                        continue;
                    }
                }
                formattedData[key] = value;
            }
        }
        return formattedData;
    }

    utils.processMerge = async function(formDataId, templateId, forHighlighting = false) {
        try {
            const templateInfoRes = await fetch(`${baseUrl}Template/GetMappedInfo/${templateId}`);
            if (!templateInfoRes.ok) throw new Error('Không thể lấy thông tin file template.');
            const templateInfo = await templateInfoRes.json();
            if (!templateInfo.success || !templateInfo.data?.exists) {
                throw new Error(templateInfo.message || 'Template chưa có file mapped hoặc không khả dụng.');
            }

            const formDataRes = await fetch(`${baseUrl}api/formData/${formDataId}`);
            if (!formDataRes.ok) throw new Error('Không thể lấy dữ liệu form.');
            const formDataPayload = await formDataRes.json();

            const fields = utils.getCurrentFields();
            const formattedData = formatDataForMerge(formDataPayload.data, fields);
            lastMergedData = formattedData;

            let dataToRender = formattedData;
            if (forHighlighting) {
                dataToRender = {};
                for (const key in formattedData) {
                    dataToRender[key] = formattedData[key] ? `__HL_START__${formattedData[key]}__HL_END__` : '';
                }
            }

            const templateRes = await fetch(templateInfo.data.fileUrl);
            if (!templateRes.ok) throw new Error('Không thể tải file template.');
            const templateBuffer = await templateRes.arrayBuffer();

            const zip = new PizZip(templateBuffer);
            const doc = new window.docxtemplater(zip, {
                paragraphLoop: true,
                linebreaks: true,
                delimiters: { start: '<<', end: '>>' },
                nullGetter: () => ""
            });
            doc.render(dataToRender);

            return doc.getZip().generate({
                type: 'blob',
                mimeType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
            });
        } catch (error) {
            console.error("Lỗi trong quá trình processMerge:", error);
            await utils.showConfirmation({ title: 'Lỗi', message: `Không thể xử lý file: ${error.message}`, showCancel: false, status: 'bg-danger', icon: 'ti ti-x', confirmText: 'Đã hiểu' });
            return null;
        }
    };

    utils.generateFilename = function(formDataId) {
        const cif = lastMergedData.SoCif || lastMergedData.SoCIF || lastMergedData.socif || 'unknown';
        const pad = (n) => n.toString().padStart(2, '0');
        const now = new Date();
        const ts = `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}-${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`;
        return `merged_${cif}_${formDataId}_${ts}.docx`;
    };


    // --- CÁC HÀM RENDER GIAO DIỆN ĐỘNG ---

    /**
     * Thiết lập các placeholder tương tác trong HTML preview
     * Tìm các placeholder dạng <<fieldName>> và chuyển thành các span có style
     * @param {HTMLElement} rootElement - Element gốc chứa HTML preview
     */
    utils.setupInteractivePlaceholders = function(rootElement) {
        if (!rootElement) {
            console.warn('setupInteractivePlaceholders: rootElement không tồn tại');
            return;
        }

        console.log('Bắt đầu thiết lập interactive placeholders');
        const regex = /<<([^<>]+)>>/g;
        const walker = document.createTreeWalker(rootElement, NodeFilter.SHOW_TEXT, null);
        let nodesToProcess = [];
        let node;

        // Tìm tất cả text node chứa placeholder
        while (node = walker.nextNode()) {
            if (node.textContent.includes('<<')) {
                nodesToProcess.push(node);
            }
        }

        // Xử lý từng text node
        nodesToProcess.forEach(textNode => {
            const parent = textNode.parentNode;
            if (!parent || !textNode.textContent) return;

            const parts = textNode.textContent.split(regex);
            if (parts.length <= 1) return;

            const fragment = document.createDocumentFragment();
            for (let i = 0; i < parts.length; i++) {
                const text = parts[i];
                if (i % 2 === 1) {
                    // Đây là placeholder
                    const span = document.createElement('span');
                    span.className = 'placeholder-span placeholder-empty';
                    span.dataset.placeholderFor = text;
                    span.textContent = `<<${text}>>`;
                    fragment.appendChild(span);
                } else if (text) {
                    // Đây là text thường
                    fragment.appendChild(document.createTextNode(text));
                }
            }
            parent.replaceChild(fragment, textNode);
        });

        //console.log(`Đã xử lý ${nodesToProcess.length} placeholder trong HTML preview`);
    };

    /**
     * Cập nhật giá trị cho các placeholder trong HTML preview
     * @param {string} fieldName - Tên trường cần cập nhật
     * @param {string} value - Giá trị mới
     * @param {string} dataType - Loại dữ liệu của trường
     */
    /**
     * THAY ĐỔI: Cập nhật hàm updatePreviewField để định dạng số theo chuẩn en-US.
     * Chuẩn này sử dụng ',' cho hàng nghìn và '.' cho thập phân.
     */
    utils.updatePreviewField = function(fieldName, value, dataType) {
        const placeholderSpans = document.querySelectorAll(`[data-placeholder-for="${fieldName}"]`);
        if (placeholderSpans.length === 0) {
            return;
        }

        const fieldInfo = currentFields.find(f => f.fieldName === fieldName);
        let displayValue = value;

        if (value) {
            if ((fieldInfo && fieldInfo.dataType === 'NUMBER') || dataType === 'NUMBER') {
                 // Xóa dấu phẩy (,) phân cách hàng nghìn để parse
                 let cleanValue = value.toString().replace(/,/g, '');
                 const num = Number(cleanValue);
                 if (!isNaN(num)) {
                    // Sử dụng 'en-US' để có định dạng 12,345.67
                    displayValue = num.toLocaleString('en-US', { maximumFractionDigits: 6 });
                 }
            } else if ((fieldInfo && fieldInfo.dataType === 'DATE') || dataType === 'DATE') {
                let date;
                if (/^\d{4}-\d{2}-\d{2}/.test(value)) {
                    date = new Date(value);
                } else if (/^\d{1,2}\/\d{1,2}\/\d{4}/.test(value)) {
                    const parts = value.split('/');
                    if (parts.length === 3) date = new Date(`${parts[1]}/${parts[0]}/${parts[2]}`);
                }
                if (date && !isNaN(date.getTime())) {
                    displayValue = date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
                }
            }
        }

        placeholderSpans.forEach(span => {
            if (value) {
                span.textContent = displayValue;
                span.classList.remove('placeholder-empty');
                span.classList.add('placeholder-filled');
            } else {
                span.textContent = `<<${fieldName}>>`;
                span.classList.remove('placeholder-filled');
                span.classList.add('placeholder-empty');
            }
        });
    };

    /**
     * CẬP NHẬT: Render các trường nhập liệu động, có xử lý cho chế độ chỉ đọc.
     * @param {Array} fields - Mảng đối tượng định nghĩa các trường.
     * @param {HTMLElement} container - Element để render các trường vào.
     * @param {object} [initialData={}] - Dữ liệu ban đầu để điền vào form.
     * @param {boolean} [readOnly=false] - Cờ xác định chế độ chỉ đọc.
     */
    /**
     * THAY ĐỔI: Cập nhật hàm renderDynamicFields
     * - Sửa định dạng số cho các trường chỉ đọc (read-only).
     * - Tối ưu hóa logic nhập liệu cho các trường số có thể chỉnh sửa.
     */
    utils.renderDynamicFields = function(fields, container, initialData = {}, readOnly = false) {
        currentFields = fields;
        container.innerHTML = '';

        fields.forEach(f => {
            const formGroup = document.createElement('div');
            formGroup.className = 'row align-items-center mb-3';
            // ... (Tạo label và các element khác)
            const label = document.createElement('label');
            label.className = 'col-sm-4 col-form-label';
            label.textContent = f.displayName + (f.isRequired && !readOnly ? ' *' : '');
            label.setAttribute('for', f.fieldName);
            formGroup.appendChild(label);

            const colInput = document.createElement('div');
            colInput.className = 'col-sm-8';
            let input;
            const isReadOnly = readOnly === true;

            if (f.dataType === 'TEXTAREA') {
                input = document.createElement('textarea');
                input.className = 'form-control';
                input.rows = 2;
            } else {
                input = document.createElement('input');
                input.className = 'form-control';
                input.type = 'text'; // Luôn dùng type=text để kiểm soát định dạng
            }

            input.name = `FormValues[${f.fieldName}]`;
            input.id = f.fieldName;

            if (isReadOnly) {
                input.readOnly = true;
                input.classList.add('bg-light');
            } else {
                if (f.isRequired) input.required = true;

                if (f.dataType === 'NUMBER') {
                    input.inputMode = 'decimal';
                    input.placeholder = "Ví dụ: 12345.67";
                    // THAY ĐỔI: Đơn giản hóa logic nhập liệu số
                    input.addEventListener('input', function() {
                        // Tự động thay thế dấu phẩy bằng dấu chấm
                        let value = this.value.replace(/,/g, '.');
                        // Chỉ cho phép số và một dấu chấm duy nhất
                        const match = value.match(/^-?\d*\.?\d*/);
                        this.value = match ? match[0] : '';
                        // Cập nhật preview real-time
                        utils.updatePreviewField(f.fieldName, this.value, f.dataType);
                    });
                } else if (f.dataType === 'DATE') {
                    input.setAttribute('data-bs-toggle', 'datepicker');
                    input.placeholder = 'dd/MM/yyyy';
                    input.autocomplete = 'off';
                    // Đổi từ 'input' sang 'blur' để lấy giá trị chuẩn sau khi người dùng thoát khỏi ô nhập
                    input.addEventListener('blur', (e) => utils.updatePreviewField(f.fieldName, e.target.value, f.dataType));
                } else {
                     input.addEventListener('input', (e) => utils.updatePreviewField(f.fieldName, e.target.value, f.dataType));
                }
            }

            if (f.dataSourceType === 'CIF') input.dataset.source = 'CIF';

            if (f.fieldName.toLowerCase() === 'socif' && !isReadOnly) {
                // ... (Logic nút tìm CIF không đổi)
                const group = document.createElement('div');
                group.className = 'input-group';
                group.appendChild(input);
                const searchBtn = document.createElement('button');
                searchBtn.type = 'button';
                searchBtn.className = 'btn btn-outline-secondary';
                searchBtn.innerHTML = '<i class="ti ti-database-search"></i>&nbsp;Tìm';
                group.appendChild(searchBtn);
                colInput.appendChild(group);
                searchBtn.addEventListener('click', async () => {
                    const cif = input.value.trim();
                    if (!cif) return;

                    try {
                        const res = await fetch(`/api/cif/${cif}`);
                        const payload = await res.json();

                        if (!res.ok || payload.exists === false) {
                            await utils.showConfirmation({
                                title: 'Kết quả tìm kiếm',
                                message: payload.message || 'Không tìm thấy CIF.',
                                showCancel: false,
                                status: 'bg-warning',
                                icon: 'ti ti-database-x text-warning',
                                confirmText: 'Đã hiểu'
                            });
                            return;
                        }

                        const data = payload.data || {};
                        const soCif = data.cif || cif;
                        const tenCif = data.tenCif || 'Không rõ tên';
                        const ngayCapNhatRaw = data.ngayCapNhatDuLieu || '';
                        let ngayCapNhatStr = 'không xác định';

                        if (ngayCapNhatRaw) {
                            const d = new Date(ngayCapNhatRaw);
                            if (!isNaN(d)) {
                                const pad = n => n.toString().padStart(2, '0');
                                ngayCapNhatStr = `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
                            }
                        }

                        const confirm = await utils.showConfirmation({
                            title: 'Kết quả tìm kiếm',
                            message: `Đã tìm thấy CIF: <b>${soCif}</b> - <b>${tenCif}</b><br>Thời gian cập nhật: <b>${ngayCapNhatStr}</b><br><br>Bạn có muốn áp dụng thông tin này vào các trường liên quan không?`,
                            icon: 'ti ti-database-search text-success mb-2',
                            status: 'bg-success', showCancel: true,
                            confirmText: 'Đồng ý',
                            cancelText: 'Không',
                            confirmBtnClass: 'btn btn-success w-100',
                            cancelBtnClass: 'btn btn-secondary w-100'
                        });

                        if (!confirm) return;

                        // Áp dụng dữ liệu nếu người dùng đồng ý
                        document.querySelectorAll('[data-source="CIF"]').forEach(el => {
                            const fieldId = el.id;
                            for (const [key, value] of Object.entries(data)) {
                                if (fieldId.toLowerCase() === key.toLowerCase()) {
                                    el.value = value;
                                    el.dispatchEvent(new Event('input', { bubbles: true }));
                                }
                            }
                        });

                    } catch (err) {
                        await utils.showConfirmation({
                            title: 'Lỗi',
                            message: 'Lỗi tra cứu CIF.',
                            showCancel: false,
                            status: 'bg-danger',
                            icon: 'ti ti-x text-danger',
                            confirmText: 'Đã hiểu'
                        });
                    }
                });

            } else {
                colInput.appendChild(input);
            }

            formGroup.appendChild(colInput);
            container.appendChild(formGroup);

            if (f.dataType === 'DATE' && !isReadOnly) {
                utils.initDatepicker($(input));
            }

            // Điền dữ liệu ban đầu
            if (initialData[f.fieldName] !== null && initialData[f.fieldName] !== undefined) {
                let initVal = initialData[f.fieldName];
                if (f.dataType === 'DATE' && /^\d{4}-\d{2}-\d{2}/.test(initVal)) {
                    const [y, m, d] = initVal.split('T')[0].split('-');
                    input.value = `${d}/${m}/${y}`;
                }
                // THAY ĐỔI: Định dạng số cho trường read-only theo chuẩn en-US
                else if (isReadOnly && f.dataType === 'NUMBER') {
                    const num = Number(initVal);
                    input.value = !isNaN(num) ? num.toLocaleString('en-US', { maximumFractionDigits: 6 }) : initVal;
                }
                else {
                    input.value = initVal;
                }
                if (!isReadOnly && $(input).data('datepicker')) {
                    $(input).datepicker('update', input.value);
                }
            }
        });
    };

    // Hàm loadDocxPreview đã được loại bỏ vì không còn cần thiết với HTML preview từ server

}(window.CTOM.formUtils = window.CTOM.formUtils || {}));
