using System;
using System.Web;
using System.Web.Mvc;
using System.Security.Claims;

public class JwtAuthorizeAttribute : AuthorizeAttribute
{
    protected override bool AuthorizeCore(HttpContextBase httpContext)
    {
        // Lấy token từ header Authorization
        var token = httpContext.Request.Headers["Authorization"];

        if (!string.IsNullOrEmpty(token))
        {
            token = token.Replace("Bearer ", ""); // Loại bỏ từ "Bearer"
            var principal = JwtTokenHelper.GetPrincipal(token);
            if (principal != null)
            {
                HttpContext.Current.User = principal;
                return true; // Cho phép truy cập
            }
        }

        return false; // Không có token hoặc token không hợp lệ
    }

    protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
    {
        filterContext.Result = new JsonResult
        {
            Data = new { message = "Unauthorized" },
            JsonRequestBehavior = JsonRequestBehavior.AllowGet
        };
    }
}
