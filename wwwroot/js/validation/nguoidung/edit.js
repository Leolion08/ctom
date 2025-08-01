// Khai báo các biến toàn cục
let form = null;

// Hàm khởi tạo validation
function initializeValidation() {
    form = document.getElementById('editUserForm');
    if (!form) return;

    // Thêm sự kiện submit form
    form.addEventListener('submit', handleFormSubmit);

    // Thêm sự kiện input để xóa thông báo lỗi khi người dùng nhập
    const inputs = form.querySelectorAll('input, select, textarea');
    inputs.forEach(input => {
        input.addEventListener('input', clearFieldError);
    });
}

// Xử lý khi submit form
function handleFormSubmit(e) {
    e.preventDefault();
    
    // Xóa tất cả thông báo lỗi cũ
    clearAllErrors();

    // Validate form
    if (validateForm()) {
        // Nếu hợp lệ, submit form
        form.submit();
    }
}

// Validate form
function validateForm() {
    let isValid = true;
    
    // Kiểm tra tên người dùng
    const tenUser = document.getElementById('TenUser');
    if (!tenUser.value.trim()) {
        showFieldError(tenUser, 'Vui lòng nhập họ và tên');
        isValid = false;
    }

    // Kiểm tra phòng ban
    const maPhong = document.getElementById('MaPhong');
    if (!maPhong.value) {
        showFieldError(maPhong, 'Vui lòng chọn phòng ban');
        isValid = false;
    }

    // Kiểm tra trạng thái
    const trangThai = document.getElementById('TrangThai');
    if (!trangThai.value) {
        showFieldError(trangThai, 'Vui lòng chọn trạng thái');
        isValid = false;
    }

    // Kiểm tra ít nhất một nhóm quyền được chọn
    const roleCheckboxes = document.querySelectorAll('input[name="SelectedRoleNames"]:checked');
    if (roleCheckboxes.length === 0) {
        showError('Vui lòng chọn ít nhất một nhóm quyền');
        isValid = false;
    }

    return isValid;
}

// Khởi tạo validation khi DOM đã tải xong
document.addEventListener('DOMContentLoaded', initializeValidation);
