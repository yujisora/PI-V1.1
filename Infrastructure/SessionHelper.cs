using System.Collections.Generic;
using System.Web;
using PIV11.Models;
using PIV11.Models.ViewModels;

namespace PIV11.Infrastructure
{
    /* =====================================================================
       SessionHelper
       Centralizes all reads/writes to Session so no controller or view
       has to know the exact Session key names.

       Login state is now a REAL account system: Role is stored
       separately from the username (previously the username itself WAS
       the role, which only worked because exactly three fixed accounts
       existed). Any number of accounts can now share a role - admin
       creates employee accounts, anyone can self-register as shopper.

       Also centralizes the "Recent Searches" list and "which product was
       last viewed" tracking, since BOTH the Home search (HomeController)
       and viewing a product directly (ProductController) need to record
       the exact same way - putting that logic here means there's one
       place to keep it consistent instead of two controllers duplicating
       it slightly differently.
       ===================================================================== */
    public static class SessionHelper
    {
        private const string UsernameKey = "Username";
        private const string RoleKey = "Role";
        private const string DisplayNameKey = "DisplayName";
        private const string MemberIdKey = "MemberID";
        private const string RecentSearchesKey = "RecentSearches";
        private const string CurrentProductKey = "CurrentProductUPC";
        private const int MaxRecentSearches = 20;

        /* ---------------- Login state ---------------- */

        // Stores everything about the logged-in account after a
        // successful login - all four pieces come from the Users row,
        // read once at login time so every later page doesn't need a
        // fresh database query just to render the header.
        public static void LogIn(string username, string role, string displayName, string memberId)
        {
            HttpContext.Current.Session[UsernameKey] = username;
            HttpContext.Current.Session[RoleKey] = role;
            HttpContext.Current.Session[DisplayNameKey] = displayName;
            HttpContext.Current.Session[MemberIdKey] = memberId;
        }

        // Clears the session on logout - including Recent Searches and
        // the "current product" nav tracking, so neither carries over
        // into the next login (whether that's a re-login as the same
        // account or switching to a different one). During an active
        // session these are meant to persist across pages; only logout
        // should reset them.
        public static void LogOut()
        {
            HttpContext.Current.Session.Remove(UsernameKey);
            HttpContext.Current.Session.Remove(RoleKey);
            HttpContext.Current.Session.Remove(DisplayNameKey);
            HttpContext.Current.Session.Remove(MemberIdKey);
            HttpContext.Current.Session.Remove(RecentSearchesKey);
            HttpContext.Current.Session.Remove(CurrentProductKey);
        }

        // The login username (credential) - null if nobody is logged in.
        public static string CurrentUsername
        {
            get { return HttpContext.Current.Session[UsernameKey] as string; }
        }

        // "admin" / "employee" / "shopper", or null if logged out.
        public static string CurrentRole
        {
            get { return HttpContext.Current.Session[RoleKey] as string; }
        }

        // The account's real name, for display (e.g. header "John | Employee").
        // Falls back to the username itself if no display name was set
        // (covers the original seeded accounts / any account created
        // without one).
        public static string CurrentDisplayName
        {
            get
            {
                var name = HttpContext.Current.Session[DisplayNameKey] as string;
                return string.IsNullOrWhiteSpace(name) ? CurrentUsername : name;
            }
        }

        // Optional Worker ID (employee) / Shopper ID (shopper) - may be null.
        public static string CurrentMemberId
        {
            get { return HttpContext.Current.Session[MemberIdKey] as string; }
        }

        public static bool IsLoggedIn
        {
            get { return CurrentUsername != null; }
        }

        public static bool IsAdmin
        {
            get { return CurrentRole == "admin"; }
        }

        public static bool IsEmployee
        {
            get { return CurrentRole == "employee"; }
        }

        public static bool IsShopper
        {
            get { return CurrentRole == "shopper"; }
        }

        // True for roles that can propose or apply product edits - admin
        // applies directly, an employee submits changes for admin review.
        // "shopper" is deliberately excluded: no Edit Product button, and
        // the Edit screen itself redirects away if reached by direct URL.
        public static bool CanEditProducts
        {
            get { return IsAdmin || IsEmployee; }
        }

        // True for roles that can register brand-new products via Add
        // Product. "shopper" is excluded here too - search/view only.
        public static bool CanAddProducts
        {
            get { return IsAdmin || IsEmployee; }
        }

        // True for roles that have a personal "My People" allergen
        // circle. Shopper-only now - admin never had it, and employee
        // lost it per updated project requirements (an employee manages
        // product data, not a personal allergy circle).
        public static bool CanAccessMyPeople
        {
            get { return IsShopper; }
        }

        /* ---------------- Recent searches / current product ---------------- */

        // The full recent-searches list, most-recently-viewed first.
        // Session-only by design (not persisted to the database) - it
        // resets whenever the browser session ends.
        public static List<RecentSearchItem> GetRecentSearches()
        {
            return HttpContext.Current.Session[RecentSearchesKey] as List<RecentSearchItem>
                   ?? new List<RecentSearchItem>();
        }

        // The UPC of whichever product was most recently viewed - used by
        // _Layout.cshtml to enable the "Product Info" nav link/back arrow
        // even after navigating away to Home, My People, etc.
        public static decimal? CurrentProductUPC
        {
            get { return HttpContext.Current.Session[CurrentProductKey] as decimal?; }
        }

        // Call this any time a product is actually shown to the person
        // (after a search, after clicking a Recent Searches entry, after
        // adding a new product, or when Product Info itself loads). Moves
        // the product to the front of Recent Searches (or adds it) and
        // marks it as the "current" product for header navigation.
        public static void RecordProductView(Product product, bool hasAllergens)
        {
            var list = GetRecentSearches();
            list.RemoveAll(r => r.UPC == product.UPC);
            list.Insert(0, new RecentSearchItem
            {
                UPC = product.UPC,
                Name = product.ProductName,
                Brand = product.Brand,
                HasAllergens = hasAllergens
            });
            if (list.Count > MaxRecentSearches)
            {
                list.RemoveRange(MaxRecentSearches, list.Count - MaxRecentSearches);
            }
            HttpContext.Current.Session[RecentSearchesKey] = list;
            HttpContext.Current.Session[CurrentProductKey] = product.UPC;
        }

        // Call this when a product is deleted, so it immediately
        // disappears from Recent Searches and stops being tracked as the
        // "current product" for header navigation - without this, a
        // deleted product kept showing up until the session reset itself
        // (e.g. on logout).
        public static void ForgetProduct(decimal upc)
        {
            var list = GetRecentSearches();
            list.RemoveAll(r => r.UPC == upc);
            HttpContext.Current.Session[RecentSearchesKey] = list;

            decimal? current = CurrentProductUPC;
            if (current.HasValue && current.Value == upc)
            {
                HttpContext.Current.Session.Remove(CurrentProductKey);
            }
        }
    }
}