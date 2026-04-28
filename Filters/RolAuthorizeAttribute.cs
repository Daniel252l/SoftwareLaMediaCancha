using System.Web;
using System.Web.Mvc;

namespace LaMediaCancha.Filters
{
    public class RolAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly string[] _roles;

        public RolAuthorizeAttribute(params string[] roles)
        {
            _roles = roles;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext.Session["UserId"] == null)
                return false;

            string rolNombre = httpContext.Session["UserRol"]?.ToString();
            if (string.IsNullOrEmpty(rolNombre))
                return false;

            foreach (var rol in _roles)
                if (rolNombre.Equals(rol, System.StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.Session["UserId"] == null)
                filterContext.Result = new RedirectResult("/Account/Login");
            else
                filterContext.Result = new RedirectResult("/Home/Denegado");
        }
    }
}