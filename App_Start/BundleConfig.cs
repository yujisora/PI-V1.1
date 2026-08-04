using System.Web;
using System.Web.Optimization;

namespace PIV11
{
    public class BundleConfig
    {
        // No bundles registered - this app links Content/site.css and
        // Scripts/site.js directly in _Layout.cshtml rather than using
        // ASP.NET bundling/minification.
        public static void RegisterBundles(BundleCollection bundles)
        {
        }
    }
}