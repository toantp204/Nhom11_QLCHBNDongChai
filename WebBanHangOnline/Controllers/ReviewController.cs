using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanHangOnline.Models;
using WebBanHangOnline.Models.EF;

namespace WebBanHangOnline.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private ApplicationDbContext _db = new ApplicationDbContext();

        // GET: Review
        public ActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult _Review(int productId)
        {
            ViewBag.ProductId = productId;
            var item = new ReviewProduct();

            // Nếu người dùng đã đăng nhập, lấy thông tin người dùng
            if (User.Identity.IsAuthenticated)
            {
                var userStore = new UserStore<ApplicationUser>(new ApplicationDbContext());
                var userManager = new UserManager<ApplicationUser>(userStore);
                var user = userManager.FindByName(User.Identity.Name);

                if (user != null)
                {
                    // Gán thông tin người dùng vào đối tượng ReviewProduct
                    item.Email = user.Email;
                    item.FullName = user.FullName;
                    item.UserName = user.UserName;
                }

                return PartialView(item);
            }

            // Trả về PartialView mặc định nếu người dùng không đăng nhập
            return PartialView();
        }

        public ActionResult LichSuDonHang()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userStore = new UserStore<ApplicationUser>(new ApplicationDbContext());
                var userManager = new UserManager<ApplicationUser>(userStore);
                var user = userManager.FindByName(User.Identity.Name);

                // Lấy danh sách đơn hàng theo người dùng hiện tại
                var orders = _db.Orders.Where(x => x.CustomerId == user.Id).ToList();

                // Lưu lịch sử đơn hàng với chi tiết sản phẩm
                var orderHistory = orders.Select(order => new OrderHistory
                {
                    Id = order.Id,
                    Code = order.Code,
                    ProductNames = order.OrderDetails.Select(detail => detail.Product.Title).ToList()
                }).ToList();

                return PartialView(orderHistory);
            }

            return PartialView();
        }

        [AllowAnonymous]
        public ActionResult _Load_Review(int productId)
        {
            // Tải các review cho sản phẩm và sắp xếp theo ID giảm dần
            var item = _db.Reviews.Where(x => x.ProductId == productId).OrderByDescending(x => x.Id).ToList();
            ViewBag.Count = item.Count;
            return PartialView(item);
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult PostReview(ReviewProduct req)
        {
            if (ModelState.IsValid)
            {
                // Thiết lập thời gian tạo là thời gian hiện tại
                req.CreatedDate = DateTime.Now;

                // Thêm review mới vào cơ sở dữ liệu
                _db.Reviews.Add(req);
                _db.SaveChanges();
                return Json(new { Success = true });
            }

            // Trả về lỗi nếu model không hợp lệ
            return Json(new { Success = false });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Giải phóng tài nguyên khi controller bị dispose
                if (_db != null)
                {
                    _db.Dispose();
                    _db = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}
