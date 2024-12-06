using DatSan.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace DatSan.Controllers
{
    public class NguoiDungController : Controller
    {
        // GET: NguoiDung
        DataClasses1DataContext data = new DataClasses1DataContext();
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult DangNhap()
        {
            return View();
        }
        [HttpPost]
        public ActionResult DangNhap(FormCollection formCollection)
        {
            var tendn = formCollection["TenDN"];
            var matkhau = formCollection["MatKhau"];
            if(string.IsNullOrEmpty(tendn) )
            {
                ViewData["Loi1"] = "Phai nhap ten dang nhap";
            }
            else if(string.IsNullOrEmpty(matkhau) )
            {
                ViewData["Loi2"] = "Phai nhap mat khau";
            }
            else
            {
                TaiKhoan tk = data.TaiKhoans.SingleOrDefault(n=>n.TenDangNhap==tendn && n.MatKhau==matkhau);
                if(tk != null)
                {
                    ViewBag.ThongBao = "dang nhap thanh cong";
                    Session["TaiKhoan"] = tk;
                    return RedirectToAction("Index","DatSan");
                }
                else
                {
                    ViewBag.ThongBao = "Sai thong tin tai khoan";
                }
            }
            return this.DangNhap();
        }
        public ActionResult DangKi()
        {
            return View();
        }
        [HttpPost]
        public ActionResult DangKi(FormCollection formCollection,TaiKhoan tk)
        {
            var tendn = formCollection["TenDN"];
            var matkhau = formCollection["MatKhau"];
            var nhaplaimatkhau =formCollection["NhapLaiMatKhau"];
            var email = formCollection["Email"];
            if (String.IsNullOrEmpty(tendn))
            {
                ViewData["Loi1"] = "Phải nhập tên đăng nhập";
            }
            else if (String.IsNullOrEmpty(matkhau))
            {
                ViewData["Loi2"] = "Phải nhập mật khẩu";
            }
            else if (String.IsNullOrEmpty(nhaplaimatkhau))
            {
                ViewData["Loi3"] = "Phải nhập lại mật khẩu";
            }
            else
            {
                var TaiKhoanTonTai = data.TaiKhoans.FirstOrDefault(n => n.TenDangNhap == tendn);
                if (TaiKhoanTonTai == null)
                {
                    tk.TenDangNhap = tendn;
                    tk.MatKhau = matkhau;
                    tk.Email = email;
                    tk.NgayTao = DateTime.Now;
                    data.TaiKhoans.InsertOnSubmit(tk);
                    data.SubmitChanges();
                    ViewBag.Thongbao = "Đăng ký thành công";
                }
                else
                {
                    ViewBag.Thongbao = "Tài khoản đã tồn tại";
                }
            }
            return this.DangKi();
        }
        public ActionResult DangXuat()
        {
            Session.Clear(); // Xóa tất cả thông tin trong session
            FormsAuthentication.SignOut(); // Đăng xuất người dùng nếu dùng FormsAuthentication
            return RedirectToAction("Index", "DatSan");
        }
    }
}