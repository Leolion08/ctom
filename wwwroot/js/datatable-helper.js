/**
 * Khởi tạo tooltip thông minh cho các cột được chỉ định trong DataTable.
 * Hàm này sẽ tự động kiểm tra nội dung bị tràn và chỉ hiển thị tooltip khi cần thiết.
 * Nó cũng xử lý tooltip cho các nút bấm có thuộc tính 'title'.
 *
 * @param {object} dtApi - Đối tượng API của DataTables (this.api()).
 * @param {number[]} truncateColumnIndexes - Mảng các chỉ số cột cần kiểm tra cắt nội dung.
 */
function initializeTruncateTooltips(dtApi, truncateColumnIndexes = []) {
    const tolerance = 1; // Tránh lỗi làm tròn

    truncateColumnIndexes.forEach(colIndex => {
        dtApi.column(colIndex, { page: 'current' }).nodes().to$().each(function () {
            const $truncateDiv = $(this).find('.text-truncate');
            if ($truncateDiv.length === 0) return;

            const el = $truncateDiv[0];

            // Đảm bảo render hoàn tất trước khi đo kích thước
            requestAnimationFrame(() => {
                const isOverflowing = el.scrollWidth > el.clientWidth + tolerance;
                const tooltipInstance = bootstrap.Tooltip.getInstance(el);

                if (isOverflowing) {
                    $truncateDiv.addClass('cursor-help');
                    if (!tooltipInstance) {
                        new bootstrap.Tooltip(el, {
                            title: $truncateDiv.text(),
                            placement: 'top',
                            trigger: 'hover',
                            customClass: 'tooltip-wide'
                        });
                    } else {
                        el.setAttribute('title', $truncateDiv.text()); // Cập nhật title nếu đã có tooltip
                    }
                } else {
                    $truncateDiv.removeClass('cursor-help');
                    if (tooltipInstance) tooltipInstance.dispose();
                    el.removeAttribute('title');
                }
            });
        });
    });

    // 2. Tooltip cho phần tử có title, trừ button và <a.btn>
    dtApi.$('[title]').each(function () {
        const tag = this.tagName.toLowerCase();
        const isButtonOrFakeButton =
            tag === 'button' ||
            (tag === 'a' && this.classList.contains('btn'));

        if (isButtonOrFakeButton) return;

        const $el = $(this);
        const titleText = this.getAttribute('title');
        if (!titleText?.trim()) return;

        $el.addClass('cursor-help');

        if (!bootstrap.Tooltip.getInstance(this)) {
            new bootstrap.Tooltip(this, {
                placement: 'top',
                trigger: 'hover',
                delay: { show: 100, hide: 50 },
            });
        }
    });

    // (Không bật mặc định, uncomment nếu muốn)
    // Khởi tạo tooltip cho các nút có thuộc tính title
    // dtApi.$('[title]').each(function () {
    //     if (!bootstrap.Tooltip.getInstance(this)) {
    //         new bootstrap.Tooltip(this);
    //     }
    // });
}

/**
 * Định dạng ngày theo chuỗi format tuỳ chọn, ví dụ: 'dd/MM/yyyy HH:mm'.
 * Các mã hỗ trợ: dd, MM, yyyy, HH, mm, ss
 *
 * @param {string|Date|null} inputDate - Ngày đầu vào (chuỗi ISO hoặc Date object).
 * @param {string} formatStr - Chuỗi định dạng. Mặc định: 'dd/MM/yyyy HH:mm'
 * @returns {string} - Chuỗi ngày đã định dạng hoặc rỗng nếu không hợp lệ.
 */
function formatDateTime(inputDate, formatStr = 'dd/MM/yyyy HH:mm') {
    if (!inputDate) return '';
    const date = new Date(inputDate);
    if (isNaN(date.getTime())) return '';

    const pad = (n) => String(n).padStart(2, '0');

    const map = {
        dd: pad(date.getDate()),
        MM: pad(date.getMonth() + 1),
        yyyy: date.getFullYear(),
        HH: pad(date.getHours()),
        mm: pad(date.getMinutes()),
        ss: pad(date.getSeconds())
    };

    return formatStr.replace(/dd|MM|yyyy|HH|mm|ss/g, match => map[match] || match);
}
