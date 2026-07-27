using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Data.Entity;
using PIV11.Models;

namespace PIV11
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Database.SetInitializer<NorteMartContext>(null);

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
        protected void Application_Error()
        {
            var exception = Server.GetLastError();
            if (exception == null) return;

            var httpException = exception as HttpException;
            if (httpException != null && httpException.GetHttpCode() == 404) return;

            Server.ClearError();
            Response.Clear();
            Response.StatusCode = 500;

            var routeData = new RouteData();
            routeData.Values["controller"] = "Error";
            routeData.Values["action"] = "General";
            IController controller = new PIV11.Controllers.ErrorController();
            controller.Execute(new RequestContext(new HttpContextWrapper(Context), routeData));
        }
    }
}
