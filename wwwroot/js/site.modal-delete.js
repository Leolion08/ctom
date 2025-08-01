/*
 * Khởi tạo chức năng xóa bằng modal cho một bảng DataTables.
 * @param {object} options Các tùy chọn cấu hình.
 * @param {string} options.modalId ID của phần tử modal (vd: '#deleteConfirmModal').
 * @param {string} options.triggerButtonSelector Selector CSS cho các nút kích hoạt modal xóa (vd: '.delete-confirmation-button').
 * @param {string} options.confirmButtonId ID của nút xác nhận xóa trong modal (vd: '#confirmDeleteButton').
 * @param {string} options.nameElementSelector Selector CSS cho phần tử hiển thị tên item trong modal (vd: '#phongBanNameToDelete').
 * @param {string} options.idElementSelector Selector CSS cho phần tử hiển thị ID item trong modal (vd: '#phongBanIdToDelete') - tùy chọn.
 * @param {string} options.dataIdAttribute Tên thuộc tính data-* trên nút kích hoạt chứa ID cần xóa (vd: 'data-phongban-maPhong' hoặc chuẩn hóa thành 'data-id').
 * @param {string} options.dataNameAttribute Tên thuộc tính data-* trên nút kích hoạt chứa Tên cần hiển thị (vd: 'data-phongban-tenPhong' hoặc chuẩn hóa thành 'data-name').
 * @param {string} options.deleteUrlTemplate Mẫu URL cho action xóa, với placeholder '{id}' (vd: '/PhongBan/Delete/{id}').
 * @param {object} options.dataTableInstance Instance của DataTables đang được sử dụng.
 * @param {string} options.antiForgeryToken Giá trị của AntiForgeryToken.
 * @param {string} [options.entityName='mục'] Tên gọi chung của đối tượng đang xóa (vd: 'phòng ban', 'nhóm quyền') để hiển thị thông báo.
 */
function initializeDeleteModal(options) {
    const modalElement = document.querySelector(options.modalId);
    //console.log('DEBUG: Modal Element Found:', modalElement); // <-- Thêm dòng này (ngay sau khi querySelector)

    const confirmButton = document.getElementById(options.confirmButtonId);
    const modalNameElement = modalElement ? modalElement.querySelector(options.nameElementSelector) : null;
    const modalIdElement = options.idElementSelector ? (modalElement ? modalElement.querySelector(options.idElementSelector) : null) : null;

    let itemIdToDelete = null;
    let triggerButton = null;
    let itemName = ''; // Lưu tên để hiển thị thông báo

    // Câu lệnh IF gây lỗi:
    if (!modalElement || !confirmButton || !options.dataTableInstance || !options.antiForgeryToken) {
        console.error('Modal Delete Initialization Failed: One or more prerequisites missing.', {
            modalElementExists: !!modalElement,
            confirmButtonExists: !!confirmButton,
            dataTableInstanceExists: !!options.dataTableInstance,
            antiForgeryTokenExists: !!options.antiForgeryToken
        });
        console.log('DEBUG: Options object passed:', options);
        return;
    }

    const modalInstance = bootstrap.Modal.getOrCreateInstance(modalElement);

    // Lắng nghe sự kiện click trên các nút xóa (sử dụng event delegation cho hiệu quả)
    document.addEventListener('click', function (event) {
        const targetButton = event.target.closest(options.triggerButtonSelector);
        if (!targetButton) return; // Không phải nút xóa

        triggerButton = targetButton; // Lưu nút đã bấm
        itemIdToDelete = triggerButton.getAttribute(options.dataIdAttribute);
        itemName = triggerButton.getAttribute(options.dataNameAttribute) || itemIdToDelete; // Lấy tên, fallback về ID nếu không có

        if (itemIdToDelete && modalNameElement) {
            modalNameElement.textContent = itemName; // Cập nhật tên trong modal
        }
        if (itemIdToDelete && modalIdElement) {
            modalIdElement.textContent = itemIdToDelete; // Cập nhật ID nếu có
            if (modalIdElement.parentElement) modalIdElement.parentElement.style.display = ''; // Hiển thị lại nếu bị ẩn
        } else if (modalIdElement && modalIdElement.parentElement) {
            modalIdElement.parentElement.style.display = 'none'; // Ẩn nếu không dùng
        }

        // Hiển thị modal xác nhận
        modalInstance.show();

    });

    // Lắng nghe sự kiện khi modal ẩn đi để dọn dẹp backdrop
    modalElement.addEventListener('hidden.bs.modal', cleanupModalBackdrop); // Gọi hàm cleanup từ site.js

    // Lắng nghe sự kiện click nút xác nhận xóa
    confirmButton.addEventListener('click', function () {
        if (!itemIdToDelete) return;

        const finalUrl = options.deleteUrlTemplate.replace('{id}', encodeURIComponent(itemIdToDelete));
        const currentItemName = itemName; // Lưu lại tên để dùng trong thông báo

        fetch(finalUrl, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': options.antiForgeryToken,
                'X-Requested-With': 'XMLHttpRequest',
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        })
        .then(async response => {
            const data = await response.json();

            if (!response.ok) {
                // Lấy thông báo lỗi từ response
                const errorMsg = data.message || `Lỗi HTTP! status: ${response.status}`;
                const error = new Error(errorMsg);
                error.data = data;
                throw error;
            }

            return data;
        })
        .then(data => {
            // Kiểm tra success từ response
            if (data.success) {
                // Xóa dòng khỏi DataTables
                try {
                    options.dataTableInstance.row($(triggerButton).closest('tr')).remove().draw(false);
                } catch (e) {
                    console.error("Lỗi khi xóa dòng khỏi DataTable:", e);
                    // Fallback: reload lại trang nếu cần
                    // location.reload();
                }
                // Hiển thị thông báo thành công từ server hoặc dùng mặc định
                const successMessage = data.message ||
                    `Đã xóa thành công ${options.entityName || 'mục'} '${currentItemName}'.`;
                //showAppNotification(successMessage, 'success');
                showAppNotification(successMessage, 'success', 'notificationPlaceholder', false); // false để tắt auto-hide
            } else {
                // Xử lý lỗi từ server
                const errorMessage = data.message || 'Đã xảy ra lỗi không xác định khi xóa.';
                //showAppNotification(`Lỗi: ${errorMessage}`, 'danger');
                showAppNotification(`Lỗi: ${errorMessage}`, 'danger', 'notificationPlaceholder', false); // false để tắt auto-hide
            }
        })
            .catch(error => {
                console.error('Fetch Error:', error);
                //showAppNotification(`Đã xảy ra lỗi khi gửi yêu cầu xóa. ${error.message}`, 'danger');
                showAppNotification(`Đã xảy ra lỗi khi gửi yêu cầu xóa. ${error.message}`, 'danger', 'notificationPlaceholder', false); // false để tắt auto-hide
            })
            .finally(() => {
                modalInstance.hide(); // Luôn đóng modal
                itemIdToDelete = null; // Reset ID
                triggerButton = null;
                itemName = '';
            });
    });
}

/*
 * Giống initializeDeleteModal cách khai báo
 * Khác: xóa và reload lại datatable thay vì remove dòng trên datatable.
 */
function initializeDataTableDeleteModal(options) {
    const modalElement = document.querySelector(options.modalId);
    //console.log('DEBUG: Modal Element Found:', modalElement); // <-- Thêm dòng này (ngay sau khi querySelector)

    const confirmButton = document.getElementById(options.confirmButtonId);
    const modalNameElement = modalElement ? modalElement.querySelector(options.nameElementSelector) : null;
    const modalIdElement = options.idElementSelector ? (modalElement ? modalElement.querySelector(options.idElementSelector) : null) : null;

    let itemIdToDelete = null;
    let triggerButton = null;
    let itemName = ''; // Lưu tên để hiển thị thông báo

    // Câu lệnh IF gây lỗi:
    if (!modalElement || !confirmButton || !options.dataTableInstance || !options.antiForgeryToken) {
        console.error('Modal Delete Initialization Failed: One or more prerequisites missing.', {
            modalElementExists: !!modalElement,
            confirmButtonExists: !!confirmButton,
            dataTableInstanceExists: !!options.dataTableInstance,
            antiForgeryTokenExists: !!options.antiForgeryToken
        });
        console.log('DEBUG: Options object passed:', options);
        return;
    }

    const modalInstance = bootstrap.Modal.getOrCreateInstance(modalElement);

    // Lắng nghe sự kiện click trên các nút xóa (sử dụng event delegation cho hiệu quả)
    document.addEventListener('click', function (event) {
        const targetButton = event.target.closest(options.triggerButtonSelector);
        if (!targetButton) return; // Không phải nút xóa

        triggerButton = targetButton; // Lưu nút đã bấm
        itemIdToDelete = triggerButton.getAttribute(options.dataIdAttribute);
        itemName = triggerButton.getAttribute(options.dataNameAttribute) || itemIdToDelete; // Lấy tên, fallback về ID nếu không có

        if (itemIdToDelete && modalNameElement) {
            modalNameElement.textContent = itemName; // Cập nhật tên trong modal
        }
        if (itemIdToDelete && modalIdElement) {
            modalIdElement.textContent = itemIdToDelete; // Cập nhật ID nếu có
            if (modalIdElement.parentElement) modalIdElement.parentElement.style.display = ''; // Hiển thị lại nếu bị ẩn
        } else if (modalIdElement && modalIdElement.parentElement) {
            modalIdElement.parentElement.style.display = 'none'; // Ẩn nếu không dùng
        }

        // Hiển thị modal xác nhận
        modalInstance.show();

    });

    // Lắng nghe sự kiện khi modal ẩn đi để dọn dẹp backdrop
    modalElement.addEventListener('hidden.bs.modal', cleanupModalBackdrop); // Gọi hàm cleanup từ site.js

    // Lắng nghe sự kiện click nút xác nhận xóa
    confirmButton.addEventListener('click', function () {
        if (!itemIdToDelete) return;

        const finalUrl = options.deleteUrlTemplate.replace('{id}', encodeURIComponent(itemIdToDelete));
        const currentItemName = itemName; // Lưu lại tên để dùng trong thông báo

        fetch(finalUrl, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': options.antiForgeryToken,
                'X-Requested-With': 'XMLHttpRequest',
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        })
        .then(async response => {
            const data = await response.json();

            if (!response.ok) {
                // Lấy thông báo lỗi từ response
                const errorMsg = data.message || `Lỗi HTTP! status: ${response.status}`;
                const error = new Error(errorMsg);
                error.data = data;
                throw error;
            }

            return data;
        })
        .then(data => {
            // Kiểm tra success từ response
            if (data.success) {
                // Xóa dòng khỏi DataTables
                try {
                    //options.dataTableInstance.row($(triggerButton).closest('tr')).remove().draw(false);
                    options.dataTableInstance.ajax.reload(null, false); // false để giữ nguyên trang hiện tại
                } catch (e) {
                    console.error("Lỗi khi xóa dòng khỏi DataTable:", e);
                    // Fallback: reload lại trang nếu cần
                    // location.reload();
                }
                // Hiển thị thông báo thành công từ server hoặc dùng mặc định
                const successMessage = data.message ||
                    `Đã xóa thành công ${options.entityName || 'mục'} '${currentItemName}'.`;
                //showAppNotification(successMessage, 'success');
                showAppNotification(successMessage, 'success', 'notificationPlaceholder', false); // false để tắt auto-hide
            } else {
                // Xử lý lỗi từ server
                const errorMessage = data.message || 'Đã xảy ra lỗi không xác định khi xóa.';
                //showAppNotification(`Lỗi: ${errorMessage}`, 'danger');
                showAppNotification(`Lỗi: ${errorMessage}`, 'danger', 'notificationPlaceholder', false); // false để tắt auto-hide
            }
        })
            .catch(error => {
                console.error('Fetch Error:', error);
                //showAppNotification(`Đã xảy ra lỗi khi gửi yêu cầu xóa. ${error.message}`, 'danger');
                showAppNotification(`Đã xảy ra lỗi khi gửi yêu cầu xóa. ${error.message}`, 'danger', 'notificationPlaceholder', false); // false để tắt auto-hide
            })
            .finally(() => {
                modalInstance.hide(); // Luôn đóng modal
                itemIdToDelete = null; // Reset ID
                triggerButton = null;
                itemName = '';
            });
    });

}
