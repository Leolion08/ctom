# Thư mục JavaScript

## Cấu trúc thư mục

```
wwwroot/
├── js/
│   ├── common.js            # Tiện ích chung toàn cục (load trong _Layout.cshtml)
│   ├── site.js             # Script chính của ứng dụng
│   ├── site.modal-delete.js # Xử lý modal xóa
│   │
│   └── validation/         # Các script liên quan đến validation
│       ├── validation-common.js  # Hàm validation dùng chung
│       └── nguoidung/      # Script riêng cho từng action của NguoiSuDung
│           ├── create.js
│           ├── edit.js
│           └── ...
```

## Quy ước đặt tên

- **File toàn cục**: Đặt tên ngắn gọn, ví dụ: `common.js`, `site.js`
- **File validation**: Thêm tiền tố `validation-`, ví dụ: `validation-common.js`
- **File theo module/controller**: Đặt trong thư mục tương ứng, ví dụ: `nguoidung/create.js`

## Cách sử dụng

1. **common.js**: Tự động được load trong `_Layout.cshtml`, có thể sử dụng các hàm qua đối tượng `window.appCommon`
   ```javascript
   appCommon.showToast('Thành công!', 'success');
   const formattedDate = appCommon.formatDate(new Date());
   ```

2. **validation-common.js**: Tự động được load khi sử dụng `_ValidationScriptsPartial`

3. **Script riêng cho từng trang**: Đặt trong thư mục tương ứng, sẽ tự động được load khi vào trang đó
