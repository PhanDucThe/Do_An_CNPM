using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using quan_li_thuoc.Helpers;
using quan_li_thuoc.Models;

using System.Web.Security;

namespace quan_li_thuoc.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        [Display(Name = "Họ tên")]
        public string full_name { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string phone_number { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [Display(Name = "Email")]
        public string email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 50 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string password { get; set; }


    }


    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string password { get; set; }
    }


    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string email { get; set; }
    }



    public class ResetPasswordViewModel
    {
        //[Required(ErrorMessage = "Token xác thực không hợp lệ.")]
        public string token { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 50 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string newPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        public string confirmPassword { get; set; }
    }
}
namespace quan_li_thuoc.Controllers.User
{
   
    public class AccountController : Controller
    {
        db_CongNghePhanMemEntities db= new db_CongNghePhanMemEntities();
        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }
        //string full_name, string phone_number, string email, string password
        [HttpPost]

        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Form không hợp lệ → quay lại view và hiển thị lỗi
                return View(model);
            }
            if (db.users.Any(u => u.email == model.email))
            {
                ViewBag.Error = "Đã tồn tại tài khoản với email này !";
                return View();
            }

            var hashPass = HashHelper.HashPassword(model.password);

            var newUser = new user
            {
                full_name = model.full_name,
                phone_number = model.phone_number,
                email = model.email,
                password = hashPass,
                is_active = true,
                created_at = DateTime.Now,
                created_by = "system"
            };

            db.users.Add(newUser);
            db.SaveChanges();

            // Gán mặc định role "CUSTOMER"
            var customerRole = db.roles.FirstOrDefault(r => r.code == "CUSTOMER");
            if (customerRole != null)
            {
                db.user_roles.Add(new user_roles
                {
                    user_id = newUser.id,
                    role_id = customerRole.id,
                    created_at = DateTime.Now
                });
                db.SaveChanges();
            }

            TempData["Success"] = "Đăng ký thành công! Mời đăng nhập.";
            return RedirectToAction("Login");
        }


        // === Đăng nhập ===
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        //string email, string password
        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Form không hợp lệ → quay lại view và hiển thị lỗi
                return View(model);
            }
            string hashPass = HashHelper.HashPassword(model.password);


            var user = db.users.FirstOrDefault(u => u.email == model.email && u.password == hashPass && u.is_active == true);

            if (user != null)
            {
                FormsAuthentication.SetAuthCookie(user.email, false);
                Session["UserId"] = user.id;
                Session["UserName"] = user.full_name;

                // Lấy vai trò
                var role = (from ur in db.user_roles
                            join r in db.roles on ur.role_id equals r.id
                            where ur.user_id == user.id
                            select r.code).FirstOrDefault();

                Session["UserRole"] = role ?? "CUSTOMER";

                // Chuyển hướng
                if (role == "ADMIN")
                    return RedirectToAction("Index", "Default");
                else
                    return RedirectToAction("Index", "Account");
            }

            ViewBag.Error = "Email hoặc mật khẩu không đúng!";


            return View();
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }


        public ActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Form không hợp lệ → quay lại view và hiển thị lỗi
                return View(model);
            }
            var user = db.users.FirstOrDefault(u => u.email == model.email && u.is_active == true);
            if (user == null)
            {
                ViewBag.Message = "Không tìm thấy tài khoản với email này!";
                return View();
            }

            // Tạo mã token ngẫu nhiên
            string token = Guid.NewGuid().ToString();

            // Lưu token vào TempData tạm thời (hoặc bảng khác nếu bạn muốn an toàn hơn)
            TempData["ResetToken_" + token] = user.id;

            // Tạo link reset
            var resetLink = Url.Action("ResetPassword", "Account", new { token = token }, Request.Url.Scheme);


            MailHelper.SendMail(
                toEmail: user.email,
                subject: "Đặt lại mật khẩu - Web bán thuốc",
                body: $"Xin chào {user.full_name},<br/>Nhấn vào liên kết sau để đặt lại mật khẩu của bạn:<br/><a href='{resetLink}'>Đặt lại mật khẩu</a>"
            );

            ViewBag.Message = "Liên kết đặt lại mật khẩu đã được gửi đến email của bạn!";
            return View();
        }


        //public ActionResult ResetPassword()
        //{
        //    return View();
        //}

        [HttpGet]
        public ActionResult ResetPassword(string token)
        {
            if (token == null || TempData["ResetToken_" + token] == null)
            {
                ViewBag.Message = "Liên kết không hợp lệ hoặc đã hết hạn!";
                return View();
            }

            TempData.Keep("ResetToken_" + token); // giữ lại token cho lần POST
            return View();
        }

        [HttpPost]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (model.newPassword != model.confirmPassword)
            {
                ViewBag.Message = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            var userId = TempData["ResetToken_" + model.token];
            if (userId == null)
            {
                ViewBag.Message = "Liên kết không hợp lệ hoặc đã hết hạn!";
                return View();
            }

            int id = (int)userId;
            var user = db.users.Find(id);
            if (user == null)
            {
                ViewBag.Message = "Không tìm thấy người dùng!";
                return View();
            }

            user.password = HashHelper.HashPassword(model.newPassword);
            user.updated_at = DateTime.Now;
            user.updated_by = "ResetPassword-" + user.email; // Ghi rõ lý do hoặc người thực hiện

            db.SaveChanges();




            ViewBag.Message = "Mật khẩu đã được đặt lại thành công!";
            return View();
        }
        

    }
}