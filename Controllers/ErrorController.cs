using System.Web.Mvc;

namespace PIV11.Controllers
{
    /// <summary>
    /// Error page controller. Handles 404 (Not Found) and 500 (General) errors.
    /// Shows user a friendly error page instead of the default IIS error page.
    /// Does not reference any database or other models, so it can be used even if the database is down.
    /// </summary>

    public class ErrorController : Controller
    {
        // GET: /Error/NotFound - shown for a mistyped/unmatched URL (404).
            //GET: /Error/NotFound - mostrado para una URL mal escrita/no coincidente (404).
        public ActionResult NotFound()
        {
            Response.StatusCode = 404;
            Response.TrySkipIisCustomErrors = true;
            return View();
        }

        // GET: /Error/General - shown for any other unhandled error (500).
            // GET: /Error/General - mostrado para cualquier otro error no controlado (500).
        public ActionResult General()
        {
            Response.StatusCode = 500;
            Response.TrySkipIisCustomErrors = true;
            return View();
        }
    }
}