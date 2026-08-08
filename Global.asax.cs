using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceProcess;
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
        public class Global : System.Web.HttpApplication
        {
            protected void Application_Start(object sender, EventArgs e)
            {
                try
                {
                    using (var sc = new ServiceController("MSSQL$SQLEXPRESS"))
                    {
                        if (sc.Status != ServiceControllerStatus.Running)
                        {
                            sc.Start();
                            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                        }
                    }
                }
                catch (Exception ex)
                {
                
                }
            }
        }
        protected void Application_Error()
        {
            // Temporarily disabled to see errors
            /*
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
            */
        }
    }
}
