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
     * Tìm kiếm thông tin CIF và áp dụng vào form
     * @param {string} cif - Số CIF cần tìm
     * @param {object} options - Các tùy chọn
     * @param {boolean} [options.showSuccessMessage=true] - Hiển thị thông báo thành công
     * @returns {Promise<boolean>} - Trả về true nếu áp dụng thành công
     */
    utils.searchAndApplyCif = async function(cif, options = {}) {
        const { showSuccessMessage = true } = options;
        let loadingModal = null;
        
        try {
            if (!cif) {
                await utils.showConfirmation({
                    title: 'Thông báo',
                    message: 'Vui lòng nhập số CIF cần tìm kiếm',
                    showCancel: false,
                    status: 'bg-warning',
                    icon: 'ti ti-alert-triangle text-warning',
                    confirmText: 'Đã hiểu'
                });
                return false;
            }

            // Kiểm tra nếu đang có modal nào đang mở thì đóng lại
            const existingModals = document.querySelectorAll('.modal.show');
            existingModals.forEach(modal => {
                const bsModal = bootstrap.Modal.getInstance(modal);
                if (bsModal) bsModal.hide();
            });

            // Hiển thị loading
            loadingModal = await utils.showLoading('Đang tìm kiếm thông tin CIF...');
            
            try {
                const res = await fetch(`${window.appBaseUrl || '/'}api/cif/${cif}`);
                const payload = await res.json();

                // Đóng loading trước khi hiển thị modal xác nhận
                if (loadingModal) {
                    loadingModal.hide();
                    loadingModal = null;
                }

                if (!res.ok || payload.exists === false) {
                    const goCreate = await utils.showConfirmation({
                        title: 'Kết quả tìm kiếm',
                        message: payload.message || 'Không tìm thấy thông tin CIF',
                        showCancel: true,
                        status: 'bg-warning',
                        icon: 'ti ti-database-x text-warning',
                        confirmIcon: 'ti ti-plus',
                        confirmText: 'Khai báo CIF mới',
                        cancelText: 'Đóng',
                        confirmBtnClass: 'btn btn-primary w-100',
                        cancelBtnClass: 'btn btn-secondary w-100'
                    });

                    if (goCreate) {
                        utils.openAjaxFormModal({
                            url: `${window.appBaseUrl || '/'}KhachHangDN/CreatePartial`,
                            postUrl: `${window.appBaseUrl || '/'}KhachHangDN/Create`,
                            title: 'Thêm mới Khách hàng Doanh nghiệp',
                            size: 'xl'
                        });
                    }
                    return false;
                }

                const data = payload.data || {};
                const tenCif = data.tenCif || 'Không rõ tên';
                const ngayCapNhatRaw = data.ngayCapNhatDuLieu || '';
                let ngayCapNhatStr = 'không xác định';
                
                // Định dạng ngày cập nhật nếu có
                if (ngayCapNhatRaw) {
                    try {
                        const ngayCapNhat = new Date(ngayCapNhatRaw);
                        if (!isNaN(ngayCapNhat.getTime())) {
                            ngayCapNhatStr = ngayCapNhat.toLocaleDateString('vi-VN') + ' ' + 
                                           ngayCapNhat.toLocaleTimeString('vi-VN', {hour: '2-digit', minute:'2-digit'});
                        }
                    } catch (e) {
                        console.warn('Lỗi định dạng ngày cập nhật:', e);
                    }
                }
                
                // Kiểm tra xem có trường nào đã có dữ liệu chưa
                let hasExistingData = false;
                const cifFields = document.querySelectorAll('[data-source="CIF"]');
                
                cifFields.forEach(field => {
                    if (field.value && field.value.trim() !== '') {
                        hasExistingData = true;
                    }
                });

                // Hiển thị thông tin CIF và xác nhận trước khi áp dụng
                const confirmApply = await utils.showConfirmation({
                    title: 'Kết quả tìm kiếm',
                    message: `Đã tìm thấy CIF: <b>${cif}</b> - <b>${tenCif}</b><br>Thời gian cập nhật: <b>${ngayCapNhatStr}</b><br><br>` +
                             'Bạn có muốn áp dụng thông tin này vào các trường liên quan không?<br><br>' +
                             '<span class="text-warning">Lưu ý: Các trường đã có dữ liệu sẽ bị ghi đè.</span>',
                             //(hasExistingData ? '<span class="text-warning">Lưu ý: Một số trường đã có dữ liệu sẽ bị ghi đè.</span>' : ''),
                    showCancel: true,
                    status: 'bg-success',
                    icon: 'ti ti-database-search text-success mb-2',
                    confirmText: 'Đồng ý',
                    cancelText: 'Hủy bỏ',
                    confirmBtnClass: 'btn btn-success w-100',
                    cancelBtnClass: 'btn btn-secondary w-100'
                });

                if (!confirmApply) {
                    return false; // Người dùng không muốn áp dụng
                }

                // Áp dụng dữ liệu CIF vào các trường
                let updatedFieldCount = 0;
                cifFields.forEach(field => {
                    const fieldId = field.id.toLowerCase();
                    for (const [key, value] of Object.entries(data)) {
                        if (fieldId === key.toLowerCase()) {
                            field.value = value;
                            field.dispatchEvent(new Event('input', { bubbles: true }));
                            updatedFieldCount++;
                            //console.log(`Updated field: ${fieldId} with value: ${value}`);
                            break;
                        }
                    }
                });

                // Hiển thị thông báo thành công nếu cần
                if (showSuccessMessage) {
                    await utils.showConfirmation({
                        title: 'Thành công',
                        message: `Đã cập nhật <b>${updatedFieldCount}</b> trường từ thông tin CIF.`,
                        showCancel: false,
                        status: 'bg-success',
                        icon: 'ti ti-check text-success',
                        confirmText: 'Đã hiểu'
                    });
                }

                return true;
                
            } catch (error) {
                // Đóng loading nếu có lỗi
                if (loadingModal) loadingModal.hide();
                throw error; // Ném lỗi để xử lý ở catch bên ngoài
            }
            
        } catch (error) {
            console.error('Lỗi khi tìm kiếm CIF:', error);
            await utils.showConfirmation({
                title: 'Lỗi',
                message: 'Đã xảy ra lỗi khi tìm kiếm thông tin CIF. Vui lòng thử lại sau.',
                showCancel: false,
                status: 'bg-danger',
                icon: 'ti ti-x text-danger',
                confirmText: 'Đã hiểu'
            });
            return false;
        }

        try {
            // Kiểm tra nếu đang có modal nào đang mở thì đóng lại
            const existingModals = document.querySelectorAll('.modal.show');
            existingModals.forEach(modal => {
                const bsModal = bootstrap.Modal.getInstance(modal);
                if (bsModal) bsModal.hide();
            });

            // Hiển thị loading
            const loadingModal = await utils.showLoading('Đang tìm kiếm thông tin CIF...');
            
            try {
                const res = await fetch(`${window.appBaseUrl || '/'}api/cif/${cif}`);
                const payload = await res.json();

                // Đóng loading
                if (loadingModal) loadingModal.hide();

                if (!res.ok || payload.exists === false) {
                    const goCreate = await utils.showConfirmation({
                        title: 'Kết quả tìm kiếm',
                        message: payload.message || 'Không tìm thấy thông tin CIF',
                        showCancel: true,
                        status: 'bg-warning',
                        icon: 'ti ti-database-x text-warning',
                        confirmIcon: 'ti ti-plus',
                        confirmText: 'Khai báo CIF mới',
                        cancelText: 'Đóng',
                        confirmBtnClass: 'btn btn-primary w-100',
                        cancelBtnClass: 'btn btn-secondary w-100'
                    });

                    if (goCreate) {
                        utils.openAjaxFormModal({
                            url: `${window.appBaseUrl || '/'}KhachHangDN/CreatePartial`,
                            postUrl: `${window.appBaseUrl || '/'}KhachHangDN/Create`,
                            title: 'Thêm mới Khách hàng Doanh nghiệp',
                            size: 'xl'
                        });
                    }
                    return false;
                }

                const data = payload.data || {};
                
                // Kiểm tra xem có trường nào đã có dữ liệu chưa
                let hasExistingData = false;
                const cifFields = document.querySelectorAll('[data-source="CIF"]');
                
                cifFields.forEach(field => {
                    if (field.value && field.value.trim() !== '') {
                        hasExistingData = true;
                    }
                });

                // Luôn hiển thị xác nhận trước khi áp dụng dữ liệu CIF
                const confirmApply = await utils.showConfirmation({
                    title: 'Xác nhận áp dụng thông tin CIF',
                    message: `Đã tìm thấy CIF: <b>${cif}</b> - <b>${tenCif}</b><br>Thời gian cập nhật: <b>${ngayCapNhatStr}</b><br><br>` +
                             'Bạn có muốn áp dụng thông tin này vào các trường liên quan không?<br><br>' +
                             (hasExistingData ? '<span class="text-warning">Lưu ý: Một số trường đã có dữ liệu sẽ bị ghi đè.</span>' : ''),
                    showCancel: true,
                    status: hasExistingData ? 'bg-warning' : 'bg-info',
                    icon: hasExistingData ? 'ti ti-alert-triangle text-warning' : 'ti ti-info-circle text-info',
                    confirmText: 'Đồng ý',
                    cancelText: 'Hủy bỏ',
                    confirmBtnClass: 'btn btn-primary w-100',
                    cancelBtnClass: 'btn btn-secondary w-100'
                });

                if (!confirmApply) {
                    return false; // Người dùng không muốn áp dụng
                }

                // Áp dụng dữ liệu CIF vào các trường
                let updatedFieldCount = 0;
                cifFields.forEach(field => {
                    const fieldId = field.id.toLowerCase();
                    for (const [key, value] of Object.entries(data)) {
                        if (fieldId === key.toLowerCase()) {
                            field.value = value;
                            field.dispatchEvent(new Event('input', { bubbles: true }));
                            updatedFieldCount++;
                            break;
                        }
                    }
                });

                // Hiển thị thông báo thành công nếu cần
                if (showSuccessMessage) {
                    await utils.showConfirmation({
                        title: 'Thành công',
                        message: `Đã cập nhật <b>${updatedFieldCount}</b> trường từ thông tin CIF.`,
                        showCancel: false,
                        status: 'bg-success',
                        icon: 'ti ti-check text-success',
                        confirmText: 'Đã hiểu'
                    });
                }

                return true;
                
            } catch (error) {
                // Đóng loading nếu có lỗi
                if (loadingModal) loadingModal.hide();
                throw error; // Ném lỗi để xử lý ở catch bên ngoài
            }
            
        } catch (error) {
            console.error('Lỗi khi tìm kiếm CIF:', error);
            await utils.showConfirmation({
                title: 'Lỗi',
                message: 'Đã xảy ra lỗi khi tìm kiếm thông tin CIF. Vui lòng thử lại sau.',
                showCancel: false,
                status: 'bg-danger',
                icon: 'ti ti-x text-danger',
                confirmText: 'Đã hiểu'
            });
            return false;
        }
    },

    /**
     * Getter để lấy danh sách các trường hiện tại.
     * @returns {Array} Mảng định nghĩa các trường.
     */
    utils.getCurrentFields = function() {
        return currentFields;
    };

    // --- CÁC HÀM TIỆN ÍCH (HELPER FUNCTIONS) ---

    /**
     * Bật/tắt trạng thái đang xử lý cho một nút bất kỳ (spinner + disable)
     * @param {boolean} isProcessing - true để bật spinner, false để khôi phục
     * @param {HTMLButtonElement} button - Nút mục tiêu
     * @param {{ text?: string, iconHtml?: string }} [options]
     */
    utils.setProcessingState = function(isProcessing, button, options = {}) {
        if (!button) return;
        const text = options.text ?? 'Đang xử lý...';
        const iconHtml = options.iconHtml ?? '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>';

        if (isProcessing) {
            button.dataset.originalHtml = button.innerHTML;
            button.innerHTML = `${iconHtml}${text}`;
            button.disabled = true;
        } else {
            if (button.dataset.originalHtml) button.innerHTML = button.dataset.originalHtml;
            button.disabled = false;
        }
    };

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
            //console.log(`Đã điều chỉnh chiều cao preview-pane-body: ${previewBodyHeight}px`);
        }

        //console.log(`Đã đồng bộ chiều cao: ${optimalHeight}px (form: ${formHeight}px, min: ${minHeight}px)`);
    };

    /**
     * Tối ưu hiển thị HTML preview để phù hợp với không gian hiển thị
     * Dựa trên logic của hàm fitDocxToPane gốc nhưng được điều chỉnh cho HTML preview
     */
    utils.fitHtmlToPane = function() {
        // KHÔNG scale nữa để tránh bị thu nhỏ và tạo khoảng trắng hai bên.
        const wrapper = document.getElementById('htmlPreviewContainer');
        const htmlPreview = document.getElementById('html-preview');
        if (!wrapper || !htmlPreview) return;

        // Reset mọi transform/margin do lần trước (nếu có)
        htmlPreview.style.transform = 'none';
        htmlPreview.style.transformOrigin = '';
        htmlPreview.style.margin = '0';
        htmlPreview.style.width = '100%';

        // Dựa hoàn toàn vào CSS: .docx-html { width:794px; max-width:100%; }
        // Điều này cho phép nội dung tự co theo khung mà không cần scale thủ công.
    };

    // Hàm hiển thị loading
    utils.showLoading = function(message = 'Đang xử lý...') {
        // Tạo ID duy nhất cho mỗi lần gọi
        const loadingId = 'loading_' + Date.now();
        
        // Tạo modal loading mới
        const modal = document.createElement('div');
        modal.id = loadingId;
        modal.className = 'modal';
        modal.style.display = 'block';
        modal.style.backgroundColor = 'rgba(0,0,0,0.5)';
        modal.style.position = 'fixed';
        modal.style.top = '0';
        modal.style.left = '0';
        modal.style.width = '100%';
        modal.style.height = '100%';
        modal.style.zIndex = '9999';
        
        // Tạo nội dung modal
        modal.innerHTML = `
            <div style="position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%);">
                <div class="card">
                    <div class="card-body text-center p-4">
                        <div class="spinner-border text-primary mb-3" role="status">
                            <span class="visually-hidden">Loading...</span>
                        </div>
                        <p class="mb-0">${message}</p>
                    </div>
                </div>
            </div>
        `;
        
        // Thêm modal vào body
        document.body.appendChild(modal);
        
        // Trả về đối tượng để đóng modal
        return {
            hide: function() {
                const element = document.getElementById(loadingId);
                if (element) {
                    element.style.opacity = '0';
                    setTimeout(() => {
                        element.remove();
                    }, 300);
                }
            }
        };
    };

    utils.showConfirmation = async function(options) {
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

            // Sử dụng options thay vì config
            modal.title.textContent = options.title || 'Xác nhận';
            modal.message.innerHTML = options.message || '';
            modal.icon.className = options.icon || 'ti ti-help-circle';
            modal.status.className = `modal-status ${options.status || 'bg-primary'}`;

            // Xử lý nút xác nhận
            modal.confirmBtn.innerHTML = `<i class="${options.confirmIcon || 'ti ti-check'} me-2"></i> ${options.confirmText || 'Đồng ý'}`;
            modal.confirmBtn.className = options.confirmBtnClass || 'btn btn-primary';

            // Xử lý nút hủy
            modal.cancelBtn.innerHTML = `<i class="${options.cancelIcon || 'ti ti-x'} me-2"></i> ${options.cancelText || 'Hủy'}`;
            modal.cancelBtn.className = options.cancelBtnClass || 'btn btn-secondary';
            modal.cancelBtn.style.display = options.showCancel === false ? 'none' : 'block';

            const colConfirm = modal.confirmBtn.parentElement;
            const colCancel = modal.cancelBtn.parentElement;
            if (options.showCancel === false) {
                colConfirm.classList.remove('col');
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


    // MỞ PARTIAL VIEW TRONG MODAL VÀ SUBMIT FORM QUA AJAX
    utils.openAjaxFormModal = function(options) {
        const { url, postUrl, title = '', size = 'xl', onSuccess } = options || {};
        if (!url) return;

        const modalId = `ajaxFormModal_${Date.now()}`;
        const wrapper = document.createElement('div');
        wrapper.innerHTML = `
            <div class="modal modal-blur fade" id="${modalId}" tabindex="-1">
              <div class="modal-dialog modal-${size} modal-dialog-centered modal-dialog-scrollable">
                <div class="modal-content">
                  <div class="modal-header py-2">
                    <h5 class="modal-title">${title}</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                  </div>
                  <div class="modal-body">
                    <div class="d-flex align-items-center text-muted py-5" id="${modalId}_loading">
                      <span class="spinner-border me-2" role="status" aria-hidden="true"></span>
                      Đang tải...
                    </div>
                    <div id="${modalId}_content" class="d-none"></div>
                  </div>
                </div>
              </div>
            </div>`;

        const modalEl = wrapper.firstElementChild;
        document.body.appendChild(modalEl);
        const bsModal = new bootstrap.Modal(modalEl);
        modalEl.addEventListener('hidden.bs.modal', () => modalEl.remove(), { once: true });
        bsModal.show();

        const contentEl = modalEl.querySelector(`#${modalId}_content`);
        const loadingEl = modalEl.querySelector(`#${modalId}_loading`);

        // Load partial
        fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(res => res.text())
            .then(html => {
                // Ẩn loader đúng cách khi .d-flex có !important
                loadingEl.classList.add('d-none');
                contentEl.classList.remove('d-none');
                contentEl.innerHTML = html;

                // Lấy form trước khi dùng ở các khối jQuery Validate
                const form = contentEl.querySelector('form#khachHangForm');
                if (!form) return;

                // Kích hoạt unobtrusive validation nếu có
                if (window.jQuery && $.validator && $.validator.unobtrusive) {
                    $.validator.unobtrusive.parse(contentEl);
                }

                // Ghi đè rule date của jQuery Validate cho dd/MM/yyyy (chỉ làm 1 lần)
                if (window.jQuery && $.validator && !window.__ctom_date_validator_patched__) {
                    const parseVnDate = (val) => {
                        if (!val) return null;
                        const m = String(val).match(/^\s*(\d{1,2})\/(\d{1,2})\/(\d{4})\s*$/);
                        if (!m) return null;
                        const d = parseInt(m[1], 10), mo = parseInt(m[2], 10) - 1, y = parseInt(m[3], 10);
                        const dt = new Date(y, mo, d);
                        return (dt.getFullYear() === y && dt.getMonth() === mo && dt.getDate() === d) ? dt : null;
                    };
                    const origDateMethod = $.validator.methods.date;
                    $.validator.methods.date = function(value, element) {
                        if (this.optional(element)) return true;
                        return !!parseVnDate(value) || (typeof origDateMethod === 'function' && origDateMethod.call(this, value, element));
                    };
                    window.__ctom_date_validator_patched__ = true;
                }

                // Khởi tạo datepicker cho các input trong modal (đọc start/end date)
                function initDatepicker($el, options) {
                    const defaultOptions = {
                        format: 'dd/mm/yyyy',
                        autoclose: true,
                        todayHighlight: true,
                        language: 'vi',
                        orientation: 'bottom auto',
                        clearBtn: true,
                        todayBtn: 'linked',
                        zIndexOffset: 1056
                    };
                    const finalOptions = Object.assign({}, defaultOptions, options || {});
                    $el.datepicker(finalOptions);
                    $el.addClass('datepicker-initialized');
                }
                if (window.jQuery && typeof $().datepicker === 'function') {
                    $(contentEl).find('input[data-bs-toggle="datepicker"]').each(function() {
                        const $this = $(this);
                        let startDate = $this.data('date-start-date');
                        let endDate = $this.data('date-end-date');
                        // Chuyển '-50y', '+50y', '0d'... thành Date
                        const toDate = (expr) => {
                            if (!expr) return undefined;
                            const s = String(expr);
                            const now = new Date();
                            if (s === '0d') return new Date();
                            const m = s.match(/^([+-]?)(\d+)([yd])$/i);
                            if (m) {
                                const sign = m[1] === '-' ? -1 : 1;
                                const amount = parseInt(m[2], 10) * sign;
                                const unit = m[3].toLowerCase();
                                const d = new Date(now);
                                if (unit === 'y') d.setFullYear(d.getFullYear() + amount);
                                else if (unit === 'd') d.setDate(d.getDate() + amount);
                                return d;
                            }
                            return expr; // có thể là Date trực tiếp
                        };
                        startDate = toDate(startDate);
                        endDate = toDate(endDate);
                        const options = {};
                        if (startDate) options.startDate = startDate;
                        if (endDate) options.endDate = endDate;
                        initDatepicker($this, options);
                    });
                }

                // Khởi tạo jQuery Validate cho form trong modal giống trang Create
                if (window.jQuery && $.fn.validate) {
                    const $form = $(form);
                    $form.validate({
                        errorClass: 'is-invalid',
                        validClass: 'is-valid',
                        errorElement: 'div',
                        errorPlacement: function(error, element) {
                            error.addClass('invalid-feedback');
                            if (element.parent().hasClass('input-group')) {
                                error.insertAfter(element.parent());
                            } else if (element.prop('type') === 'checkbox' || element.prop('type') === 'radio') {
                                error.insertAfter(element.closest('.form-check').children().last());
                            } else {
                                error.insertAfter(element);
                            }
                        },
                        highlight: function(element, errorClass, validClass) {
                            $(element).addClass(errorClass).removeClass(validClass);
                        },
                        unhighlight: function(element, errorClass, validClass) {
                            $(element).removeClass(errorClass).addClass(validClass);
                        },
                        ignore: [],
                    });
                }

                // Ngăn chuyển tab nếu tab hiện tại còn lỗi required (giống trang Create)
                if (window.jQuery) {
                    const $khachHangTabs = $(contentEl).find('#khachHangTabs');
                    $khachHangTabs.on('show.bs.tab', 'button[data-bs-toggle="tab"]', function(e) {
                        const $tabButtonBeingLeft = $(e.relatedTarget);
                        if (!$tabButtonBeingLeft.length) return; // tab đầu tiên
                        const $paneBeingLeft = $($tabButtonBeingLeft.data('bs-target'));
                        if ($paneBeingLeft.length) {
                            let paneIsValid = true;
                            let $firstInvalid = null;
                            $paneBeingLeft.find(':input[name]').each(function() {
                                const $input = $(this);
                                if ($input.is('[data-val="true"]')) {
                                    if (!$input.valid()) {
                                        paneIsValid = false;
                                        if (!$firstInvalid) $firstInvalid = $input;
                                    }
                                }
                            });
                            if (!paneIsValid) {
                                e.preventDefault();
                                if ($firstInvalid) setTimeout(() => $firstInvalid.focus(), 0);
                            }
                        }
                    });
                }
                form.addEventListener('submit', async (e) => {
                    e.preventDefault();
                    const btn = form.querySelector('#saveBtn');
                    try {
                        utils.setProcessingState(true, btn, { text: 'Đang lưu...' });

                        // Clear errors
                        contentEl.querySelectorAll('[data-valmsg-for]').forEach(el => el.textContent = '');
                        const summary = contentEl.querySelector('[data-valmsg-summary], [asp-validation-summary]');
                        if (summary) summary.innerHTML = '';

                        // Kiểm tra trùng CIF như trang Create: nếu tồn tại thì không cho lưu
                        const soCifInput = form.querySelector('#SoCif, [name="SoCif"]');
                        if (soCifInput && soCifInput.value && soCifInput.value.trim().length > 0) {
                            const cif = soCifInput.value.trim();
                            try {
                                const resCheck = await fetch(`${baseUrl}api/cif/${encodeURIComponent(cif)}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                                const payloadCheck = await resCheck.json().catch(() => ({}));
                                if (payloadCheck && payloadCheck.exists === true) {
                                    // Đặt thông báo lỗi cho SoCif và focus
                                    const msgEl = contentEl.querySelector('[data-valmsg-for="SoCif"]');
                                    if (msgEl) msgEl.textContent = `Số CIF '${cif}' đã tồn tại trong hệ thống.`;
                                    // Chuyển về tab cơ bản để hiển thị cảnh báo ngay
                                    const basicTabBtn = contentEl.querySelector('#basic-tab');
                                    if (basicTabBtn && window.bootstrap?.Tab) {
                                        const tab = bootstrap.Tab.getOrCreateInstance(basicTabBtn);
                                        tab.show();
                                    }
                                    soCifInput.classList.add('is-invalid');
                                    setTimeout(() => soCifInput.focus(), 0);
                                    return; // dừng submit
                                }
                            } catch (ex) {
                                console.warn('Không kiểm tra được trạng thái CIF, tiếp tục submit mặc định.', ex);
                            }
                        }

                        // Chuẩn hóa giá trị ngày: dd/MM/yyyy -> yyyy-MM-dd (giống Create.cshtml)
                        form.querySelectorAll('input[data-bs-toggle="datepicker"]').forEach(inp => {
                            const val = inp.value?.trim();
                            if (val && /^\d{2}\/\d{2}\/\d{4}$/.test(val)) {
                                const [dd, mm, yyyy] = val.split('/');
                                inp.value = `${yyyy}-${mm}-${dd}`;
                            }
                        });

                        const formData = new FormData(form);
                        const tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
                        const headers = {
                            'X-Requested-With': 'XMLHttpRequest'
                        };
                        if (tokenInput) headers['RequestVerificationToken'] = tokenInput.value;

                        const res = await fetch(postUrl || form.action, {
                            method: 'POST',
                            headers,
                            body: formData
                        });

                        const payload = await res.json().catch(() => ({}));
                        if (!res.ok || !payload.success) {
                            // Hiển thị lỗi validation nếu có
                            if (payload.errors) {
                                Object.keys(payload.errors).forEach(field => {
                                    const messages = payload.errors[field] || [];
                                    const msgEl = contentEl.querySelector(`[data-valmsg-for="${CSS.escape(field)}"]`);
                                    if (msgEl) msgEl.textContent = messages.join(' ');
                                });
                            }
                            // Thông điệp chung
                            const sum = contentEl.querySelector('.validation-summary-errors, [asp-validation-summary]');
                            if (sum && payload.message) {
                                sum.innerHTML = `<div class="text-danger">${payload.message}</div>`;
                            }
                            return;
                        }

                        // Thành công: đóng modal và cập nhật SoCif ở trang cha
                        bsModal.hide();
                        const data = payload.data || {};
                        const socifInput = document.getElementById('SoCif');
                        if (socifInput && data.soCif) {
                            socifInput.value = data.soCif;
                            socifInput.dispatchEvent(new Event('input', { bubbles: true }));
                            const searchBtn = socifInput.parentElement?.querySelector('button.btn');
                            if (searchBtn) searchBtn.click();
                        }
                        if (typeof onSuccess === 'function') onSuccess(payload);
                    } catch (err) {
                        console.error('Lỗi submit form CIF:', err);
                    } finally {
                        utils.setProcessingState(false, btn);
                    }
                }, { once: false });
            })
            .catch(err => {
                loadingEl.innerHTML = '<div class="text-danger">Không thể tải biểu mẫu.</div>';
                console.error('Không thể tải partial:', err);
            });

        return bsModal;
    };

    // Đã loại bỏ luồng iFrame và postMessage 'cifCreated'.

    // --- CÁC HÀM XỬ LÝ CIF SEARCH MODAL ---

    /**
     * Khởi tạo modal tìm kiếm CIF
     * @param {string} modalId - ID của modal (mặc định: 'searchCifModal')
     * @param {string} searchInputId - ID của input tìm kiếm (mặc định: 'cifSearchInput')
     * @param {string} searchButtonId - ID của nút tìm kiếm (mặc định: 'btnSearchCif')
     * @param {string} resultContainerId - ID của container kết quả (mặc định: 'cifSearchResult')
     * @returns {Object} Đối tượng chứa các phương thức điều khiển modal
     */
    utils.initCifSearchModal = function(modalId = 'searchCifModal', searchInputId = 'cifSearchInput', 
                                      searchButtonId = 'btnSearchCif', resultContainerId = 'cifSearchResult') {
        const searchCifModal = document.getElementById(modalId);
        const cifSearchInput = document.getElementById(searchInputId);
        const btnSearchCif = document.getElementById(searchButtonId);
        const cifSearchResult = document.getElementById(resultContainerId);
        
        if (!searchCifModal) {
            console.error('Không tìm thấy modal tìm kiếm CIF');
            return null;
        }

        const modal = new bootstrap.Modal(searchCifModal);
        
        // Xử lý tìm kiếm CIF
        async function handleSearchCif() {
            const cif = cifSearchInput?.value.trim() || '';
            if (!cif) {
                await utils.showConfirmation({
                    title: 'Thông báo',
                    message: 'Vui lòng nhập số CIF cần tìm kiếm',
                    showCancel: false,
                    status: 'bg-warning',
                    icon: 'ti ti-alert-triangle text-warning',
                    confirmText: 'Đã hiểu'
                });
                return;
            }
            
            try {
                // Gọi hàm searchAndApplyCif để xử lý tìm kiếm và áp dụng dữ liệu
                const success = await utils.searchAndApplyCif(cif);
                
                // Nếu tìm thấy và áp dụng thành công, đóng modal
                if (success) {
                    modal.hide();
                }
            } catch (error) {
                console.error('Lỗi khi tìm kiếm CIF:', error);
                // Lỗi đã được xử lý trong hàm searchAndApplyCif
            }
        }

        // Bắt sự kiện nhấn nút tìm kiếm
        if (btnSearchCif) {
            btnSearchCif.addEventListener('click', handleSearchCif);
        }
        
        // Bắt sự kiện nhấn Enter trong ô tìm kiếm
        if (cifSearchInput) {
            cifSearchInput.addEventListener('keypress', function(e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    handleSearchCif();
                }
            });
        }
        
        // Đặt focus vào ô tìm kiếm khi mở modal
        searchCifModal.addEventListener('shown.bs.modal', function() {
            if (cifSearchInput) cifSearchInput.focus();
        });
        
        // Reset modal khi đóng
        searchCifModal.addEventListener('hidden.bs.modal', function() {
            if (cifSearchInput) cifSearchInput.value = '';
            if (cifSearchResult) cifSearchResult.innerHTML = '';
        });

        return {
            show: () => modal.show(),
            hide: () => modal.hide(),
            setSearchValue: (value) => {
                if (cifSearchInput) cifSearchInput.value = value;
            }
        };
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

        //console.log('Bắt đầu thiết lập interactive placeholders');
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
                const group = document.createElement('div');
                group.className = 'input-group';
                group.appendChild(input);
                
                // Thêm tooltip cho trường SoCif
                //input.setAttribute('data-bs-toggle', 'tooltip');
                //input.setAttribute('title', 'Nhập số CIF và nhấn Tab hoặc Enter để tìm kiếm');
                
                // // Thêm nút thông tin
                // const infoBtn = document.createElement('button');
                // infoBtn.type = 'button';
                // infoBtn.className = 'btn btn-outline-info';
                // infoBtn.title = 'Hướng dẫn sử dụng trường CIF';
                // infoBtn.innerHTML = '<i class="ti ti-info-circle"></i>';
                // infoBtn.addEventListener('click', (e) => {
                //     e.stopPropagation();
                //     utils.showConfirmation({
                //         title: 'Hướng dẫn sử dụng trường CIF',
                //         message: '1. Nhập số CIF và nhấn Tab hoặc Enter để tìm kiếm thông tin tự động<br>2. Hoặc sử dụng nút "Tìm CIF" ở trên cùng để mở cửa sổ tìm kiếm',
                //         showCancel: false,
                //         status: 'bg-info',
                //         icon: 'ti ti-info-circle text-info',
                //         confirmText: 'Đã hiểu',
                //         confirmBtnClass: 'btn btn-info w-100'
                //     });
                // });
                
                // Thêm nút tìm kiếm
                const searchBtn = document.createElement('button');
                searchBtn.type = 'button';
                searchBtn.className = 'btn btn-outline-secondary';
                searchBtn.title = 'Tìm kiếm thông tin CIF';
                searchBtn.innerHTML = '<i class="ti ti-search"></i>';
                searchBtn.addEventListener('click', async () => {
                    const cif = input.value.trim();
                    if (cif) {
                        await utils.searchAndApplyCif(cif);
                    } else {
                        // Nếu chưa nhập CIF, focus vào ô input
                        input.focus();
                    }
                });
                
                // Vô hiệu hóa sự kiện Enter để tránh submit form
                input.addEventListener('keydown', (e) => {
                    if (e.key === 'Enter') {
                        e.preventDefault();
                    }
                });
                
                group.appendChild(searchBtn);
                //group.appendChild(infoBtn);
                colInput.appendChild(group);
                
                // Khởi tạo tooltip
                if (window.bootstrap && window.bootstrap.Tooltip) {
                    new bootstrap.Tooltip(input);
                    new bootstrap.Tooltip(searchBtn);
                    //new bootstrap.Tooltip(infoBtn);
                }

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
