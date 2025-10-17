using sportshopwebsite.Helper;
using sportshopwebsite.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers; // Dùng cho BCrypt
using System.Web.Script.Serialization; // Cần thiết cho JSON (Nếu bạn muốn triển khai logic Replay Attack)

namespace sportshopwebsite.service
{
    public class accessService
    {
        private readonly SportShopDataContext _db;
        private readonly HttpServerUtilityBase _server;

        public accessService(SportShopDataContext db, HttpServerUtilityBase server)
        {
            _db = db;
            _server = server;
        }

        // -------------------------------------------------------------------
        // LOGIC ĐĂNG NHẬP (LOGIN)
        // -------------------------------------------------------------------
        public object LoginUser(string Email, string Password)
        {
            // 1. Tìm User và Xác thực mật khẩu
            var user = _db.Users.FirstOrDefault(u => u.Email == Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.Password))
            {
                return new { success = false, message = "Email hoặc mật khẩu không đúng!" };
            }

            // 2. Tìm KeyTokens hiện tại của User
            var keyToken = _db.KeyTokens.FirstOrDefault(kt => kt.UserId == user.UserId);

            // Khai báo ngoài scope để sử dụng sau này
            string accessToken, refreshToken;
            DateTime refreshTokenExpiry;

            if (keyToken == null)
            {
                // TRƯỜNG HỢP 1: User đăng ký từ hệ thống cũ HOẶC bản ghi KeyTokens bị xóa

                // a) Tạo cặp khóa và token mới
                var newKeyPair = JwtHelper.GenerateRsaKeyPair();
                GenerateAccessAndRefreshTokens(user.UserId, newKeyPair.Key, out accessToken, out refreshToken, out refreshTokenExpiry);

                // b) Khởi tạo và lưu bản ghi KeyTokens mới
                keyToken = new KeyToken
                {
                    UserId = user.UserId,
                    PrivateKey = newKeyPair.Key,
                    PublicKey = newKeyPair.Value,
                    RefreshTokensUsed = "[]",
                    CurrentRefreshToken = refreshToken,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _db.KeyTokens.InsertOnSubmit(keyToken);
            }
            else
            {
                // TRƯỜNG HỢP 2: Đã có KeyTokens -> Chỉ cần tạo token mới và cập nhật

                string privateKeyXml = keyToken.PrivateKey;

                // a) Tạo token mới
                GenerateAccessAndRefreshTokens(user.UserId, privateKeyXml, out accessToken, out refreshToken, out refreshTokenExpiry);

                // b) CẬP NHẬT Refresh Token hiện tại
                keyToken.CurrentRefreshToken = refreshToken;
                keyToken.UpdatedAt = DateTime.Now;
            }

            // 3. Submit thay đổi (Áp dụng cho cả 2 trường hợp)
            _db.SubmitChanges();

            return new
            {
                success = true,
                message = "Đăng nhập thành công!",
                data = new
                {
                    user = new
                    {
                        id = user.UserId,
                        fullName = user.FullName,
                        email = user.Email
                    },
                    accessToken,
                    refreshToken,
                    expiresAt = refreshTokenExpiry.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
        }

        // -------------------------------------------------------------------
        // LOGIC ĐĂNG KÝ (SIGNUP)
        // -------------------------------------------------------------------
        public object RegisterUser(string FullName, string Email, string Password, string PhoneNumber, string Address)
        {
            var existingUser = _db.Users.FirstOrDefault(u => u.Email == Email);
            if (existingUser != null)
            {
                return new { success = false, message = "Email đã tồn tại!" };
            }

            // 1. Tạo User
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(Password);

            User user = new User
            {
                FullName = FullName,
                Email = Email,
                Password = hashedPassword,
                RoleId = 2, // Khách hàng
                PhoneNumber = PhoneNumber,
                Address = Address,
                Status = true,
                CreatedAt = DateTime.Now
            };

            _db.Users.InsertOnSubmit(user);
            // SubmitChanges lần 1 để lấy được newUserId
            _db.SubmitChanges();
            int newUserId = user.UserId;

            // 2. Tạo cặp khóa RSA và Token
            var keyPair = JwtHelper.GenerateRsaKeyPair();
            string privateKeyXml = keyPair.Key;
            string publicKeyXml = keyPair.Value;

            GenerateAccessAndRefreshTokens(newUserId, privateKeyXml, out string accessToken, out string refreshToken, out DateTime refreshTokenExpiry);

            // 3. Lưu Khóa và Refresh Token vào bảng KeyTokens
            KeyToken keyToken = new KeyToken
            {
                UserId = newUserId,
                PrivateKey = privateKeyXml,
                PublicKey = publicKeyXml,
                RefreshTokensUsed = "[]",
                CurrentRefreshToken = refreshToken,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _db.KeyTokens.InsertOnSubmit(keyToken);
            // SubmitChanges lần 2 để lưu KeyTokens
            _db.SubmitChanges();


            return new
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
            };
        }

        // -------------------------------------------------------------------
        // HÀM HỖ TRỢ
        // -------------------------------------------------------------------
        private void GenerateAccessAndRefreshTokens(int userId, string privateKeyXml, out string accessToken, out string refreshToken, out DateTime refreshTokenExpiry)
        {
            var jwtHelper = new JwtHelper(privateKeyXml);
            accessToken = jwtHelper.GenerateToken(userId, "access");
            refreshToken = jwtHelper.GenerateToken(userId, "refresh");

            // Lấy thời gian hết hạn từ JwtHelper để đồng bộ
            refreshTokenExpiry = JwtHelper.GetTokenExpiryTime(refreshToken);
        }

        // -------------------------------------------------------------------
        // LOGIC ĐĂNG XUẤT (LOGOUT)
        // -------------------------------------------------------------------
        public object LogoutUser(string refreshToken)
        {
            // 1. Tìm bản ghi KeyTokens có CurrentRefreshToken trùng khớp
            var keyToken = _db.KeyTokens
                .FirstOrDefault(kt => kt.CurrentRefreshToken == refreshToken);

            if (keyToken == null)
            {
                // Nếu không tìm thấy, có thể token đã bị hủy hoặc không hợp lệ
                return new { success = false, message = "Refresh Token không hợp lệ hoặc không tồn tại." };
            }

            // 2. Hủy phiên: Đặt CurrentRefreshToken về NULL
            keyToken.CurrentRefreshToken = null;

            // 3. Lưu thay đổi
            keyToken.UpdatedAt = DateTime.Now;
            _db.SubmitChanges();

            return new { success = true, message = "Đăng xuất thành công. Phiên đã bị hủy." };
        }
    }
}