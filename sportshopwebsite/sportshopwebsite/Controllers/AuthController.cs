using sportshopwebsite.Helper;
using sportshopwebsite.Models;
using System;
using System.IO; // Cần thiết cho Server.MapPath và Directory
using System.Linq;
using System.Web.Mvc;
// Đảm bảo bạn đã thêm BCrypt.Net-Next và các gói JWT

namespace sportshopwebsite.Controllers
{
    public class AuthController : Controller
    {
        private SportShopDataContext db = new SportShopDataContext();

        // GET: Auth
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Signup(string HoTen, string Email, string MatKhau, string SoDienThoai, string DiaChi)
        {
            try
            {
                // 1. Kiểm tra email tồn tại
                var existingUser = db.NguoiDungs.FirstOrDefault(u => u.Email == Email);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "Email đã tồn tại!" });
                }

                // 2. Mã hoá mật khẩu
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(MatKhau);

                // 3. Tạo người dùng mới và lưu lần 1 để lấy MaNguoiDung (ID)
                NguoiDung user = new NguoiDung
                {
                    HoTen = HoTen,
                    Email = Email,
                    MatKhau = hashedPassword,
                    MaVaiTro = 2, // Mặc định là khách hàng
                    SoDienThoai = SoDienThoai,
                    DiaChi = DiaChi,
                    TrangThai = true,
                    NgayTao = DateTime.Now
                };

                db.NguoiDungs.InsertOnSubmit(user);
                db.SubmitChanges();
                int newUserId = user.MaNguoiDung;

                // 4. Tạo cặp Khóa RSA và cập nhật Public Key
                string privateKeyXml;
                string publicKeyXml;

                // Đường dẫn lưu Private Key (chỉ server truy cập)
                string keyFolderPath = Server.MapPath("~/App_Data/PrivateKeys");
                if (!Directory.Exists(keyFolderPath))
                {
                    Directory.CreateDirectory(keyFolderPath);
                }

                RSAKeyHelper.GenerateKeys(newUserId, keyFolderPath, out privateKeyXml, out publicKeyXml);

                // Cập nhật Public Key vào người dùng và lưu lần 2
                user.PublicKey = publicKeyXml;
                db.SubmitChanges();

                // 5. Tạo JWT Access Token và Refresh Token
                var jwtHelper = new JwtHelper(privateKeyXml);

                string accessToken = jwtHelper.GenerateAccessToken(newUserId);
                string refreshToken = jwtHelper.GenerateRefreshToken(newUserId);

                // 6. LƯU REFRESH TOKEN VÀO DATABASE (BẢNG TheLamMoiToken)

                // Tính thời gian hết hạn (7 ngày từ UtcNow)
                DateTime refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

                TheLamMoiToken refreshRecord = new TheLamMoiToken
                {
                    MaNguoiDung = newUserId,
                    Token = refreshToken,
                    HetHanVao = refreshTokenExpiry,
                    DaHuy = false,
                    NgayTao = DateTime.Now
                };

                db.TheLamMoiTokens.InsertOnSubmit(refreshRecord);
                db.SubmitChanges(); // Lưu bản ghi Refresh Token

                // 7. Trả về Token cho Client
                return Json(new
                {
                    success = true,
                    message = "Đăng ký thành công!",
                    data = new
                    {
                        userId = newUserId,
                        accessToken = accessToken,
                        refreshToken = refreshToken,
                        expiresAt = refreshTokenExpiry.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // TODO: Nên log lỗi chi tiết (ex)
                return Json(new { success = false, message = "Lỗi trong quá trình đăng ký: " + ex.Message });
            }
        }
    }
}