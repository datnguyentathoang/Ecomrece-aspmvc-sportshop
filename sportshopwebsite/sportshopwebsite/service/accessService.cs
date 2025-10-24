using sportshopwebsite.Helper;
using sportshopwebsite.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers; 
using System.Web.Script.Serialization; 

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


        public ApiResponse LoginUser(string Email, string Password)
        {
            var user = _db.Users.FirstOrDefault(u => u.Email == Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.Password))
            {
                return new ApiResponse(false, "Email hoặc mật khẩu không đúng!");
            }


            var keyToken = _db.KeyTokens.FirstOrDefault(kt => kt.UserId == user.UserId);


            string accessToken, refreshToken;
            DateTime refreshTokenExpiry;

            if (keyToken == null)
            {
                
                var newKeyPair = JwtHelper.GenerateRsaKeyPair();
                if (string.IsNullOrEmpty(newKeyPair.Key))
                {
                    throw new Exception("❌ Lỗi: newKeyPair.Key (private key) bị null khi tạo mới keypair!");
                }

                if (keyToken != null && string.IsNullOrEmpty(keyToken.PrivateKey))
                {
                    throw new Exception("❌ Lỗi: privateKeyXml bị null trong database!");
                }

                GenerateAccessAndRefreshTokens(user.UserId, newKeyPair.Key, out accessToken, out refreshToken, out refreshTokenExpiry);

        
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
                string privateKeyXml = keyToken.PrivateKey;

                if (string.IsNullOrEmpty(privateKeyXml))
                {
                    return new ApiResponse(false, "Private key không tồn tại. Vui lòng đăng ký lại.");
                }

                GenerateAccessAndRefreshTokens(user.UserId, privateKeyXml, out accessToken, out refreshToken, out refreshTokenExpiry);

                keyToken.CurrentRefreshToken = refreshToken;
                keyToken.UpdatedAt = DateTime.Now;
            }

         
            _db.SubmitChanges();

            return new ApiResponse(true, "Đăng nhập thành công!", new
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
            });
        }


        public ApiResponse RegisterUser(string FullName, string Email, string Password, string PhoneNumber, string Address)
        {
            var existingUser = _db.Users.FirstOrDefault(u => u.Email == Email);
            if (existingUser != null)
            {
                return new ApiResponse(false, "Email đã tồn tại!");
            }

            // 1. Tạo User
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(Password);

            User user = new User
            {
                FullName = FullName,
                Email = Email,
                Password = hashedPassword,
                RoleId = 2, 
                PhoneNumber = PhoneNumber,
                Address = Address,
                Status = true,
                CreatedAt = DateTime.Now
            };

            _db.Users.InsertOnSubmit(user);

            _db.SubmitChanges();
            int newUserId = user.UserId;

            var keyPair = JwtHelper.GenerateRsaKeyPair();
            string privateKeyXml = keyPair.Key;
            string publicKeyXml = keyPair.Value;

            GenerateAccessAndRefreshTokens(newUserId, privateKeyXml, out string accessToken, out string refreshToken, out DateTime refreshTokenExpiry);


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
   
            _db.SubmitChanges();


            return new ApiResponse(true, "Đăng ký thành công!", new
            {
                userId = newUserId,
                accessToken = accessToken,
                refreshToken = refreshToken,
                expiresAt = refreshTokenExpiry.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        private void GenerateAccessAndRefreshTokens(int userId, string privateKeyXml, out string accessToken, out string refreshToken, out DateTime refreshTokenExpiry)
        {
            if (string.IsNullOrEmpty(privateKeyXml))
            {
                throw new ArgumentException("PrivateKey cannot be null or empty.");
            }

            var jwtHelper = new JwtHelper(privateKeyXml);
            accessToken = jwtHelper.GenerateToken(userId, "access");
            refreshToken = jwtHelper.GenerateToken(userId, "refresh");

            refreshTokenExpiry = JwtHelper.GetTokenExpiryTime(refreshToken);
        }

        public ApiResponse LogoutUser(string refreshToken)
        {

            var keyToken = _db.KeyTokens
                .FirstOrDefault(kt => kt.CurrentRefreshToken == refreshToken);

            if (keyToken == null)
            {
        
                return new ApiResponse(false, "Refresh Token không hợp lệ hoặc không tồn tại.");
            }

            keyToken.CurrentRefreshToken = null;

         
            keyToken.UpdatedAt = DateTime.Now;
            _db.SubmitChanges();

            return new ApiResponse(true, "Đăng xuất thành công. Phiên đã bị hủy.");
        }
    }
}