using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using PIV11.Infrastructure;
using PIV11.Models;
using PIV11.Models.ViewModels;

namespace PIV11.Controllers
{
    /// <summary>
    /// The consumer search flow (<see cref="Index"/>, <see cref="Search"/>),
    /// Recent Searches (<see cref="Recent"/>), Add Product
    /// (<see cref="AddProduct(string)"/> / <see cref="AddProduct(AddProductViewModel)"/>),
    /// and the entire admin dashboard including its own inline search box
    /// (<see cref="AdminDashboard"/>, <see cref="ShowSearchFeedback"/>,
    /// <see cref="Activity"/>).
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// GET: <c>/</c> and <c>/Home/Index</c>. Shows the search hero page
        /// for guest/employee/shopper. Admin sees the Dashboard instead (see
        /// <see cref="AdminDashboard"/>) - admin is mostly reviewing/managing
        /// data, not searching for products the way a shopper would.
        /// </summary>
        public ActionResult Index()
        {
            ViewBag.ActiveScreen = "Home";
            ViewBag.Title = "Home";

            if (SessionHelper.IsAdmin)
            {
                return AdminDashboard();
            }

            return View(new HomeSearchViewModel());
        }

        /// <summary>
        /// Admin-only dashboard: stat counts, a list of products with no
        /// edit history yet (the closest available proxy for "recently
        /// added," since <c>Products</c> has no creation-date column), and
        /// the 10 most recent edits across every product. Built entirely
        /// from existing data - no new schema.
        /// </summary>
        private ActionResult AdminDashboard()
        {
            using (var db = new NorteMartContext())
            {
                var vm = BuildAdminDashboardViewModel(db);
                ViewBag.Title = "Admin Dashboard";
                return View("Dashboard", vm);
            }
        }

        /// <summary>
        /// Builds the full <see cref="AdminDashboardViewModel"/> - stat
        /// counts, unedited products, recent activity. Kept separate from
        /// <see cref="AdminDashboard"/> so <see cref="ShowSearchFeedback"/>
        /// can reuse it too: a search from the dashboard's own lookup box
        /// needs to redisplay the entire dashboard with the search result
        /// layered on top, not just the search feedback alone.
        /// </summary>
        private AdminDashboardViewModel BuildAdminDashboardViewModel(NorteMartContext db)
        {
            var vm = new AdminDashboardViewModel
            {
                TotalProducts = db.Products.Count(),
                PendingEditsCount = db.EditHistory.Count(e => e.Status == "pending"),
                ApprovedEditsCount = db.EditHistory.Count(e => e.Status == "approved"),
                DeniedEditsCount = db.EditHistory.Count(e => e.Status == "denied")
            };

            var editedUpcs = db.EditHistory.Select(e => e.UPC).Distinct();
            vm.UneditedProducts = db.Products
                .Where(p => !editedUpcs.Contains(p.UPC))
                .OrderBy(p => p.ProductName)
                .Take(10)
                .Select(p => new AdminProductSummary { UPC = p.UPC, Name = p.ProductName, Brand = p.Brand })
                .ToList();

            var recentEdits = db.EditHistory
                .OrderByDescending(e => e.DateEdited)
                .Take(10)
                .ToList();

            var upcs = recentEdits.Select(e => e.UPC).Distinct().ToList();
            var namesByUpc = db.Products
                .Where(p => upcs.Contains(p.UPC))
                .ToDictionary(p => p.UPC, p => p.ProductName);

            vm.RecentActivity = recentEdits.Select(e => new AdminActivityItem
            {
                UPC = e.UPC,
                ProductName = namesByUpc.ContainsKey(e.UPC) ? namesByUpc[e.UPC] : "(deleted product)",
                FieldChanged = e.FieldChanged,
                NewValue = e.NewValue,
                EditedByUser = e.EditedByUser,
                DateEdited = e.DateEdited,
                Status = e.Status
            }).ToList();

            return vm;
        }

        /// <summary>
        /// GET: <c>/Home/Search?query=...</c>. Detects whether
        /// <paramref name="query"/> looks like a barcode (all digits) or a
        /// name/brand search, then either redirects straight to Product Info
        /// (exact match), shows a pick-list (multiple name matches), or
        /// shows an error/not-found message via <see cref="ShowSearchFeedback"/>.
        /// Used identically by the consumer search box and the admin
        /// dashboard's own inline search box.
        /// </summary>
        public ActionResult Search(string query)
        {
            ViewBag.ActiveScreen = "Home";
            ViewBag.Title = "Home";
            var model = new HomeSearchViewModel { Query = query };

            string trimmed = (query ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                model.ErrorMessage = "Please enter an EAN/UPC barcode number or product name.";
                return ShowSearchFeedback(model);
            }

            using (var db = new NorteMartContext())
            {

                // A query made up entirely of digits is treated as a barcode attempt and validated strictly. Anything else is
                // treated as a name/brand search instead.
                    // Una consulta compuesta enteramente de dígitos se trata como un intento de código de barras y se
                    // valida estrictamente. Cualquier otra cosa se trata como una búsqueda por nombre/marca.
                if (Regex.IsMatch(trimmed, @"^\d+$"))
                {
                    decimal? normalized = ValidateBarcode(trimmed, out string barcodeError);
                    if (normalized == null)
                    {
                        model.ErrorMessage = barcodeError;
                        return ShowSearchFeedback(model);
                    }

                    var product = db.Products
                        .Include(p => p.IngredientsAllergens)
                        .FirstOrDefault(p => p.UPC == normalized.Value);

                    if (product == null)
                    {
                        model.NotFoundMessage = "No product found for this barcode.";
                        model.NotFoundUpc = normalized.Value.ToString();
                        return ShowSearchFeedback(model);
                    }

                    SessionHelper.RecordProductView(product, HasAnyAllergenInfo(product));
                    return RedirectToAction("Info", "Product", new { upc = product.UPC });
                }
                else
                {
                    var matches = db.Products
                        .Include(p => p.IngredientsAllergens)
                        .Where(p => p.ProductName.Contains(trimmed) || (p.Brand != null && p.Brand.Contains(trimmed)))
                        .OrderBy(p => p.ProductName)
                        .Take(20)
                        .ToList();

                    if (matches.Count == 0)
                    {
                        model.NotFoundMessage = "No product found matching that name.";
                        return ShowSearchFeedback(model);
                    }

                    if (matches.Count == 1)
                    {
                        SessionHelper.RecordProductView(matches[0], HasAnyAllergenInfo(matches[0]));
                        return RedirectToAction("Info", "Product", new { upc = matches[0].UPC });
                    }

                    model.Results = matches.Select(ToSearchResult).ToList();
                    return ShowSearchFeedback(model);
                }
            }
        }

        /// <summary>
        /// Renders whatever <see cref="Search"/> didn't resolve directly to a
        /// single product. Admin sees it embedded in the full Dashboard
        /// shell (rebuilt via <see cref="BuildAdminDashboardViewModel"/>,
        /// with the search fields layered on top); everyone else sees the
        /// normal consumer <c>Index</c> page.
        /// </summary>
        private ActionResult ShowSearchFeedback(HomeSearchViewModel model)
        {
            if (!SessionHelper.IsAdmin)
            {
                return View("Index", model);
            }

            using (var db = new NorteMartContext())
            {
                var vm = BuildAdminDashboardViewModel(db);
                vm.SearchQuery = model.Query;
                vm.SearchError = model.ErrorMessage;
                vm.SearchNotFoundMessage = model.NotFoundMessage;
                vm.SearchResults = model.Results;
                ViewBag.Title = "Admin Dashboard";
                return View("Dashboard", vm);
            }
        }

        // GET: /Home/Recent?all=true
        /// <summary>
        /// GET: <c>/Home/Recent?all=true</c>. Lists the session's Recent
        /// Searches, or every registered product when <paramref name="all"/>
        /// is <c>true</c> or there's nothing recent yet to show.
        /// </summary>
        public ActionResult Recent(bool all = false)
        {
            ViewBag.ActiveScreen = "Recent";
            ViewBag.Title = "Recent Searches";

            var recent = SessionHelper.GetRecentSearches();
            bool hasRecent = recent.Count > 0;
            bool showAll = all || !hasRecent;

            ViewBag.HasRecent = hasRecent;
            ViewBag.ShowingAll = showAll;

            if (!showAll)
            {
                return View(recent);
            }

            /// <summary>
            /// All products, sorted by name, mapped to the lightweight RecentSearchItem shape. 
            /// This is the fallback when there are no recent searches yet, or when the user explicitly requests "show all."
            using (var db = new NorteMartContext())
            {
                var allProducts = db.Products
                    .Include(p => p.IngredientsAllergens)
                    .OrderBy(p => p.ProductName)
                    .ToList()
                    .Select(ToRecentSearchItem)
                    .ToList();
                return View(allProducts);
            }
        }

        /// <summary>
        /// GET: <c>/Home/AddProduct?upc=...</c>. Shows the Add Product form.
        /// If reached via the "Add it here" link after a failed barcode
        /// search, <paramref name="upc"/> pre-fills the barcode field.
        /// Restricted to <see cref="SessionHelper.CanAddProducts"/> roles.
        /// </summary>
        public ActionResult AddProduct(string upc)
        {
            if (!SessionHelper.IsLoggedIn)
            {
                return RedirectToAction("Login", "Account");
            }
            if (!SessionHelper.CanAddProducts)
            {
                return RedirectToAction("Recent", "Home");
            }

            ViewBag.ActiveScreen = "Recent";
            ViewBag.Title = "Add Product";
            return View(new AddProductViewModel { UPC = upc });
        }

        // POST: /Home/AddProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        /// POST: <c>/Home/AddProduct</c>. Validates and inserts a brand-new
        /// <c>Products</c> + <c>Foodstuffs</c> row. Nutrition facts, allergens,
        /// and warning seals are deliberately left for a later Edit to fill
        /// in. Redirects straight to the new product's Info page on success.
        /// </summary>
        public ActionResult AddProduct(AddProductViewModel model)
        {
            if (!SessionHelper.IsLoggedIn)
            {
                return RedirectToAction("Login", "Account");
            }
            if (!SessionHelper.CanAddProducts)
            {
                return RedirectToAction("Recent", "Home");
            }

            ViewBag.ActiveScreen = "Recent";
            ViewBag.Title = "Add Product";

            decimal? normalized = ValidateBarcode((model.UPC ?? string.Empty).Trim(), out string barcodeError);
            if (normalized == null)
            {
                ModelState.AddModelError("", barcodeError);
            }
            if (string.IsNullOrWhiteSpace(model.ProductName))
            {
                ModelState.AddModelError("", "Product name is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            /// <summary>  
            /// The UPC is valid and the product name is provided. 
            /// Check if a product with the same UPC already exists in the database. 
            /// If it does, add a model error and return the view with the current model. 
            /// Otherwise, create a new Product and Foodstuff entry, save changes to the database,
            /// record the product view in the session, and redirect to the product's Info page.
            /// </summary>
            using (var db = new NorteMartContext())
            {
                bool alreadyExists = db.Products.Any(p => p.UPC == normalized.Value);
                if (alreadyExists)
                {
                    ModelState.AddModelError("", "A product with this barcode already exists.");
                    return View(model);
                }

                var product = new Product
                {
                    UPC = normalized.Value,
                    ProductName = model.ProductName.Trim(),
                    Brand = string.IsNullOrWhiteSpace(model.Brand) ? null : model.Brand.Trim()
                };
                db.Products.Add(product);

                db.Foodstuffs.Add(new Foodstuff
                {
                    UPC = normalized.Value,
                    NetVolume = string.IsNullOrWhiteSpace(model.Weight) ? "0" : model.Weight.Trim(),
                    UnitMeasurement = string.IsNullOrWhiteSpace(model.Unit) ? "g" : model.Unit.Trim()
                });

                db.SaveChanges();

                SessionHelper.RecordProductView(product, false);
                return RedirectToAction("Info", "Product", new { upc = product.UPC });
            }
        }

        /// <summary>
        /// GET: <c>/Home/Activity?status=pending|approved|denied</c>. Admin
        /// only. Every matching <see cref="EditHistoryRecord"/> across every
        /// product, unlike the dashboard's own "Recent Activity" section
        /// (which is capped at 10 and mixes all statuses together).
        /// </summary>
        /// <param name="status">One of <c>"pending"</c>/<c>"approved"</c>/<c>"denied"</c>; anything else shows all statuses.</param>
        public ActionResult Activity(string status)
        {
            if (!SessionHelper.IsAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            string normalizedStatus = (status ?? "").Trim().ToLower();
            bool isValidStatus = normalizedStatus == "pending" || normalizedStatus == "approved" || normalizedStatus == "denied";

            using (var db = new NorteMartContext())
            {
                var query = db.EditHistory.AsQueryable();
                if (isValidStatus)
                {
                    query = query.Where(e => e.Status == normalizedStatus);
                }

                var edits = query.OrderByDescending(e => e.DateEdited).ToList();

                var upcs = edits.Select(e => e.UPC).Distinct().ToList();
                var namesByUpc = db.Products
                    .Where(p => upcs.Contains(p.UPC))
                    .ToDictionary(p => p.UPC, p => p.ProductName);

                var items = edits.Select(e => new AdminActivityItem
                {
                    UPC = e.UPC,
                    ProductName = namesByUpc.ContainsKey(e.UPC) ? namesByUpc[e.UPC] : "(deleted product)",
                    FieldChanged = e.FieldChanged,
                    NewValue = e.NewValue,
                    EditedByUser = e.EditedByUser,
                    DateEdited = e.DateEdited,
                    Status = e.Status
                }).ToList();

                ViewBag.ActiveScreen = "Home";
                ViewBag.StatusFilter = isValidStatus ? normalizedStatus : "all";
                ViewBag.Title = "Edit Activity";
                return View(items);
            }
        }

        /// <summary>
        /// Helpers are private methods that support the main action methods, such as validating barcodes, checking for allergens, 
        /// and mapping products to view models.
        /// </summary>
        /* Helpers - Ayudantes */

        /// <summary>
        /// Validates a barcode string: must be 12 or 13 digits, normalized to
        /// 13 (left-padding a 12-digit UPC-A with a zero), with a real EAN-13
        /// check-digit verification - not just a length check.
        /// </summary>
        /// <param name="trimmed">The already-trimmed candidate barcode string.</param>
        /// <param name="error">Set to a specific failure message when validation fails.</param>
        /// <returns>The normalized 13-digit barcode as a <see cref="decimal"/>, or <c>null</c> on any failure.</returns>
        private decimal? ValidateBarcode(string trimmed, out string error)
        {
            error = null;

            if (trimmed.Length < 12 || trimmed.Length > 13)
            {
                error = string.Format("Barcode must be 12 or 13 digits (got {0}).", trimmed.Length);
                return null;
            }

            string normalized = trimmed.Length == 12 ? "0" + trimmed : trimmed;
            int[] d = normalized.Select(c => c - '0').ToArray();

            int evenSum = d[1] + d[3] + d[5] + d[7] + d[9] + d[11];
            int oddSum = d[0] + d[2] + d[4] + d[6] + d[8] + d[10] + d[12];

            if ((evenSum * 3 + oddSum) % 10 != 0)
            {
                error = "Invalid barcode: check digit does not match (EAN-13 validation failed).";
                return null;
            }

            return decimal.Parse(normalized);
        }

        /// <summary>True if the product has any allergen flagged (Contains OR May-Contain) - drives the amber warning badge without needing the full lists.</summary>
        private bool HasAnyAllergenInfo(Product product)
        {
            AllergenHelper.SplitContainsAndMayContain(product.IngredientsAllergens, out List<string> contains, out List<string> mayContain);
            return contains.Count > 0 || mayContain.Count > 0;
        }

        /// <summary>Maps a <see cref="Product"/> to the lightweight shape used by the search results/pick-list.</summary>
        private ProductSearchResultViewModel ToSearchResult(Product p)
        {
            return new ProductSearchResultViewModel
            {
                UPC = p.UPC,
                Name = p.ProductName,
                Brand = p.Brand,
                HasAllergens = HasAnyAllergenInfo(p)
            };
        }

        /// <summary>
        /// Maps a <see cref="Product"/> to the lightweight
        /// <see cref="RecentSearchItem"/> shape the Recent Searches view
        /// renders - reused when falling back to "show every registered
        /// product" (see <see cref="Recent"/>).
        /// </summary>
        private RecentSearchItem ToRecentSearchItem(Product p)
        {
            return new RecentSearchItem
            {
                UPC = p.UPC,
                Name = p.ProductName,
                Brand = p.Brand,
                HasAllergens = HasAnyAllergenInfo(p)
            };
        }
    }
}