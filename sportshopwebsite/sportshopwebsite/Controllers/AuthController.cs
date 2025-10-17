using sportshopwebsite.Filters;
using sportshopwebsite.Helper;
using sportshopwebsite.Models;
using sportshopwebsite.service;
using System;
using System.IO;
using System.Linq;
using System.Web.Helpers;
using System.Web.Mvc;
// Đảm bảo bạn đã thêm BCrypt.Net-Next và các gói JWT

namespace sportshopwebsite.Controllers
{
    public class AuthController : Controller
    {
        // Giả định SportShopDataContext là DataContext/DbContext của bạn
        private SportShopDataContext db = new SportShopDataContext();

        // GET: Auth
        [ApiAttribute(RequiredPermission = "1111")]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Signup()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Signup(string FullName, string Email, string Password, string PhoneNumber, string Address)
        {
            try
            {
                var authService = new accessService(db, Server);

                var result = authService.RegisterUser(FullName, Email, Password, PhoneNumber, Address);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi trong quá trình đăng ký: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult Login(string Email, string Password)
        {
            try
            {
                var authService = new accessService(db, Server);

                var result = authService.LoginUser(Email, Password);


                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi trong quá trình đăng nhập: " + ex.Message });
            }
        }



        [HttpPost]
        public ActionResult LogOut(string refreshToken = null)
        {
   
            if (string.IsNullOrEmpty(refreshToken))
            {
            
                refreshToken = Request.Cookies["refreshToken"]?.Value;
            }

            if (string.IsNullOrEmpty(refreshToken))
            {
                return Json(new { success = false, message = "Không tìm thấy Refresh Token để đăng xuất." }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var authService = new accessService(db, Server);
             
                var result = authService.LogoutUser(refreshToken);

   
                if (Request.Cookies["refreshToken"] != null)
                {
                    Response.Cookies["refreshToken"].Expires = DateTime.Now.AddDays(-1);
                }

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi trong quá trình đăng xuất: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}