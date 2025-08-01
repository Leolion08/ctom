// Khởi tạo modal reset mật khẩu
function initResetPasswordModal() {
    // Xử lý sự kiện khi click vào nút reset mật khẩu
    document.querySelectorAll('.btn-reset-password').forEach(button => {
        button.addEventListener('click', function() {
            const userId = this.getAttribute('data-user-id');
            const userName = this.getAttribute('data-user-name');
            
            // Cập nhật modal với thông tin người dùng
            document.getElementById('resetUserId').value = userId;
            document.getElementById('resetUserName').value = userName;
            document.getElementById('displayUserName').value = userName;
            
            // Reset form
            const form = document.getElementById('resetPasswordForm');
            form.reset();
            
            // Ẩn thông báo lỗi
            document.getElementById('resetError').classList.add('d-none');
            document.getElementById('passwordMismatchError').classList.add('d-none');
            
            // Mở modal
            const modal = new bootstrap.Modal(document.getElementById('resetPasswordModal'));
            modal.show();
        });
    });
    
    // Xử lý khi modal đã ẩn
    const modal = document.getElementById('resetPasswordModal');
    if (modal) {
        modal.addEventListener('hidden.bs.modal', function () {
            // Reset form khi đóng modal
            const form = document.getElementById('resetPasswordForm');
            form.reset();
        });
    }
}

// Xử lý khi form được submit
document.addEventListener('DOMContentLoaded', function() {
    initResetPasswordModal();
    
    const form = document.getElementById('resetPasswordForm');
    if (form) {
        form.addEventListener('submit', function(e) {
            e.preventDefault();
            
            const formData = new FormData(form);
            const resetError = document.getElementById('resetError');
            
            // Hiển thị loading
            const submitButton = document.getElementById('resetPasswordSubmit');
            const originalButtonText = submitButton.innerHTML;
            submitButton.disabled = true;
            submitButton.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span> Đang xử lý...';
            
            // Gửi yêu cầu AJAX
            fetch(form.action, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value,
                    'Content-Type': 'application/x-www-form-urlencoded',
                },
                body: new URLSearchParams(formData).toString()
            })
            .then(response => {
                if (!response.ok) {
                    return response.json().then(err => { throw err; });
                }
                return response.json();
            })
            .then(data => {
                if (data.success) {
                    // Đóng modal và hiển thị thông báo thành công
                    const modal = bootstrap.Modal.getInstance(document.getElementById('resetPasswordModal'));
                    modal.hide();
                    
                    // Hiển thị thông báo thành công
                    showSuccess('Đặt lại mật khẩu thành công!');
                } else {
                    // Hiển thị thông báo lỗi
                    resetError.textContent = data.message || 'Đã xảy ra lỗi khi đặt lại mật khẩu.';
                    resetError.classList.remove('d-none');
                }
            })
            .catch(error => {
                console.error('Error:', error);
                resetError.textContent = error.message || 'Đã xảy ra lỗi khi gửi yêu cầu.';
                resetError.classList.remove('d-none');
            })
            .finally(() => {
                // Khôi phục trạng thái nút
                submitButton.disabled = false;
                submitButton.innerHTML = originalButtonText;
            });
        });
    }
});
