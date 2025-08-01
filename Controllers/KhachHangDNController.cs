using CTOM.Data;
using CTOM.Models;
using CTOM.Models.DTOs;
using CTOM.Models.Entities;
using CTOM.Models.Responses;
using System.Text.Json;
using CTOM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Linq.Dynamic.Core; // Cần thiết cho sắp xếp động
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.IO;
using Microsoft.AspNetCore.Http;
using ExcelDataReader;
using System.Data;
using CTOM.Extensions;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Generic; // Cần cho List<T>

namespace CTOM.Controllers
{
    [Authorize(Roles = "ADMIN,HTTD")]
    [Route("[controller]")]
    public partial class KhachHangDNController(
        ApplicationDbContext _context,
        UserManager<ApplicationUser> _userManager,
        ILogger<KhachHangDNController> _logger) : Controller
    {

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            // Xử lý thông báo từ TempData nếu có


            // Action Index giờ chỉ cần trả về View.
            // Mọi logic tải dữ liệu, phân trang, sắp xếp, tìm kiếm sẽ do DataTables AJAX call xử lý.
            return View();
        }

        [HttpGet]
        [Route("Create")]
        public IActionResult Create()
        {
            return View(new KhachHangDNViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Create")]
        public async Task<IActionResult> Create(KhachHangDNViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    bool cifExists = await _context.KhachHangDNs
                        .AnyAsync(kh => kh.SoCif == model.SoCif);

                    if (cifExists)
                    {
                        ModelState.AddModelError(nameof(model.SoCif),
                            $"Số CIF '{model.SoCif}' đã tồn tại trong hệ thống.");
                        return View(model);
                    }

                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser == null)
                    {
                        ModelState.AddModelError(string.Empty, "Không thể xác định người dùng hiện tại.");
                        return View(model);
                    }

                    var khachHang = new KhachHangDN
                    {
                        // Thông tin từ form
                        SoCif = model.SoCif ?? string.Empty,
                        TenCif = model.TenCif ?? string.Empty,
                        XepHangTinDungNoiBo = model.XepHangTinDungNoiBo ?? string.Empty,
                        LoaiHinhDN = model.LoaiHinhDN ?? string.Empty,
                        SoGiayChungNhanDKKD = model.SoGiayChungNhanDKKD ?? string.Empty,
                        NoiCapGiayChungNhanDKKD = model.NoiCapGiayChungNhanDKKD ?? string.Empty,
                        NgayCapGiayChungNhanDKKD = model.NgayCapGiayChungNhanDKKD,
                        TenTiengAnh = model.TenTiengAnh ?? string.Empty,
                        DiaChiTrenDKKD = model.DiaChiTrenDKKD ?? string.Empty,
                        DiaChiKinhDoanhHienTai = model.DiaChiKinhDoanhHienTai ?? string.Empty,
                        LinhVucKinhDoanhChinh = model.LinhVucKinhDoanhChinh ?? string.Empty,
                        SoDienThoaiDN = model.SoDienThoaiDN ?? string.Empty,
                        SoFaxCongTy = model.SoFaxCongTy,
                        EmailCongTy = model.EmailCongTy,
                        TenNguoiDaiDienTheoPhapLuat = model.TenNguoiDaiDienTheoPhapLuat,
                        ChucVu = model.ChucVu,
                        NgaySinhDaiDienCongTy = model.NgaySinhDaiDienCongTy,
                        GioiTinhDaiDienCongTy = model.GioiTinhDaiDienCongTy,
                        QuocTichDaiDienCongTy = model.QuocTichDaiDienCongTy,
                        SoGiayToTuyThanDaiDienCongTy = model.SoGiayToTuyThanDaiDienCongTy,
                        NgayCapGiayToTuyThanDaiDienDN = model.NgayCapGiayToTuyThanDaiDienDN,
                        NoiCapGiayToTuyThanDaiDienDN = model.NoiCapGiayToTuyThanDaiDienDN,
                        NgayHetHanGiayToTuyThanDaiDienDN = model.NgayHetHanGiayToTuyThanDaiDienDN,
                        DiaChiDaiDienCongTy = model.DiaChiDaiDienCongTy,
                        SoDienThoaiDaiDienCongTy = model.SoDienThoaiDaiDienCongTy,
                        EmailDaiDienCongTy = model.EmailDaiDienCongTy,
                        VanBanUyQuyenSo = model.VanBanUyQuyenSo,
                        TenKeToanTruong = model.TenKeToanTruong,
                        ChucVuKeToanTruong = model.ChucVuKeToanTruong,
                        NgaySinhKeToanTruong = model.NgaySinhKeToanTruong,
                        GioiTinhKeToanTruong = model.GioiTinhKeToanTruong,
                        QuocTichKeToanTruong = model.QuocTichKeToanTruong,
                        SoGiayToTuyThanKeToanTruong = model.SoGiayToTuyThanKeToanTruong,
                        NgayCapGiayToTuyThanKeToanTruong = model.NgayCapGiayToTuyThanKeToanTruong,
                        NoiCapGiayToTuyThanKeToanTruong = model.NoiCapGiayToTuyThanKeToanTruong,
                        NgayHetHanGiayToTuyThanKeToanTruong = model.NgayHetHanGiayToTuyThanKeToanTruong,
                        SoDienThoaiKeToanTruong = model.SoDienThoaiKeToanTruong,
                        EmailKeToanTruong = model.EmailKeToanTruong,
                        DiaChiKeToanTruong = model.DiaChiKeToanTruong,
                        // Thông tin hệ thống
                        UserThucHienId = currentUser?.UserName,
                        PhongThucHien = currentUser?.MaPhong,
                        NgayCapNhatDuLieu = DateTime.Now
                    };

                    _context.Add(khachHang);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Thêm mới khách hàng thành công. Số CIF: {model.SoCif}";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi thêm mới khách hàng");
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu dữ liệu. Vui lòng thử lại sau.");
                }
            }
            return View(model);
        }

        /// <summary>
        /// Hiển thị chi tiết thông tin khách hàng doanh nghiệp
        /// </summary>
        /// <param name="id">Số CIF của khách hàng cần xem chi tiết</param>
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Số CIF không hợp lệ.");
            }

            var khachHang = await _context.KhachHangDNs
                .Include(kh => kh.UserThucHien)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.SoCif == id);

            if (khachHang == null)
            {
                return NotFound($"Không tìm thấy khách hàng có số CIF '{id}'");
            }

            var viewModel = new KhachHangDNViewModel
            {
                SoCif = khachHang.SoCif,
                TenCif = khachHang.TenCif,
                XepHangTinDungNoiBo = khachHang.XepHangTinDungNoiBo,
                LoaiHinhDN = khachHang.LoaiHinhDN,
                SoGiayChungNhanDKKD = khachHang.SoGiayChungNhanDKKD,
                NoiCapGiayChungNhanDKKD = khachHang.NoiCapGiayChungNhanDKKD,
                NgayCapGiayChungNhanDKKD = khachHang.NgayCapGiayChungNhanDKKD,
                TenTiengAnh = khachHang.TenTiengAnh,
                DiaChiTrenDKKD = khachHang.DiaChiTrenDKKD,
                DiaChiKinhDoanhHienTai = khachHang.DiaChiKinhDoanhHienTai,
                LinhVucKinhDoanhChinh = khachHang.LinhVucKinhDoanhChinh,
                SoDienThoaiDN = khachHang.SoDienThoaiDN,
                SoFaxCongTy = khachHang.SoFaxCongTy,
                EmailCongTy = khachHang.EmailCongTy,
                TenNguoiDaiDienTheoPhapLuat = khachHang.TenNguoiDaiDienTheoPhapLuat,
                ChucVu = khachHang.ChucVu,
                NgaySinhDaiDienCongTy = khachHang.NgaySinhDaiDienCongTy,
                GioiTinhDaiDienCongTy = khachHang.GioiTinhDaiDienCongTy,
                QuocTichDaiDienCongTy = khachHang.QuocTichDaiDienCongTy,
                SoGiayToTuyThanDaiDienCongTy = khachHang.SoGiayToTuyThanDaiDienCongTy,
                NgayCapGiayToTuyThanDaiDienDN = khachHang.NgayCapGiayToTuyThanDaiDienDN,
                NoiCapGiayToTuyThanDaiDienDN = khachHang.NoiCapGiayToTuyThanDaiDienDN,
                NgayHetHanGiayToTuyThanDaiDienDN = khachHang.NgayHetHanGiayToTuyThanDaiDienDN,
                DiaChiDaiDienCongTy = khachHang.DiaChiDaiDienCongTy,
                SoDienThoaiDaiDienCongTy = khachHang.SoDienThoaiDaiDienCongTy,
                EmailDaiDienCongTy = khachHang.EmailDaiDienCongTy,
                VanBanUyQuyenSo = khachHang.VanBanUyQuyenSo,
                TenKeToanTruong = khachHang.TenKeToanTruong,
                ChucVuKeToanTruong = khachHang.ChucVuKeToanTruong,
                NgaySinhKeToanTruong = khachHang.NgaySinhKeToanTruong,
                GioiTinhKeToanTruong = khachHang.GioiTinhKeToanTruong,
                QuocTichKeToanTruong = khachHang.QuocTichKeToanTruong,
                SoGiayToTuyThanKeToanTruong = khachHang.SoGiayToTuyThanKeToanTruong,
                NgayCapGiayToTuyThanKeToanTruong = khachHang.NgayCapGiayToTuyThanKeToanTruong,
                NoiCapGiayToTuyThanKeToanTruong = khachHang.NoiCapGiayToTuyThanKeToanTruong,
                NgayHetHanGiayToTuyThanKeToanTruong = khachHang.NgayHetHanGiayToTuyThanKeToanTruong,
                SoDienThoaiKeToanTruong = khachHang.SoDienThoaiKeToanTruong,
                EmailKeToanTruong = khachHang.EmailKeToanTruong,
                DiaChiKeToanTruong = khachHang.DiaChiKeToanTruong,
                UserThucHienId = khachHang.UserThucHienId,
                PhongThucHien = khachHang.PhongThucHien,
                NgayCapNhatDuLieu = khachHang.NgayCapNhatDuLieu
            };

            return View(viewModel);
        }

        [HttpGet("Edit/{soCif}")]
        public async Task<IActionResult> Edit(string soCif)
        {
            if (string.IsNullOrEmpty(soCif))
            {
                _logger.LogWarning("Truy cập sửa khách hàng với số CIF trống");
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var khachHang = await _context.KhachHangDNs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(kh => kh.SoCif == soCif);

                if (khachHang == null)
                {
                    _logger.LogWarning("Không tìm thấy khách hàng với số CIF: {SoCif}", soCif);
                    TempData["ErrorMessage"] = $"Không tìm thấy khách hàng có số CIF: {soCif}";
                    return RedirectToAction(nameof(Index));
                }

                // Log thông tin truy cập
                _logger.LogInformation("Đang tải thông tin khách hàng để chỉnh sửa - Số CIF: {SoCif}", soCif);

                var viewModel = new KhachHangDNViewModel
                {
                    SoCif = khachHang.SoCif,
                    TenCif = khachHang.TenCif,
                    XepHangTinDungNoiBo = khachHang.XepHangTinDungNoiBo ?? string.Empty,
                    LoaiHinhDN = khachHang.LoaiHinhDN,
                    SoGiayChungNhanDKKD = khachHang.SoGiayChungNhanDKKD,
                    NoiCapGiayChungNhanDKKD = khachHang.NoiCapGiayChungNhanDKKD,
                    NgayCapGiayChungNhanDKKD = khachHang.NgayCapGiayChungNhanDKKD,
                    TenTiengAnh = khachHang.TenTiengAnh,
                    DiaChiTrenDKKD = khachHang.DiaChiTrenDKKD,
                    DiaChiKinhDoanhHienTai = khachHang.DiaChiKinhDoanhHienTai,
                    LinhVucKinhDoanhChinh = khachHang.LinhVucKinhDoanhChinh,
                    SoDienThoaiDN = khachHang.SoDienThoaiDN,
                    SoFaxCongTy = khachHang.SoFaxCongTy,
                    EmailCongTy = khachHang.EmailCongTy,
                    TenNguoiDaiDienTheoPhapLuat = khachHang.TenNguoiDaiDienTheoPhapLuat,
                    ChucVu = khachHang.ChucVu,
                    NgaySinhDaiDienCongTy = khachHang.NgaySinhDaiDienCongTy,
                    GioiTinhDaiDienCongTy = khachHang.GioiTinhDaiDienCongTy,
                    QuocTichDaiDienCongTy = khachHang.QuocTichDaiDienCongTy,
                    SoGiayToTuyThanDaiDienCongTy = khachHang.SoGiayToTuyThanDaiDienCongTy,
                    NgayCapGiayToTuyThanDaiDienDN = khachHang.NgayCapGiayToTuyThanDaiDienDN,
                    NoiCapGiayToTuyThanDaiDienDN = khachHang.NoiCapGiayToTuyThanDaiDienDN,
                    NgayHetHanGiayToTuyThanDaiDienDN = khachHang.NgayHetHanGiayToTuyThanDaiDienDN,
                    DiaChiDaiDienCongTy = khachHang.DiaChiDaiDienCongTy,
                    SoDienThoaiDaiDienCongTy = khachHang.SoDienThoaiDaiDienCongTy,
                    EmailDaiDienCongTy = khachHang.EmailDaiDienCongTy,
                    VanBanUyQuyenSo = khachHang.VanBanUyQuyenSo,
                    TenKeToanTruong = khachHang.TenKeToanTruong,
                    ChucVuKeToanTruong = khachHang.ChucVuKeToanTruong,
                    NgaySinhKeToanTruong = khachHang.NgaySinhKeToanTruong,
                    GioiTinhKeToanTruong = khachHang.GioiTinhKeToanTruong,
                    QuocTichKeToanTruong = khachHang.QuocTichKeToanTruong,
                    SoGiayToTuyThanKeToanTruong = khachHang.SoGiayToTuyThanKeToanTruong,
                    NgayCapGiayToTuyThanKeToanTruong = khachHang.NgayCapGiayToTuyThanKeToanTruong,
                    NoiCapGiayToTuyThanKeToanTruong = khachHang.NoiCapGiayToTuyThanKeToanTruong,
                    NgayHetHanGiayToTuyThanKeToanTruong = khachHang.NgayHetHanGiayToTuyThanKeToanTruong,
                    SoDienThoaiKeToanTruong = khachHang.SoDienThoaiKeToanTruong,
                    EmailKeToanTruong = khachHang.EmailKeToanTruong,
                    DiaChiKeToanTruong = khachHang.DiaChiKeToanTruong
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải thông tin khách hàng để chỉnh sửa - Số CIF: {SoCif}", soCif);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải thông tin khách hàng. Vui lòng thử lại sau.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost("Edit/{soCif}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string soCif, KhachHangDNViewModel model)
        {
            if (soCif != model.SoCif)
            {
                return NotFound();
            }

            // Kiểm tra trùng lặp CIF nếu có thay đổi
            if (soCif != model.SoCif)
            {
                bool cifExists = await _context.KhachHangDNs
                    .AnyAsync(kh => kh.SoCif == model.SoCif);
                if (cifExists)
                {
                    ModelState.AddModelError(nameof(model.SoCif),
                        $"Số CIF '{model.SoCif}' đã tồn tại trong hệ thống.");
                    return View(model);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {

                    var existingKhachHang = await _context.KhachHangDNs
                        .FirstOrDefaultAsync(kh => kh.SoCif == soCif);

                    if (existingKhachHang == null)
                    {
                        return NotFound();
                    }

                    existingKhachHang.SoCif = model.SoCif;
                    existingKhachHang.TenCif = model.TenCif ?? string.Empty;
                    existingKhachHang.XepHangTinDungNoiBo = model.XepHangTinDungNoiBo;
                    existingKhachHang.LoaiHinhDN = model.LoaiHinhDN;
                    existingKhachHang.SoGiayChungNhanDKKD = model.SoGiayChungNhanDKKD;
                    existingKhachHang.NoiCapGiayChungNhanDKKD = model.NoiCapGiayChungNhanDKKD;
                    existingKhachHang.NgayCapGiayChungNhanDKKD = model.NgayCapGiayChungNhanDKKD;
                    existingKhachHang.TenTiengAnh = model.TenTiengAnh;
                    existingKhachHang.DiaChiTrenDKKD = model.DiaChiTrenDKKD;
                    existingKhachHang.DiaChiKinhDoanhHienTai = model.DiaChiKinhDoanhHienTai;
                    existingKhachHang.LinhVucKinhDoanhChinh = model.LinhVucKinhDoanhChinh;
                    existingKhachHang.SoDienThoaiDN = model.SoDienThoaiDN;
                    existingKhachHang.SoFaxCongTy = model.SoFaxCongTy;
                    existingKhachHang.EmailCongTy = model.EmailCongTy;
                    existingKhachHang.TenNguoiDaiDienTheoPhapLuat = model.TenNguoiDaiDienTheoPhapLuat;
                    existingKhachHang.ChucVu = model.ChucVu;
                    existingKhachHang.NgaySinhDaiDienCongTy = model.NgaySinhDaiDienCongTy;
                    existingKhachHang.GioiTinhDaiDienCongTy = model.GioiTinhDaiDienCongTy;
                    existingKhachHang.QuocTichDaiDienCongTy = model.QuocTichDaiDienCongTy;
                    existingKhachHang.SoGiayToTuyThanDaiDienCongTy = model.SoGiayToTuyThanDaiDienCongTy;
                    existingKhachHang.NgayCapGiayToTuyThanDaiDienDN = model.NgayCapGiayToTuyThanDaiDienDN;
                    existingKhachHang.NoiCapGiayToTuyThanDaiDienDN = model.NoiCapGiayToTuyThanDaiDienDN;
                    existingKhachHang.NgayHetHanGiayToTuyThanDaiDienDN = model.NgayHetHanGiayToTuyThanDaiDienDN;
                    existingKhachHang.DiaChiDaiDienCongTy = model.DiaChiDaiDienCongTy;
                    existingKhachHang.SoDienThoaiDaiDienCongTy = model.SoDienThoaiDaiDienCongTy;
                    existingKhachHang.EmailDaiDienCongTy = model.EmailDaiDienCongTy;
                    existingKhachHang.VanBanUyQuyenSo = model.VanBanUyQuyenSo;
                    existingKhachHang.TenKeToanTruong = model.TenKeToanTruong;
                    existingKhachHang.ChucVuKeToanTruong = model.ChucVuKeToanTruong;
                    existingKhachHang.NgaySinhKeToanTruong = model.NgaySinhKeToanTruong;
                    existingKhachHang.GioiTinhKeToanTruong = model.GioiTinhKeToanTruong;
                    existingKhachHang.QuocTichKeToanTruong = model.QuocTichKeToanTruong;
                    existingKhachHang.SoGiayToTuyThanKeToanTruong = model.SoGiayToTuyThanKeToanTruong;
                    existingKhachHang.NgayCapGiayToTuyThanKeToanTruong = model.NgayCapGiayToTuyThanKeToanTruong;
                    existingKhachHang.NoiCapGiayToTuyThanKeToanTruong = model.NoiCapGiayToTuyThanKeToanTruong;
                    existingKhachHang.NgayHetHanGiayToTuyThanKeToanTruong = model.NgayHetHanGiayToTuyThanKeToanTruong;
                    existingKhachHang.SoDienThoaiKeToanTruong = model.SoDienThoaiKeToanTruong;
                    existingKhachHang.EmailKeToanTruong = model.EmailKeToanTruong;
                    existingKhachHang.DiaChiKeToanTruong = model.DiaChiKeToanTruong;

                    // Cập nhật thông tin hệ thống
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser != null)
                    {
                        existingKhachHang.UserThucHienId = currentUser.UserName;
                        existingKhachHang.PhongThucHien = currentUser.MaPhong;
                    }
                    existingKhachHang.NgayCapNhatDuLieu = DateTime.Now;

                    try
                    {
                        _context.Update(existingKhachHang);
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = $"Đã cập nhật thông tin khách hàng CIF '{model.SoCif}' thành công.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        _logger.LogError(ex, "Lỗi đồng thời khi cập nhật khách hàng CIF: {SoCif}", model.SoCif);
                        ModelState.AddModelError("", "Dữ liệu đã bị thay đổi bởi người khác. Vui lòng tải lại trang và thử lại.");
                        return View(model);
                    }
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!await KhachHangDNExistsAsync(model.SoCif))
                    {
                        return NotFound();
                    }
                    _logger.LogError(ex, "Lỗi khi cập nhật khách hàng CIF: {SoCif}", model.SoCif);
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi cập nhật dữ liệu. Vui lòng thử lại sau.");
                }
            }
            return View(model);
        }

        /// <summary>
        /// Xử lý xóa khách hàng doanh nghiệp
        /// </summary>
        [HttpPost("Delete/{soCif}"), ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string soCif)
        {
            if (string.IsNullOrWhiteSpace(soCif))
                return Json(ApiResponse.Fail("Số CIF không được để trống"));

            try
            {
                var khachHang = await _context.KhachHangDNs
                    .FirstOrDefaultAsync(kh => kh.SoCif == soCif);

                if (khachHang == null)
                    return Json(ApiResponse.Fail($"Không tìm thấy khách hàng có CIF '{soCif}'"));

                // Kiểm tra các ràng buộc khác trước khi xóa (nếu cần)
                // Ví dụ: Kiểm tra nếu có bất kỳ dữ liệu liên quan nào khác

                _context.KhachHangDNs.Remove(khachHang);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đã xóa khách hàng: {SoCif} - {TenCif}", khachHang.SoCif, khachHang.TenCif);
                TempData["SuccessMessage"] = $"Đã xóa khách hàng: {khachHang.SoCif} - {khachHang.TenCif}";

                //return Json(ApiResponse.Ok($"Đã xóa thành công khách hàng '{khachHang.TenCif}'", new { id = khachHang.SoCif }));
                return Json(ApiResponse.Ok($"Đã xóa thành công khách hàng '{khachHang.SoCif} - {khachHang.TenCif}'", new { id = khachHang.SoCif, name = khachHang.TenCif }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa khách hàng CIF: {SoCif}", soCif);
                return Json(ApiResponse.Fail($"Đã xảy ra lỗi khi xóa khách hàng: {ex.Message}"));
            }
        }

        private async Task<bool> KhachHangDNExistsAsync(string soCif)
        {
            return await _context.KhachHangDNs.AnyAsync(e => e.SoCif == soCif);
        }

        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex EmailRegex();

        [GeneratedRegex(@"^[0-9\-\+\(\)\s]{10,20}$", RegexOptions.Compiled)]
        private static partial Regex PhoneNumberRegex();

        #region DataTables API

        /// <summary>
        /// Lấy dữ liệu cho DataTables với logic chuẩn (lọc, sắp xếp, phân trang phía server).
        /// </summary>
        [HttpPost("GetDataTable")] // Đổi tên action và route
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetDataTable([FromForm] DataTablesRequest dtRequest, [FromForm(Name = "q")] string? customSearch)
        {
            try
            {
                // --- 1. KHỞI TẠO TRUY VẤN CƠ SỞ ---
                var query = _context.KhachHangDNs.AsNoTracking().AsQueryable();

                // --- 2. LỌC THEO QUYỀN HẠN (nếu có) ---
                // (Hiện tại chưa có logic lọc quyền cho KHDN, có thể bổ sung sau)

                // --- 3. ĐẾM TỔNG SỐ BẢN GHI ---
                int recordsTotal = await query.CountAsync();

                // --- 4. ÁP DỤNG BỘ LỌC TÌM KIẾM ---
                // Giữ nguyên logic tìm kiếm theo SoCif và TenCif
                if (!string.IsNullOrWhiteSpace(customSearch))
                {
                    string filter = $"%{customSearch.Trim()}%";
                    query = query.Where(kh =>
                        (kh.SoCif != null && EF.Functions.Like(kh.SoCif, filter)) ||
                        (kh.TenCif != null && EF.Functions.Like(kh.TenCif, filter))
                    );
                }

                // --- 5. ĐẾM SỐ BẢN GHI SAU KHI LỌC ---
                int recordsFiltered = await query.CountAsync();

                // --- 6. XỬ LÝ SẮP XẾP (LOGIC CHUẨN VỚI TỐI ƯU MAPPING) ---
                var order = dtRequest.Order.FirstOrDefault();
                if (order != null && order.Column < dtRequest.Columns.Count)
                {
                    var clientSortColumn = dtRequest.Columns[order.Column].Name;
                    var sortDirection = order.Dir;

                    // "Bộ từ điển" ánh xạ tên cột từ client sang thuộc tính của Entity
                    var columnMapping = new Dictionary<string, string>
                    {
                        { "soCif", "SoCif" },
                        { "tenCif", "TenCif" },
                        { "userThucHienId", "UserThucHienId" },
                        { "ngayCapNhatDuLieu", "NgayCapNhatDuLieu" }
                    };

                    if (!string.IsNullOrEmpty(clientSortColumn) && columnMapping.TryGetValue(clientSortColumn, out var serverSortColumn))
                    {
                        query = query.OrderBy($"{serverSortColumn} {sortDirection}");
                    }
                    else
                    {
                        query = query.OrderByDescending(kh => kh.NgayCapNhatDuLieu); // Sắp xếp mặc định
                    }
                }
                else
                {
                    query = query.OrderByDescending(kh => kh.NgayCapNhatDuLieu); // Sắp xếp mặc định
                }

                // --- 7. PHÂN TRANG VÀ CHUYỂN ĐỔI DỮ LIỆU ---
                var pagedData = await query
                    .Skip(dtRequest.Start)
                    .Take(dtRequest.Length)
                    .Select(kh => new
                    {
                        soCif = kh.SoCif,
                        tenCif = kh.TenCif,
                        userThucHienId = kh.UserThucHienId,
                        ngayCapNhatDuLieu = kh.NgayCapNhatDuLieu
                        // Giữ nguyên định dạng ngày tháng như logic gốc
                        //ngayCapNhatDuLieu = kh.NgayCapNhatDuLieu.ToString("dd/MM/yyyy HH:mm"),
                    })
                    .ToListAsync();

                // --- 8. ĐÓNG GÓI PHẢN HỒI ---
                var response = new DataTablesResponse<object>
                {
                    Draw = dtRequest.Draw,
                    RecordsTotal = recordsTotal,
                    RecordsFiltered = recordsFiltered,
                    Data = pagedData.Cast<object>().ToList()
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải dữ liệu KhachHangDN cho DataTables.");
                return Json(new DataTablesResponse<object>
                {
                    Draw = dtRequest?.Draw ?? 0,
                    Error = "Có lỗi xảy ra phía máy chủ."
                });
            }
        }

        // [HttpPost]
        // [Route("LoadData")] // Đổi tên action cho rõ ràng hơn hoặc giữ nguyên GetKhachHangDataTable
        // public async Task<IActionResult> LoadData([FromBody] DataTablesRequest dtRequest)
        // {
        //     // Kiểm tra nếu dtRequest bị null do model binding thất bại
        //     if (dtRequest == null)
        //     {
        //         _logger.LogError("DataTablesRequest (dtRequest) is null. Model binding failed. Check client request payload, Content-Type, and DTO structure (especially Columns and Order properties, which should be Lists).");
        //         return Json(new DataTablesResponse<object>
        //         {
        //             Draw = 0, // Không thể lấy draw từ request nếu dtRequest null
        //             RecordsTotal = 0,
        //             RecordsFiltered = 0,
        //             Data = [],
        //             Error = "Lỗi server: Định dạng request không hợp lệ hoặc không thể bind model."
        //         });
        //     }

        //     try
        //     {
        //         var draw = dtRequest.Draw;
        //         var start = dtRequest.Start;
        //         var length = dtRequest.Length;
        //         // searchValue sẽ được sử dụng với EF.Functions.Like, không cần ToLower() ở đây
        //         // vì Like thường tôn trọng collation của DB (thường là case-insensitive).
        //         // Nếu bạn muốn ép case-insensitive một cách tường minh và DB của bạn là case-sensitive,
        //         // bạn có thể ToLower() cả cột và searchValue trong EF.Functions.Like,
        //         // ví dụ: EF.Functions.Like(kh.SoCif.ToLower(), $"%{searchValue.ToLower()}%")
        //         // nhưng hãy thử cách đơn giản trước.
        //         var searchValue = dtRequest.Search?.Value ?? string.Empty;

        //         //var searchValue = dtRequest.Search?.Value?.ToLower() ?? string.Empty;

        //         // Thiết lập sắp xếp mặc định phía server.
        //         // Giá trị này sẽ được sử dụng nếu client không gửi thông tin sắp xếp,
        //         // hoặc nếu thông tin sắp xếp từ client không hợp lệ (ví dụ: cột không cho phép sắp xếp).
        //         string sortColumn = "NgayCapNhatDuLieu"; // Mặc định
        //         string sortColumnDirection = "desc"; // Mặc định

        //         // Điều chỉnh logic sắp xếp để phù hợp với List<DataTablesOrder> và List<DataTablesColumn>
        //         if (dtRequest.Order != null && dtRequest.Order.Count > 0)
        //         {
        //             var firstOrder = dtRequest.Order.First(); // Order giờ là List<DataTablesOrder>
        //             if (dtRequest.Columns != null && firstOrder.Column < dtRequest.Columns.Count && firstOrder.Column >= 0)
        //             {
        //                 var col = dtRequest.Columns[firstOrder.Column]; // Columns giờ là List<DataTablesColumn>
        //                 if (col != null && !string.IsNullOrEmpty(col.Name) && col.Orderable)
        //                 {
        //                     sortColumn = col.Name;
        //                     sortColumnDirection = firstOrder.Dir?.ToLower() ?? "asc";
        //                 }
        //             }
        //         }

        //         IQueryable<KhachHangDN> query = _context.KhachHangDNs.AsQueryable();
        //         int recordsTotal = await query.CountAsync();

        //         if (!string.IsNullOrEmpty(searchValue))
        //         {
        //             //Tìm kiếm gần đúng theo SoCif hoặc TenCif
        //             string pattern = $"%{searchValue}%"; // Chuẩn bị pattern cho LIKE
        //             query = query.Where(kh =>
        //                 (kh.SoCif != null && EF.Functions.Like(kh.SoCif, pattern)) ||
        //                 (kh.TenCif != null && EF.Functions.Like(kh.TenCif, pattern))
        //                 //||
        //                 //(kh.UserThucHienId != null && EF.Functions.Like(kh.UserThucHienId, pattern))
        //             );

        //         }

        //         int recordsFiltered = await query.CountAsync();

        //         if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
        //         {
        //             query = query.OrderBy($"{sortColumn} {sortColumnDirection}");
        //         }

        //         //var pagedData = await query.Skip(start).Take(length).ToListAsync();
        //         // Xử lý phân trang
        //         var queryAfterSkip = query.Skip(start);
        //         List<KhachHangDN> pagedData;

        //         if (length > 0) // Nếu length > 0, áp dụng Take
        //         {
        //             pagedData = await queryAfterSkip.Take(length).ToListAsync();
        //         }
        //         else // Nếu length là -1 (hoặc <=0), lấy tất cả các bản ghi sau khi Skip
        //         {
        //             pagedData = await queryAfterSkip.ToListAsync();
        //         }

        //         var data = pagedData.Select(kh => new
        //         {
        //             kh.SoCif,
        //             kh.TenCif,
        //             kh.UserThucHienId,
        //             NgayCapNhatDuLieu = kh.NgayCapNhatDuLieu.ToString("dd/MM/yyyy HH:mm"),
        //         }).ToList();

        //         return Json(new DataTablesResponse<object>
        //         {
        //             Draw = draw,
        //             RecordsTotal = recordsTotal,
        //             RecordsFiltered = recordsFiltered,
        //             Data = data.Cast<object>().ToList()
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Lỗi khi tải dữ liệu cho DataTables.");
        //         // Sử dụng dtRequest.Draw nếu dtRequest không null, ngược lại dùng 0
        //         return Json(new DataTablesResponse<object>
        //         {
        //             Draw = dtRequest?.Draw ?? 0,
        //             RecordsTotal = 0,
        //             RecordsFiltered = 0,
        //             //Data = new List<object>(),
        //             Data = [],
        //             Error = "Đã có lỗi xảy ra trong quá trình xử lý yêu cầu."
        //         });
        //     }
        // }

        #endregion
    }
}
