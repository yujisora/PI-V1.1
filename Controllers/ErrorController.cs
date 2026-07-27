using System.Web.Mvc;

namespace PIV11.Controllers
{
    /* =====================================================================
       ErrorController
       Friendly error pages instead of the raw ASP.NET "yellow screen of
       death". Reached two ways:
         - Web.config's <customErrors> redirects here for a 404 (unmatched
           URL) or any unhandled exception (500).
         - Global.asax's Application_Error also forwards here as a final
           safety net for anything customErrors doesn't catch (e.g. an
           error that happens before routing even runs).

       Deliberately has NO dependency on the database, Session, or
       anything else that could itself throw - if something is already
       going wrong, this page needs to be as close to bulletproof as
       possible.
       ===================================================================== */
    public class ErrorController : Controller
    {
        // GET: /Error/NotFound - shown for a mistyped/unmatched URL (404).
        public ActionResult NotFound()
        {
            Response.StatusCode = 404;
            Response.TrySkipIisCustomErrors = true;
            return View();
        }

        // GET: /Error/General - shown for any other unhandled error (500).
        public ActionResult General()
        {
            Response.StatusCode = 500;
            Response.TrySkipIisCustomErrors = true;
            return View();
        }
    }
}