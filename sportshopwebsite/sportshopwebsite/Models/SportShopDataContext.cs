using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Linq;
using System.Linq;
using System.Web;


namespace sportshopwebsite.Models
{
    public partial class SportShopDataContext : DataContext
    {
        public SportShopDataContext()
            : base(ConfigurationManager.ConnectionStrings["SportShopConnectionString"].ConnectionString)
        {
        }
    }
}