using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace sportshopwebsite.Models
{
    [MetadataType(typeof(UserMetadata))]
    public partial class User
    {
        // This class will be extended with validation attributes
    }

    public class UserMetadata
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(150, ErrorMessage = "Họ tên không được vượt quá 150 ký tự")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(150, ErrorMessage = "Email không được vượt quá 150 ký tự")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string Password { get; set; }

        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        public string PhoneNumber { get; set; }

        [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự")]
        public string Address { get; set; }
    }
}
