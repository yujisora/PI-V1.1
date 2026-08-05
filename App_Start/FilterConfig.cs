using System.Web;
using System.Web.Mvc;

namespace PIV11
{
    /// <summary>
    /// There are no global filters registered - this app uses Web.config's <customErrors> + ErrorController
    /// for error handling instead of HandleErrorAttribute.
    /// </summary>
    /// <param name="filters"></param>
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
        }
    }
}