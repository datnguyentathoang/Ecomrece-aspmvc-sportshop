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
            return Content("✅ Test Controller is working! Time: " + DateTime.Now.ToString());
        }

        public ActionResult LoginDebug(string email = "test@example.com", string password = "password123")
        {
            try
            {
                using (var db = new SportShopDataContext())
                {
                    var result = "=== LOGIN DEBUG ===<br/>";
                    
                    // 1. Check user exists
                    var user = db.Users.FirstOrDefault(u => u.Email == email);
                    result += $"1. User found: {user != null}<br/>";
                    
                    if (user != null)
                    {
                        result += $"   - UserId: {user.UserId}<br/>";
                        result += $"   - Email: {user.Email}<br/>";
                        result += $"   - FullName: {user.FullName}<br/>";
                        result += $"   - HasPassword: {!string.IsNullOrEmpty(user.Password)}<br/>";
                        
                        // 2. Check password
                        bool passwordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
                        result += $"2. Password valid: {passwordValid}<br/>";
                        
                        if (passwordValid)
                        {
                            // 3. Check KeyToken
                            var keyToken = db.KeyTokens.FirstOrDefault(kt => kt.UserId == user.UserId);
                            result += $"3. KeyToken found: {keyToken != null}<br/>";
                            
                            if (keyToken != null)
                            {
                                result += $"   - UserId: {keyToken.UserId}<br/>";
                                result += $"   - HasPrivateKey: {!string.IsNullOrEmpty(keyToken.PrivateKey)}<br/>";
                                result += $"   - PrivateKey length: {keyToken.PrivateKey?.Length ?? 0}<br/>";
                                result += $"   - HasPublicKey: {!string.IsNullOrEmpty(keyToken.PublicKey)}<br/>";
                                
                                if (!string.IsNullOrEmpty(keyToken.PrivateKey))
                                {
                                    // 4. Test JwtHelper creation
                                    try
                                    {
                                        var jwtHelper = new sportshopwebsite.Helper.JwtHelper(keyToken.PrivateKey);
                                        result += $"4. JwtHelper created: SUCCESS<br/>";
                                        
                                        // 5. Test token generation
                                        var accessToken = jwtHelper.GenerateToken(user.UserId, "access");
                                        result += $"5. Access token generated: SUCCESS<br/>";
                                        result += $"   - Token length: {accessToken.Length}<br/>";
                                        
                                        var refreshToken = jwtHelper.GenerateToken(user.UserId, "refresh");
                                        result += $"6. Refresh token generated: SUCCESS<br/>";
                                        result += $"   - Token length: {refreshToken.Length}<br/>";
                                        
                                    }
                                    catch (Exception ex)
                                    {
                                        result += $"4. JwtHelper creation FAILED: {ex.Message}<br/>";
                                    }
                                }
                                else
                                {
                                    result += $"4. PrivateKey is NULL or EMPTY<br/>";
                                }
                            }
                            else
                            {
                                result += $"3. No KeyToken found for user<br/>";
                            }
                        }
                    }
                    else
                    {
                        result += $"1. User not found with email: {email}<br/>";
                    }
                    
                    return Content(result);
                }
            }
            catch (Exception ex)
            {
                return Content($"❌ Login Debug Error: {ex.Message}<br/>Stack: {ex.StackTrace}");
            }
        }
   
    
    }
}