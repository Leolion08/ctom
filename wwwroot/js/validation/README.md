# Hướng dẫn sử dụng thư viện Validation

Thư viện này cung cấp các chức năng validation cho ứng dụng CTOM, sử dụng jQuery Validation và các plugin liên quan.

## Cấu trúc thư mục

```
wwwroot/js/validation/
├── common/                  # Các file validation dùng chung
│   ├── validation-common.js # Các hàm validation cơ bản
│   └── validation-helper.js # Các hàm hỗ trợ validation
├── khachhangdn/             # Validation cho module KhachHangDN
│   └── khachhangdn.js       # Cấu hình validation cho form KhachHangDN
├── index.js                 # File index để import các module
└── README.md               # Tài liệu hướng dẫn
```

## Cách sử dụng

### 1. Import thư viện

Thêm các thư viện cần thiết vào file layout hoặc view:

```html
<!-- Trong _Layout.cshtml hoặc view tương ứng -->
<script src="~/lib/jquery/dist/jquery.min.js"></script>
<script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"></script>

<!-- Import thư viện validation -->
<script src="~/js/validation/common/validation-common.js" asp-append-version="true"></script>
<script src="~/js/validation/common/validation-helper.js" asp-append-version="true"></script>
<script src="~/js/validation/khachhangdn/khachhangdn.js" asp-append-version="true"></script>
```

### 2. Sử dụng trong form

Thêm class `khachhangdn-form` vào form cần validate:

```html
<form class="khachhangdn-form" asp-action="Create" method="post">
    <!-- Các trường dữ liệu -->
    <div class="form-group">
        <label asp-for="SoCif"></label>
        <input asp-for="SoCif" class="form-control" />
        <span asp-validation-for="SoCif" class="text-danger"></span>
    </div>
    
    <!-- Các trường khác... -->
    
    <button type="submit" class="btn btn-primary">Lưu</button>
</form>
```

### 3. Tùy chỉnh validation

Bạn có thể tùy chỉnh validation bằng cách truyền các options:

```javascript
$(document).ready(function() {
    $('form.khachhangdn-form').each(function() {
        khachHangDNValidation.init(this, {
            enableAjaxValidation: true, // Bật/tắt kiểm tra trùng lặp qua AJAX
            customRules: {
                // Thêm rule tùy chỉnh
                customRule: {
                    validator: function(value, element) {
                        // Logic validation
                        return true;
                    },
                    message: 'Thông báo lỗi tùy chỉnh'
                }
            },
            messages: {
                // Ghi đè thông báo lỗi mặc định
                'SoCif': {
                    required: 'Vui lòng nhập số CIF',
                    remote: 'Số CIF đã tồn tại trong hệ thống'
                }
            },
            onSubmit: function(form) {
                // Xử lý khi form hợp lệ
                form.submit();
            }
        });
    });
});
```

## Các hàm validation có sẵn

### 1. Trong validation-common.js

- `isValidVietnamesePhoneNumber(phoneNumber)`: Kiểm tra số điện thoại Việt Nam hợp lệ
- `isValidEmail(email)`: Kiểm tra email hợp lệ
- `isValidDate(dateString)`: Kiểm tra ngày tháng hợp lệ (dd/MM/yyyy)
- `formatVietnamesePhoneNumber(phoneNumber)`: Định dạng số điện thoại Việt Nam
- `isDateAfter(date1, date2)`: Kiểm tra date1 có sau date2 không
- `isNumeric(value)`: Kiểm tra giá trị có phải là số không
- `isLength(value, min, max)`: Kiểm tra độ dài chuỗi

### 2. Trong validation-helper.js

- `formatPhoneNumber(phoneNumber)`: Định dạng số điện thoại
- `isElementVisible($element)`: Kiểm tra phần tử có hiển thị không
- `scrollToFirstError()`: Cuộn đến trường bị lỗi đầu tiên
- `initDatepicker(selector, options)`: Khởi tạo datepicker

## Quy ước

1. Đặt tên file theo quy ước: `[tên-module]-validation.js`
2. Mỗi module validation nên có file riêng trong thư mục tương ứng
3. Sử dụng các hàm helper có sẵn trong `validation-common.js` và `validation-helper.js`
4. Tuân thủ quy ước đặt tên và cấu trúc thư mục

## Ghi chú

- Thư viện này phụ thuộc vào jQuery và jQuery Validation
- Đảm bảo các thư viện này đã được tải trước khi sử dụng
- Sử dụng `asp-append-version="true"` để tránh cache khi phát triển
