using Microsoft.IdentityModel.Tokens;
using sportshopwebsite.Models;
using System;
using System.Collections.Generic;
using System.EnterpriseServices.Internal;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace sportshopwebsite.Controllers
{
    public class TestController : Controller
    {
        // GET: Test
        public ActionResult Index()
        {
            try
            {
                using (var db = new SportShopDataContext())
                {
                    var nguoiDungs = db.NguoiDungs.ToList();
                    return Content("✅ Kết nối thành công! Có " + nguoiDungs.Count + " người dùng trong database.");
                }
            }
            catch (Exception ex)
            {
                return Content("❌ Lỗi kết nối database: " + ex.Message);
            }
        }
   
    
    }
}