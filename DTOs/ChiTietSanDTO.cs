using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DatSan.DTOs
{
    public class ChiTietSanDTO
    {
        public int SanID { get; set; }        // ID sân
        public string SanSo { get; set; }    // Số sân (hoặc tên sân)
        public decimal GiaMoiGio { get; set; } // Giá thuê sân mỗi giờ
    }
}