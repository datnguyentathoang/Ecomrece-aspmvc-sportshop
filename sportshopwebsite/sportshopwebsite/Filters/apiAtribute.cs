using sportshopwebsite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace sportshopwebsite.Filters
{
    public class ApiAttribute  : ActionFilterAttribute
    {
        public string RequiredPermission { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var db = new SportShopDataContext();
            var request = filterContext.HttpContext.Request;

            var apiKey = request.Headers["x-api-key"] ?? request.QueryString["api_key"];
            if (string.IsNullOrEmpty(apiKey))
            {
                filterContext.Result = new HttpStatusCodeResult(403, "Access Denied: Missing API Key");
                return;
            }


            var keyInfo = db.ApiKeys.FirstOrDefault(k => k.Key == apiKey && k.Status == true);
            if (keyInfo == null)
            {
                filterContext.Result = new HttpStatusCodeResult(403, "Access Denied: Invalid or inactive API Key");
                return;
            }

            if (!string.IsNullOrEmpty(RequiredPermission))
            {
                var serializer = new JavaScriptSerializer();
                List<string> allowedPermissions;

                try
                {
                    allowedPermissions = serializer.Deserialize<List<string>>(keyInfo.Permissions);
                }
                catch
                {
                    filterContext.Result = new HttpStatusCodeResult(500, "Server Error: API Key permissions format is invalid.");
                    return;
                }

                if (!allowedPermissions.Contains(RequiredPermission))
                {
                    filterContext.Result = new HttpStatusCodeResult(403, "Access Denied: Insufficient permissions");
                    return;
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}