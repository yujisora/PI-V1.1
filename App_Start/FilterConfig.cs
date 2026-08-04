using System.Web;
using System.Web.Mvc;

namespace PIV11
{
    public class FilterConfig
    {
        // HandleErrorAttribute removed - error handling is done via
        // Web.config's <customErrors> + ErrorController instead.
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
        }
    }
}