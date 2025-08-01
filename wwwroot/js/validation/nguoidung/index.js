function initNguoiDungIndex() {
    // Xử lý xác nhận xóa người dùng
    $('.btn-delete-user').on('click', function(e) {
        e.preventDefault();
        
        const userId = $(this).data('id');
        const userName = $(this).data('name') || 'người dùng này';
        
        if (confirm(`Bạn có chắc chắn muốn xóa ${userName}?`)) {
            window.location.href = `/NguoiSuDung/Delete/${userId}`;
        }
    });
    
    // Xử lý tìm kiếm
    const $searchForm = $('#searchForm');
    if ($searchForm.length) {
        $searchForm.on('submit', function(e) {
            const searchTerm = $('#searchTerm').val().trim();
            if (searchTerm.length > 0 && searchTerm.length < 2) {
                e.preventDefault();
                showError('Vui lòng nhập ít nhất 2 ký tự để tìm kiếm');
                return false;
            }
            return true;
        });
    }
}

// Khởi tạo khi DOM sẵn sàng
$(document).ready(function() {
    try {
        initNguoiDungIndex();
    } catch (error) {
        console.error('Lỗi khi khởi tạo trang danh sách người dùng:', error);
    }
});
