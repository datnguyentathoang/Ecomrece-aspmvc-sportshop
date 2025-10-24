using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace sportshopwebsite.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return Content("✅ Home Controller is working! Time: " + DateTime.Now.ToString());
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}