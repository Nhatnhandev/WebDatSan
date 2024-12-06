using DatSan.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using PagedList;
using PagedList.Mvc;
using DatSan.DTOs;
using System.Diagnostics;
using Microsoft.Ajax.Utilities;
namespace DatSan.Controllers
{
    public class DatSanController : Controller
    {
        // GET: DatSan
        DataClasses1DataContext data = new DataClasses1DataContext();

        public ActionResult TrangChu()
        {
            // Lấy danh sách sân để hiển thị trong giao diện
            var sanList = data.SanCauLongs.Take(6).ToList(); // Lấy tối đa 6 sân hiển thị ở trang chủ
            ViewBag.SanCauLongs = sanList;

            return View();
        }
        public ActionResult Index(int? page)
        {
            int pagedNumber = page ?? 1; // Trang hiện tại, mặc định là 1
            int pageSize = 15; // 7 hàng * 3 sân mỗi hàng = 21 sân

            var sanList = data.SanCauLongs
                .OrderBy(s => s.TenSan) // Sắp xếp theo tên sân
                .ToPagedList(pagedNumber, pageSize);

            return View(sanList);
        }
        public ActionResult Search(string query = "", decimal? minPrice = null, decimal? maxPrice = null, int? page = 1)
        {
            // Số lượng bản ghi trên mỗi trang
            int pageSize = 15;
            int pageNumber = page ?? 1;

            // Truy vấn cơ sở dữ liệu
            var searchResults = data.SanCauLongs.AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(query))
            {
                searchResults = searchResults.Where(s =>
                    s.TenSan.ToLower().Contains(query.ToLower()) ||
                    s.DiaDiem.ToLower().Contains(query.ToLower()));
            }

            // Lọc theo giá tiền
            if (minPrice.HasValue)
            {
                searchResults = searchResults.Where(s => s.GiaTrungBinhMoiGio >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                searchResults = searchResults.Where(s => s.GiaTrungBinhMoiGio <= maxPrice.Value);
            }

            // Sắp xếp kết quả theo tên
            searchResults = searchResults.OrderBy(s => s.TenSan);

            // Chuyển đổi danh sách sang dạng phân trang
            var pagedResults = searchResults.ToPagedList(pageNumber, pageSize);

            // Truyền thông tin vào ViewBag để hiển thị trong View
            ViewBag.Query = query;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(pagedResults);
        }
        public ActionResult LichTuan(int? sanID, DateTime? startDate)
        {
            if (!sanID.HasValue)
            {
                return RedirectToAction("Index", "DatSan");
            }

            // Sử dụng startDate nếu có, nếu không thì mặc định là ngày hiện tại
            DateTime start = startDate ?? DateTime.Today;
            DateTime endDate = start.AddDays(6); // Ngày cuối tuần

            // Lấy danh sách chi tiết sân
            var danhSachChiTietSan = data.ChiTietSans
                .Where(c => c.SanID == sanID)
                .ToList();

            // Lấy danh sách lịch tuần
            var lichTuan = data.LichSans
                .Where(l => l.SanID == sanID && l.Ngay >= start && l.Ngay <= endDate)
                .ToList();

            ViewBag.SanID = sanID;
            ViewBag.StartDate = start;
            ViewBag.EndDate = endDate;
            ViewBag.ThoiGianBatDau = 5;
            ViewBag.ThoiGianKetThuc = 22;

            return View(Tuple.Create(danhSachChiTietSan, lichTuan));
        }

        [HttpPost]
        public ActionResult LichTuanPost(List<string> thoiGianChon, int? sanID, string hoTen, string soDienThoai)
        {
            if (Session["TaiKhoan"] == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thực hiện đặt sân.";
                return RedirectToAction("DangNhap", "NguoiDung");
            }

            if (!sanID.HasValue)
            {
                TempData["ErrorMessage"] = "ID sân không được để trống.";
                return RedirectToAction("LichTuan", "DatSan");
            }

            var chiTietSan = data.ChiTietSans.FirstOrDefault(c => c.SanID == sanID.Value);
            if (chiTietSan == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin sân.";
                return RedirectToAction("LichTuan", "DatSan");
            }

            decimal giaMoiGio = chiTietSan.GiaMoiGio;

            if (thoiGianChon == null || !thoiGianChon.Any())
            {
                TempData["ErrorMessage"] = "Vui lòng chọn thời gian.";
                return RedirectToAction("LichTuan", new { sanID });
            }

            TimeSpan tongThoiGian = TimeSpan.Zero;
            decimal tongTien = 0;

            foreach (var tg in thoiGianChon)
            {
                var parts = tg.Split('-');
                var gioBatDau = TimeSpan.Parse(parts[0]);
                var gioKetThuc = TimeSpan.Parse(parts[1]);

                tongThoiGian += gioKetThuc - gioBatDau;
                tongTien += (decimal)(gioKetThuc - gioBatDau).TotalHours * giaMoiGio;
            }

            // Lưu vào bảng DonDat và ChiTietDonDat
            var donDat = new DonDat
            {
                TaiKhoanID = (int)Session["TaiKhoanID"], // Lấy ID tài khoản từ Session
                NgayDat = DateTime.Now
            };

            data.DonDats.InsertOnSubmit(donDat);
            data.SubmitChanges();

            foreach (var tg in thoiGianChon)
            {
                var gioBatDau = TimeSpan.Parse(tg.Split('-')[0]);
                var gioKetThuc = TimeSpan.Parse(tg.Split('-')[1]);

                var chiTietDonDat = new ChiTietDonDat
                {
                    DonDatID = donDat.DonDatID,
                    SanID = sanID.Value,
                    ThoiGianBatDau = DateTime.Today.Add(gioBatDau),
                    ThoiGianKetThuc = DateTime.Today.Add(gioKetThuc),
                    HoTen = hoTen,
                    SoDienThoai = soDienThoai
                };

                data.ChiTietDonDats.InsertOnSubmit(chiTietDonDat);
            }

            data.SubmitChanges();

            TempData["DonDatID"] = donDat.DonDatID;
            TempData["TongTien"] = tongTien;

            return RedirectToAction("ThanhToan", "DatSan");
        }



        public ActionResult ChiTietThanhToan()
        {
            var thoiGianChon = TempData["ThoiGianChon"] as List<string>;
            var tongThoiGian = TempData["TongThoiGian"]?.ToString();
            var tongTien = TempData["TongTien"];
            var sanID = TempData["SanID"] as int?;  // Lấy SanID từ TempData

            ViewBag.ThoiGianChon = thoiGianChon;
            ViewBag.TongThoiGian = tongThoiGian;
            ViewBag.TongTien = tongTien;

            return View();

        }

        [HttpPost]
        public ActionResult ChiTietThanhToan(string hoTen, string soDienThoai, int sanID, int TaiKhoanID)
        {
            if (string.IsNullOrEmpty(hoTen) || string.IsNullOrEmpty(soDienThoai))
            {
                TempData["ErrorMessage"] = "Họ tên và số điện thoại không được để trống.";
                return RedirectToAction("ChiTietThanhToan");
            }

            try
            {
                var thoiGianChon = TempData["ThoiGianChon"] as List<string>;
                var tongTien = (decimal)TempData["TongTien"];

                // Lưu thông tin đơn đặt
                var donDat = new DonDat
                {
                    TaiKhoanID = TaiKhoanID,
                    NgayDat = DateTime.Now
                };

                data.DonDats.InsertOnSubmit(donDat);
                data.SubmitChanges();

                // Lưu chi tiết đơn đặt sân
                foreach (var tg in thoiGianChon)
                {
                    var gioBatDau = TimeSpan.Parse(tg.Split('-')[0]);
                    var gioKetThuc = TimeSpan.Parse(tg.Split('-')[1]);

                    var chiTietDonDat = new ChiTietDonDat
                    {
                        DonDatID = donDat.DonDatID,
                        SanID = sanID,
                        ThoiGianBatDau = DateTime.Today.Add(gioBatDau),
                        ThoiGianKetThuc = DateTime.Today.Add(gioKetThuc),
                        HoTen = hoTen,
                        SoDienThoai = soDienThoai
                    };

                    data.ChiTietDonDats.InsertOnSubmit(chiTietDonDat);
                }

                data.SubmitChanges();

                // Lưu ID đơn đặt và tổng tiền vào TempData để dùng cho thanh toán
                TempData["DonDatID"] = donDat.DonDatID;
                TempData["TongTien"] = tongTien;

                return RedirectToAction("ThanhToan");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi lưu thông tin: " + ex.Message;
                return RedirectToAction("ChiTietThanhToan");
            }
        }

        [HttpPost]
        public ActionResult ThanhToan(int donDatID, string phuongThucThanhToan)
        {
            var soTienThanhToan = (decimal?)Session["TongTien"];
            var thanhToan = new ThanhToan
            {
                DonDatID = donDatID,
                NgayThanhToan = DateTime.Now,
                SoTien = soTienThanhToan.Value,
                PhuongThucThanhToan = phuongThucThanhToan,
                TrangThai = "Đã Thanh Toán"
            };

            data.ThanhToans.InsertOnSubmit(thanhToan);
            data.SubmitChanges();

            TempData["SuccessMessage"] = "Thanh toán thành công!";
            return RedirectToAction("Index", "DatSan");


        }
    }
}