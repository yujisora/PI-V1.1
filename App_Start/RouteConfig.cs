using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace PIV11
{
    public class RouteConfig
    {
        /// <summary>
        /// Registers the default route for the application. The default route pattern is "{controller}/{action}/{id}", 
        /// with "Home" as the default controller, "Index" as the default action, and an optional "id" parameter.
        /// </summary>
        /// <param name="routes"></param>
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
