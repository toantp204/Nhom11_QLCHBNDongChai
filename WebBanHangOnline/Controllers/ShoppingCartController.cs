using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using WebBanHangOnline.Models;
using WebBanHangOnline.Models.EF;
using WebBanHangOnline.Models.Payments;

namespace WebBanHangOnline.Controllers
{
    [Authorize]
    public class ShoppingCartController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public ShoppingCartController()
        {
        }

        public ShoppingCartController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
        // GET: ShoppingCart
        [AllowAnonymous]
        public ActionResult Index()
        {

            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null && cart.Items.Any())
            {
                ViewBag.CheckCart = cart;
            }
            return View();
        }
        [AllowAnonymous]
        public ActionResult VnpayReturn()
        {
            if (Request.QueryString.Count > 0)
            {
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];
                var vnpayData = Request.QueryString;
                VnPayLibrary vnpay = new VnPayLibrary();

                // Collect response data
                foreach (string key in vnpayData.AllKeys)
                {
                    if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(key, vnpayData[key]);
                    }
                }

                // Extract response values
                string orderCode = vnpay.GetResponseData("vnp_TxnRef");
                long vnpayTranId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                string vnp_SecureHash = Request.QueryString["vnp_SecureHash"];
                long vnp_Amount = Convert.ToInt64(vnpay.GetResponseData("vnp_Amount")) / 100;

                // Validate signature
                bool isSignatureValid = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

                if (isSignatureValid)
                {
                    if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                    {
                        // Successful payment processing
                        var order = db.Orders.FirstOrDefault(x => x.Code == orderCode);
                        if (order != null)
                        {
                            order.Status = 2; // Update status to paid
                            db.Entry(order).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                        ViewBag.InnerText = "Giao dịch được thực hiện thành công. Cảm ơn quý khách đã sử dụng dịch vụ.";
                    }
                    else
                    {
                        // Payment failure handling
                        ViewBag.InnerText = "Có lỗi xảy ra trong quá trình xử lý. Mã lỗi: " + vnp_ResponseCode;
                    }

                    ViewBag.ThanhToanThanhCong = "Số tiền thanh toán (VND): " + vnp_Amount;
                }
            }

            return View();
        }

        [AllowAnonymous]
        public ActionResult CheckOut()
        {
            var cart = GetShoppingCart();

            if (cart != null && cart.Items.Any())
            {
                ViewBag.CheckCart = cart;
            }

            return View();
        }

        private ShoppingCart GetShoppingCart()
        {
            return (ShoppingCart)Session["Cart"];
        }

        [AllowAnonymous]
        public ActionResult CheckOutSuccess()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult Partial_Item_ThanhToan()
        {
            var cartItems = GetCartItems();
            return PartialView(cartItems);
        }

        private IEnumerable<ShoppingCartItem> GetCartItems()
        {
            var cart = (ShoppingCart)Session["Cart"];
            return cart?.Items ?? Enumerable.Empty<ShoppingCartItem>();
        }
        [AllowAnonymous]
        public ActionResult Partial_Item_Cart()
        {
            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null && cart.Items.Any())
            {
                return PartialView(cart.Items);
            }
            return PartialView();
        }

        [AllowAnonymous]
        public ActionResult ShowCount()
        {
            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null)
            {
                return Json(new { Count = cart.Items.Count }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { Count = 0 }, JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        public ActionResult Partial_CheckOut()
        {
            var user = UserManager.FindByNameAsync(User.Identity.Name).Result;
            if (user != null)
            {
                ViewBag.User = user;
            }
            return PartialView();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult CheckOut(OrderViewModel req)
        {
            var response = new { Success = false, Code = -1, Url = "" };

            if (ModelState.IsValid)
            {
                ShoppingCart cart = (ShoppingCart)Session["Cart"];
                if (cart != null && cart.Items.Any())
                {
                    var order = CreateOrder(req, cart);
                    db.Orders.Add(order);
                    db.SaveChanges();

                    SendOrderConfirmationEmails(order, cart, req.Email);

                    cart.ClearCart();
                    response = new { Success = true, Code = req.TypePayment, Url = GetPaymentUrl(req, order.Code) };
                }
            }

            return Json(response);
        }

        private Order CreateOrder(OrderViewModel req, ShoppingCart cart)
        {
            var order = new Order
            {
                CustomerName = req.CustomerName,
                Phone = req.Phone,
                Address = req.Address,
                Email = req.Email,
                Status = 1, // 1: Not paid
                TypePayment = req.TypePayment,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                CreatedBy = req.Phone,
                CustomerId = User.Identity.IsAuthenticated ? User.Identity.GetUserId() : null,
                Code = GenerateOrderCode()
            };

            foreach (var item in cart.Items)
            {
                order.OrderDetails.Add(new OrderDetail
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                });
            }
            order.TotalAmount = cart.Items.Sum(x => (x.Price * x.Quantity));

            return order;
        }

        private string GenerateOrderCode()
        {
            var random = new Random();
            return "DH" + random.Next(1000, 9999); // Generates a random order code
        }

        private void SendOrderConfirmationEmails(Order order, ShoppingCart cart, string customerEmail)
        {
            var productDetails = GenerateProductDetails(cart.Items);
            var totalAmount = cart.Items.Sum(x => x.TotalPrice);

            SendEmailToCustomer(order, productDetails, totalAmount, customerEmail);
            SendEmailToAdmin(order, productDetails, totalAmount);
        }

        private string GenerateProductDetails(IEnumerable<ShoppingCartItem> items)
        {
            var productDetails = new StringBuilder();
            foreach (var item in items)
            {
                productDetails.Append("<tr>");
                productDetails.AppendFormat("<td>{0}</td>", item.ProductName);
                productDetails.AppendFormat("<td>{0}</td>", item.Quantity);
                productDetails.AppendFormat("<td>{0}</td>", WebBanHangOnline.Common.Common.FormatNumber(item.TotalPrice, 0));
                productDetails.Append("</tr>");
            }
            return productDetails.ToString();
        }

        private void SendEmailToCustomer(Order order, string productDetails, decimal totalAmount, string customerEmail)
        {
            string contentCustomer = System.IO.File.ReadAllText(Server.MapPath("~/Content/templates/send2.html"));
            contentCustomer = PopulateEmailContent(contentCustomer, order, productDetails, totalAmount);
            WebBanHangOnline.Common.Common.SendMail("ShopOnline", "Đơn hàng #" + order.Code, contentCustomer, customerEmail);
        }

        private void SendEmailToAdmin(Order order, string productDetails, decimal totalAmount)
        {
            string contentAdmin = System.IO.File.ReadAllText(Server.MapPath("~/Content/templates/send1.html"));
            contentAdmin = PopulateEmailContent(contentAdmin, order, productDetails, totalAmount);
            WebBanHangOnline.Common.Common.SendMail("ShopOnline", "Đơn hàng mới #" + order.Code, contentAdmin, ConfigurationManager.AppSettings["EmailAdmin"]);
        }

        private string PopulateEmailContent(string content, Order order, string productDetails, decimal totalAmount)
        {
            return content
                .Replace("{{MaDon}}", order.Code)
                .Replace("{{SanPham}}", productDetails)
                .Replace("{{NgayDat}}", DateTime.Now.ToString("dd/MM/yyyy"))
                .Replace("{{TenKhachHang}}", order.CustomerName)
                .Replace("{{Phone}}", order.Phone)
                .Replace("{{Email}}", order.Email)
                .Replace("{{DiaChiNhanHang}}", order.Address)
                .Replace("{{ThanhTien}}", WebBanHangOnline.Common.Common.FormatNumber(totalAmount, 0))
                .Replace("{{TongTien}}", WebBanHangOnline.Common.Common.FormatNumber(totalAmount, 0)); // Assuming total amount is same for both
        }

        private string GetPaymentUrl(OrderViewModel req, string orderCode)
        {
            return req.TypePayment == 2 ? UrlPayment(req.TypePaymentVN, orderCode) : string.Empty;
        }



        [AllowAnonymous]
        [HttpPost]
        public ActionResult AddToCart(int id, int quantity)
        {
            var response = new { Success = false, msg = "", code = -1, Count = 0 };

            if (quantity < 1)
            {
                response = new { Success = false, msg = "Số lượng sản phẩm phải lớn hơn 0", code = -1, Count = 0 };
                return Json(response);
            }

            var product = db.Products.FirstOrDefault(x => x.Id == id);
            if (product == null)
            {
                response = new { Success = false, msg = "Sản phẩm không tồn tại.", code = -1, Count = 0 };
                return Json(response);
            }

            ShoppingCart cart = Session["Cart"] as ShoppingCart ?? new ShoppingCart();

            var productImage = product.ProductImage.FirstOrDefault(x => x.IsDefault)?.Image;

            ShoppingCartItem item = new ShoppingCartItem
            {
                ProductId = product.Id,
                ProductName = product.Title,
                CategoryName = product.ProductCategory.Title,
                Alias = product.Alias,
                Quantity = quantity,
                ProductImg = productImage,
                Price = product.PriceSale > 0 ? (decimal)product.PriceSale : product.Price,
                TotalPrice = quantity * (product.PriceSale > 0 ? (decimal)product.PriceSale : product.Price)
            };

            cart.AddToCart(item, quantity);
            Session["Cart"] = cart;

            response = new { Success = true, msg = "Thêm sản phẩm vào giỏ hàng thành công!", code = 1, Count = cart.Items.Count };
            return Json(response);
        }



        [AllowAnonymous]
        [HttpPost]
        public ActionResult Update(int id, int quantity)
        {
            if (quantity < 1)
            {
                return Json(new { Success = false, msg = "Số lượng sản phẩm phải lớn hơn 0" });
            }

            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null)
            {
                cart.UpdateQuantity(id, quantity);
                return Json(new { Success = true });
            }
            return Json(new { Success = false });
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Delete(int id)
        {
            var code = new { Success = false, msg = "", code = -1, Count = 0 };

            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null)
            {
                var checkProduct = cart.Items.FirstOrDefault(x => x.ProductId == id);
                if (checkProduct != null)
                {
                    cart.Remove(id);
                    code = new { Success = true, msg = "", code = 1, Count = cart.Items.Count };
                }
            }
            return Json(code);
        }


        [AllowAnonymous]
        [HttpPost]
        public ActionResult DeleteAll()
        {
            ShoppingCart cart = (ShoppingCart)Session["Cart"];
            if (cart != null)
            {
                cart.ClearCart();
                return Json(new { Success = true });
            }
            return Json(new { Success = false });
        }



        #region Thanh toán vnpay
        public string UrlPayment(int TypePaymentVN, string orderCode)
        {
            var urlPayment = "";
            var order = db.Orders.FirstOrDefault(x => x.Code == orderCode);
            //Get Config Info
            string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"]; //URL nhan ket qua tra ve 
            string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"]; //URL thanh toan cua VNPAY 
            string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"]; //Ma định danh merchant kết nối (Terminal Id)
            string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"]; //Secret Key

            //Build URL for VNPAY
            VnPayLibrary vnpay = new VnPayLibrary();
            var Price = (long)order.TotalAmount * 100;
            vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", Price.ToString()); //Số tiền thanh toán. Số tiền không mang các ký tự phân tách thập phân, phần nghìn, ký tự tiền tệ. Để gửi số tiền thanh toán là 100,000 VND (một trăm nghìn VNĐ) thì merchant cần nhân thêm 100 lần (khử phần thập phân), sau đó gửi sang VNPAY là: 10000000
            if (TypePaymentVN == 1)
            {
                vnpay.AddRequestData("vnp_BankCode", "VNPAYQR");
            }
            else if (TypePaymentVN == 2)
            {
                vnpay.AddRequestData("vnp_BankCode", "VNBANK");
            }
            else if (TypePaymentVN == 3)
            {
                vnpay.AddRequestData("vnp_BankCode", "INTCARD");
            }

            vnpay.AddRequestData("vnp_CreateDate", order.CreatedDate.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress());
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toán đơn hàng :" + order.Code);
            vnpay.AddRequestData("vnp_OrderType", "other"); //default value: other

            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", order.Code); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày

            //Add Params of 2.1.0 Version
            //Billing

            urlPayment = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            //log.InfoFormat("VNPAY URL: {0}", paymentUrl);
            return urlPayment;
        }
        #endregion
    }
}