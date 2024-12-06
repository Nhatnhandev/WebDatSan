using DatSan.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Configuration;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using PagedList;
using PagedList.Mvc;
namespace DatSan.Controllers
{
    public class AdminController : Controller
    {
        // Database context
        DataClasses1DataContext data = new DataClasses1DataContext();

        public ActionResult Index()
        {
            return View();
        }

        // Add Court View
        public ActionResult AddCourt()
        {
            var DiaDiemList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Bình Dương", Value = "BinhDuong" },
                new SelectListItem { Text = "Hồ Chí Minh", Value = "HoChiMinh" },
                new SelectListItem { Text = "Đà Nẵng", Value = "DaNang" }
            };

            ViewBag.DiaDiem = new SelectList(DiaDiemList, "Value", "Text");
            return View();
        }

        // Add Court - POST
        [HttpPost]
        public ActionResult AddCourt(FormCollection collection, SanCauLong Scl, HttpPostedFileBase HinhAnh)
        {
            var Sanid = collection["Sanid"];
            var TenSan = collection["TenSan"];
            var DiaChi = collection["DiaChi"];
            var SoDienThoai = collection["SoDienThoai"];
            var Mota = collection["Mota"];
            var DiaDiem = collection["DiaDiem"];

            if (String.IsNullOrEmpty(Sanid))
            {
                ViewData["Loi1"] = "Mã sân không được để trống hoặc không hợp lệ";
            }
            else if (String.IsNullOrEmpty(TenSan))
            {
                ViewData["Loi2"] = "Tên sân không được để trống";
            }
            else if (String.IsNullOrEmpty(DiaChi))
            {
                ViewData["Loi3"] = "Địa chỉ không được để trống";
            }
            else if (String.IsNullOrEmpty(SoDienThoai))
            {
                ViewData["Loi4"] = "Số điện thoại không được để trống";
            }
            else
            {
                int parsedSanid;
                if (int.TryParse(Sanid, out parsedSanid))
                {
                    var SanTonTai = data.SanCauLongs.FirstOrDefault(n => n.SanID == parsedSanid);
                    if (SanTonTai == null)
                    {
                        Scl.SanID = parsedSanid;
                        Scl.TenSan = TenSan;
                        Scl.DiaChi = DiaChi;
                        Scl.SoDienThoai = SoDienThoai;
                        Scl.Mota = Mota;
                        Scl.DiaDiem = DiaDiem;

                        if (HinhAnh != null && HinhAnh.ContentLength > 0)
                        {
                            try
                            {
                                string fileName = Path.GetFileNameWithoutExtension(HinhAnh.FileName);
                                string extension = Path.GetExtension(HinhAnh.FileName);
                                string uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
                                string path = Path.Combine(Server.MapPath("~/Images/SanCauLong"), uniqueFileName);

                                string directoryPath = Path.GetDirectoryName(path);
                                if (!Directory.Exists(directoryPath))
                                {
                                    Directory.CreateDirectory(directoryPath);
                                }

                                HinhAnh.SaveAs(path);
                                Scl.HinhAnh = uniqueFileName;
                            }
                            catch (Exception ex)
                            {
                                ViewBag.ErrorMessage = "Có lỗi xảy ra khi tải ảnh lên: " + ex.Message;
                                return View(Scl);
                            }
                        }

                        data.SanCauLongs.InsertOnSubmit(Scl);
                        data.SubmitChanges();

                        ViewBag.Thongbao = "Thêm thành công";
                        return RedirectToAction("AddCourt");
                    }
                    else
                    {
                        ViewBag.Thongbao = "Lưu không thành công, sân đã tồn tại";
                    }
                }
                else
                {
                    ViewData["Loi1"] = "Mã sân không hợp lệ, vui lòng nhập số nguyên.";
                }
            }

            var DiaDiemList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Bình Dương", Value = "BinhDuong" },
                new SelectListItem { Text = "Hồ Chí Minh", Value = "HoChiMinh" },
                new SelectListItem { Text = "Đà Nẵng", Value = "DaNang" }
            };

            ViewBag.DiaDiem = new SelectList(DiaDiemList, "Value", "Text");
            return View(Scl);
        }

        // Display Courts
        public ActionResult DisplayCourts(int? page)
        {
            int pagedNumber = (page ?? 1);
            int pageSize = 7;
            return View(data.SanCauLongs.ToList().OrderBy(n => n.SanID).ToPagedList(pagedNumber, pageSize));
        }

        // Court Details
        public ActionResult CourtDetails(int sanId)
        {
            var san = data.SanCauLongs.FirstOrDefault(s => s.SanID == sanId);

            if (san == null)
            {
                return HttpNotFound("Không tìm thấy sân cầu lông này.");
            }

            // Gán thông tin vào ViewBag để sử dụng trong View
            ViewBag.SanID = sanId;
            ViewBag.SanTen = san.TenSan;

            // Lấy danh sách chi tiết sân
            var chiTietSanList = data.ChiTietSans.Where(c => c.SanID == sanId).ToList();

            return View(chiTietSanList);
        }
        private void UpdateGiaTrungBinhMoiGio(int sanId)
        {
            var chiTietSans = data.ChiTietSans.Where(c => c.SanID == sanId).ToList();

            if (chiTietSans.Any())
            {
                // Tính giá trung bình
                decimal giaTrungBinh = chiTietSans.Average(c => c.GiaMoiGio);

                // Cập nhật giá trung bình trong bảng SanCauLong
                var san = data.SanCauLongs.SingleOrDefault(s => s.SanID == sanId);
                if (san != null)
                {
                    san.GiaTrungBinhMoiGio = giaTrungBinh;
                    data.SubmitChanges();
                }
            }
            else
            {
                // Nếu không còn chi tiết sân, đặt giá trung bình thành null hoặc 0
                var san = data.SanCauLongs.SingleOrDefault(s => s.SanID == sanId);
                if (san != null)
                {
                    san.GiaTrungBinhMoiGio = null; // Hoặc = 0 nếu muốn mặc định là 0
                    data.SubmitChanges();
                }
            }
        }


        // Add Court Detail
        public ActionResult AddCourtDetail(int sanId)
        {
            ViewData["SanID"] = sanId;
            return View();
        }

        [HttpPost]
        public ActionResult AddCourtDetail(int sanId, FormCollection collection)
        {
            var giaMoiGio = collection["GiaMoiGio"];
            var SanSo=collection["SanSo"];

            if (String.IsNullOrEmpty(giaMoiGio) || !decimal.TryParse(giaMoiGio, out decimal parsedGiaMoiGio))
            {
                ViewData["Loi1"] = "Giá mỗi giờ không hợp lệ hoặc để trống";
            }
            else
            {
                var chiTietSan = new ChiTietSan
                {

                    SanID = sanId,
                    SanSo =SanSo,
                    GiaMoiGio = parsedGiaMoiGio,
                };

                data.ChiTietSans.InsertOnSubmit(chiTietSan);
                data.SubmitChanges();
                UpdateGiaTrungBinhMoiGio(sanId);
                ViewBag.Thongbao = "Thêm chi tiết sân thành công";
                return RedirectToAction("CourtDetails", new { sanId });
            }

            ViewBag.SanID = sanId;
            return View();
        }

        // Delete Court Detail
        public ActionResult DeleteCourtDetail(int  id)
        {
            var detail = data.ChiTietSans.SingleOrDefault(d => d.ChiTietSanID == id);
            if (detail == null)
            {
                return HttpNotFound();
            }

            data.ChiTietSans.DeleteOnSubmit(detail);
            data.SubmitChanges();
            UpdateGiaTrungBinhMoiGio(id);
            return RedirectToAction("CourtDetails", new { sanId = detail.SanID });
        }

        // Additional Methods
        public ActionResult DisplayBookings()
        {
            var bookings = data.DonDats.ToList();
            return View(bookings);
        }

        public ActionResult ManageUsers()
        {
            var users = data.TaiKhoans.ToList();
            return View(users);
        }

        public ActionResult Lichsan()
        {
            var lichsans = data.LichSans.ToList();
            return View(lichsans);
        }

        public ActionResult DisplayPayments()
        {
            var payments = data.ThanhToans.ToList();
            return View(payments);
        }

        public ActionResult DeleteCourt(int id)
        {
            var court = data.SanCauLongs.SingleOrDefault(s => s.SanID == id);
            if (court == null)
            {
                return HttpNotFound();
            }

            data.SanCauLongs.DeleteOnSubmit(court);
            data.SubmitChanges();

            return RedirectToAction("DisplayCourts");
        }
    }
}
