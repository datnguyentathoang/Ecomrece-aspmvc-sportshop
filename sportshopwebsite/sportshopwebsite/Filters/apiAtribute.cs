using sportshopwebsite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
// Thêm thư viện để làm việc với JSON
using System.Web.Script.Serialization;

namespace sportshopwebsite.Filters
{
    // Đổi tên class sang PascalCase chuẩn C#
    public class ApiAttribute : ActionFilterAttribute
    {
        public string RequiredPermission { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Khởi tạo DataContext
            var db = new SportShopDataContext();
            var request = filterContext.HttpContext.Request;

            // 1. Lấy API Key
            var apiKey = request.Headers["x-api-key"] ?? request.QueryString["api_key"];
            if (string.IsNullOrEmpty(apiKey))
            {
                filterContext.Result = new HttpStatusCodeResult(403, "Access Denied: Missing API Key");
                return;
            }

            // 2. Xác thực API Key và Status
            // Sử dụng tên cột tiếng Anh: [Key], [Status]
            var keyInfo = db.ApiKeys.FirstOrDefault(k => k.Key == apiKey && k.Status == true);
            if (keyInfo == null)
            {
                filterContext.Result = new HttpStatusCodeResult(403, "Access Denied: Invalid or inactive API Key");
                return;
            }

            // 3. Kiểm tra Quyền hạn (Permissions)
            if (!string.IsNullOrEmpty(RequiredPermission))
            {
                // Giải mã chuỗi JSON permissions thành mảng chuỗi (List<string>)
                var serializer = new JavaScriptSerializer();
                List<string> allowedPermissions;

                try
                {
                    // Sử dụng tên cột tiếng Anh: [Permissions]
                    allowedPermissions = serializer.Deserialize<List<string>>(keyInfo.Permissions);
                }
                catch
                {
                    // Xử lý lỗi nếu chuỗi JSON trong DB bị định dạng sai
                    filterContext.Result = new HttpStatusCodeResult(500, "Server Error: API Key permissions format is invalid.");
                    return;
                }

                // Kiểm tra xem RequiredPermission có tồn tại trong danh sách được phép không
                if (!allowedPermissions.Contains(RequiredPermission))
                {
                    filterContext.Result = new HttpStatusCodeResult(403, "Access Denied: Insufficient permissions");
                    return;
                }
            }

            // Nếu mọi thứ hợp lệ, tiếp tục thực thi action
            base.OnActionExecuting(filterContext);
        }
    }
}