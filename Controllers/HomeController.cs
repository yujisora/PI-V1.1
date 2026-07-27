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
    /* =====================================================================
       HomeController
       The Home/search screen, Recent Searches, and Add Product. All three
       are grouped here (rather than split into separate controllers)
       because they're all part of the same "find or add a product"
       workflow the mockup groups under the Home/Recent screens.
       ===================================================================== */
    public class HomeController : Controller
    {
        // GET: / and /Home/Index
        // The empty search box + feature cards for guest/user/shopper.
        // Admin sees a dashboard instead (see AdminDashboard() below) -
        // an admin is mostly reviewing/managing data, not searching for
        // products the way a shopper would.
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

        // Admin-only dashboard: total products, edit-review counts, a
        // list of products with no edit history yet (the closest
        // available proxy for "recently added / not yet reviewed" -
        // Products has no creation-date column to sort by chronologically),
        // and the most recent Edit History activity across all products.
        // Everything here comes from existing Products/EditHistory data -
        // no new columns or tables.
        private ActionResult AdminDashboard()
        {
            using (var db = new NorteMartContext())
            {
                var vm = BuildAdminDashboardViewModel(db);
                ViewBag.Title = "Admin Dashboard";
                return View("Dashboard", vm);
            }
        }

        // Builds the dashboard's data, separated from AdminDashboard()
        // itself so ShowSearchFeedback() below can reuse it too - a
        // search attempted from the dashboard's compact lookup box needs
        // to redisplay the FULL dashboard (stats, unedited products,
        // recent activity) with the search result/error layered on top,
        // not just the search feedback on its own.
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

        // GET: /Home/Search?query=...
        // Figures out whether the typed text looks like a barcode or a
        // product name/brand, and either:
        //   - redirects straight to Product Info (exact barcode match, or
        //     a name search that matched exactly one product), or
        //   - shows a short pick-list (name search matched several), or
        //   - shows an error/not-found message back on the Home screen
        //     (or, for admin, back on the Dashboard - see
        //     ShowSearchFeedback() below).
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
                // A query made up entirely of digits is treated as a
                // barcode attempt and validated strictly. Anything else is
                // treated as a name/brand search instead.
                if (Regex.IsMatch(trimmed, @"^\d+$"))
                {
                    string barcodeError;
                    decimal? normalized = ValidateBarcode(trimmed, out barcodeError);
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

        // Renders whatever the search didn't resolve directly (an error,
        // a "not found" message, or a multi-match pick-list). Admin sees
        // it inside the Dashboard shell (their "Home" screen is the
        // dashboard, not the consumer search page); everyone else sees
        // the normal Index search page.
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
        // Normally lists the session's Recent Searches. Falls back to
        // showing every registered product automatically when there's
        // nothing recent yet (a fresh session otherwise has nothing to
        // show at all) - and that same "all products" list can also be
        // requested explicitly via ?all=true even when recent searches
        // does have entries, as a simple way to browse everything.
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

        // GET: /Home/AddProduct?upc=...
        // Shows the Add Product form. If reached via the "Add it here"
        // link after a failed barcode search, the barcode is prefilled.
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
        // Validates and inserts a brand-new product (Products +
        // Foodstuffs rows). Nutrition facts, allergens, and warning seals
        // are left for the Edit screen (a later phase) to fill in -
        // matching the mockup, where a freshly-added product starts with
        // default/empty nutrition data too.
        [HttpPost]
        [ValidateAntiForgeryToken]
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

            string barcodeError;
            decimal? normalized = ValidateBarcode((model.UPC ?? string.Empty).Trim(), out barcodeError);
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

        // GET: /Home/Activity?status=pending|approved|denied (or omitted for all)
        // Admin only. The dashboard's stat cards link here - unlike the
        // Dashboard's "Recent Activity" section (capped at 10, mixed
        // statuses), this shows every matching EditHistory record with no
        // cap, filtered to one status at a time.
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

        /* ---------------- Helpers ---------------- */

        // Ports the mockup's validateUPC() logic exactly: accepts a
        // 12-digit UPC-A or 13-digit EAN-13, normalizes to 13 digits, and
        // checks the EAN-13 check digit. Returns null + an error message
        // on any failure.
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

        // True if the product has ANY allergen flagged (contains OR may
        // contain) - used to show the amber warning badge in search
        // results / recent searches, without needing the full lists.
        private bool HasAnyAllergenInfo(Product product)
        {
            List<string> contains, mayContain;
            AllergenHelper.SplitContainsAndMayContain(product.IngredientsAllergens, out contains, out mayContain);
            return contains.Count > 0 || mayContain.Count > 0;
        }

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

        // Converts a full Product into the lightweight shape the Recent
        // Searches view renders - reused when falling back to "show every
        // registered product" (see Recent() above).
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