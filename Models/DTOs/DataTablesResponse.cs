using System.Collections.Generic; // Required for List<T>

namespace CTOM.Models.DTOs
{
    // Lớp này đại diện cho yêu cầu được gửi từ DataTables.net AJAX
    public class DataTablesRequest
    {
        // Số thứ tự của yêu cầu, DataTables sử dụng để đồng bộ
        public int Draw { get; set; }

        // Chỉ số bắt đầu của dữ liệu cho trang hiện tại (cho phân trang)
        public int Start { get; set; }

        // Số lượng bản ghi hiển thị trên mỗi trang
        public int Length { get; set; }

        // Thông tin về các cột trong bảng
        // Đã thay đổi từ Dictionary<int, DataTablesColumn> thành List<DataTablesColumn>
        // để khớp với cách DataTables gửi dữ liệu (dưới dạng mảng các đối tượng cột)
        public List<DataTablesColumn> Columns { get; set; } = [];

        // Thông tin về cách sắp xếp các cột
        // Đã thay đổi từ Dictionary<int, DataTablesOrder> thành List<DataTablesOrder>
        // DataTables gửi một mảng các đối tượng sắp xếp (thường chỉ có 1 phần tử nếu orderMulti=false)
        public List<DataTablesOrder> Order { get; set; } = [];

        // Thông tin tìm kiếm chung cho toàn bảng
        public Search Search { get; set; } = new Search();
    }

    // Lớp này đại diện cho phản hồi mà server gửi lại cho DataTables.net
    public class DataTablesResponse<T>
    {
        // Phải khớp với `Draw` từ request để DataTables chấp nhận dữ liệu
        public int Draw { get; set; }

        // Tổng số bản ghi trong toàn bộ tập dữ liệu (trước khi lọc)
        public int RecordsTotal { get; set; }

        // Tổng số bản ghi sau khi đã áp dụng bộ lọc (nếu có)
        public int RecordsFiltered { get; set; }

        // Dữ liệu thực tế cho trang hiện tại (mảng các đối tượng)
        public List<T> Data { get; set; } = [];

        // Thông báo lỗi (nếu có) để DataTables hiển thị
        public string Error { get; set; } = string.Empty;
    }

    // Đại diện cho một cột trong DataTables
    public class DataTablesColumn
    {
        // Tên thuộc tính dữ liệu cho cột này (ví dụ: "soCif", "tenCif")
        // Khớp với thuộc tính 'data' trong cấu hình 'columns' của DataTables JS
        public string Data { get; set; } = string.Empty;

        // Tên của cột, thường dùng cho server-side sorting
        // Khớp với thuộc tính 'name' trong cấu hình 'columns' của DataTables JS
        // và nên khớp với tên thuộc tính của Entity/Model ở server
        public string Name { get; set; } = string.Empty;

        // Cột này có cho phép tìm kiếm không?
        public bool Searchable { get; set; }

        // Cột này có cho phép sắp xếp không?
        public bool Orderable { get; set; }

        // Thông tin tìm kiếm riêng cho cột này (nếu DataTables hỗ trợ tìm kiếm từng cột)
        public Search Search { get; set; } = new Search();
    }

    // Đại diện cho một yêu cầu sắp xếp cột
    public class DataTablesOrder
    {
        // Chỉ số của cột cần sắp xếp (trong mảng `columns` của DataTables)
        public int Column { get; set; }

        // Hướng sắp xếp ("asc" hoặc "desc")
        public string Dir { get; set; } = "asc";
    }

    // Đại diện cho một yêu cầu tìm kiếm (chung hoặc cho từng cột)
    public class Search
    {
        // Giá trị tìm kiếm
        public string Value { get; set; } = string.Empty;

        // Có sử dụng biểu thức chính quy (regex) cho tìm kiếm không?
        public bool Regex { get; set; }
    }
}
