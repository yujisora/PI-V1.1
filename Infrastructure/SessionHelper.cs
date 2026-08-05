using System.Collections.Generic;
using System.Web;
using PIV11.Models;
using PIV11.Models.ViewModels;

namespace PIV11.Infrastructure
{
    
    /// <summary>
    /// Central point for every read/write to <c>Session</c> - login state (username/role/display name/member ID), Recent Searches, and which
    /// product was last viewed. No controller or view touches <c>HttpContext.Current.Session</c> directly; everything goes through
    /// this class so the Session key names and shapes only need to be known in one place.
    /// </summary>
    public static class SessionHelper
    {
        private const string UsernameKey = "Username";
        private const string RoleKey = "Role";
        private const string DisplayNameKey = "DisplayName";
        private const string MemberIdKey = "MemberID";
        private const string RecentSearchesKey = "RecentSearches";
        private const string CurrentProductKey = "CurrentProductUPC";
        private const int MaxRecentSearches = 20;

        // Login state

        // Stores everything about the logged-in account after a successful login - all four pieces come from the Users row,
        // read once at login time so every later page doesn't need a fresh database query just to render the header.

            // Estado de inicio de sesión

            // Guarda todo sobre la cuenta con sesión iniciada después de un inicio de sesión exitoso - las cuatro piezas provienen
            // de la fila de Users, leídas una sola vez al iniciar sesión para que ninguna página posterior necesite una nueva
            // consulta a la base de datos solo para renderizar el encabezado.

        /// <summary>
        /// Stores the full logged-in state after a successful login. Called once, at login time, with values read straight from the
        /// <c>Users</c> row - later pages read them back from Session instead of re-querying the database on every request.
        /// </summary>
        /// <param name="username">The login credential (<c>Users.UserID</c>).</param>
        /// <param name="role">One of <c>"admin"</c>, <c>"employee"</c>, <c>"shopper"</c>.</param>
        /// <param name="displayName">The account's real name, or <c>null</c> if none was set.</param>
        /// <param name="memberId">Optional Worker/Shopper ID, or <c>null</c>.</param>
        public static void LogIn(string username, string role, string displayName, string memberId)
        {
            HttpContext.Current.Session[UsernameKey] = username;
            HttpContext.Current.Session[RoleKey] = role;
            HttpContext.Current.Session[DisplayNameKey] = displayName;
            HttpContext.Current.Session[MemberIdKey] = memberId;
        }

        /// <summary>
        /// Clears the entire session on logout - login state, Recent
        /// Searches, and the "current product" nav tracking - so nothing
        /// carries over into the next login, whether that's the same
        /// account signing back in or a different one.
        /// </summary>
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
            // El nombre de usuario de acceso (credencial) - null si nadie ha iniciado sesión.
        /// <summary>The login username (credential), or <c>null</c> if nobody is logged in.</summary>
        public static string CurrentUsername
        {
            get { return HttpContext.Current.Session[UsernameKey] as string; }
        }

        // "admin" / "employee" / "shopper", or null if logged out.
            // "admin" / "employee" / "shopper", o null si no hay sesión iniciada.
        /// <summary>The current account's role: <c>"admin"</c>, <c>"employee"</c>, or <c>"shopper"</c>; <c>null</c> if logged out.</summary>
        public static string CurrentRole
        {
            get { return HttpContext.Current.Session[RoleKey] as string; }
        }

        /// The account's real name for display (e.g. header "John | Employee").
        /// Falls back to <see cref="CurrentUsername"/> if no display name was ever set.
        /// </summary>
        public static string CurrentDisplayName
        {
            get
            {
                var name = HttpContext.Current.Session[DisplayNameKey] as string;
                return string.IsNullOrWhiteSpace(name) ? CurrentUsername : name;
            }
        }

        // Optional Worker ID (employee) / Shopper ID (shopper) - may be null.
            // ID de Trabajador (employee) / ID de Comprador (shopper) opcional - puede ser null.
        /// <summary>Optional Worker ID (employee) / Shopper ID (shopper); may be <c>null</c>.</summary>
        public static string CurrentMemberId
        {
            get { return HttpContext.Current.Session[MemberIdKey] as string; }
        }

        /// <summary>True if any account is currently logged in.</summary>
        public static bool IsLoggedIn
        {
            get { return CurrentUsername != null; }
        }

        /// <summary>True if the current account's role is <c>"admin"</c>.</summary>
        public static bool IsAdmin
        {
            get { return CurrentRole == "admin"; }
        }

        /// <summary>True if the current account's role is <c>"employee"</c>.</summary>
        public static bool IsEmployee
        {
            get { return CurrentRole == "employee"; }
        }

        /// <summary>True if the current account's role is <c>"shopper"</c>.</summary>
        public static bool IsShopper
        {
            get { return CurrentRole == "shopper"; }
        }

        /// <summary>
        /// True for roles allowed to propose or apply product edits (admin applies directly, employee submits for review). Gates both the
        /// Edit Product button/link and the Edit action itself server-side.
        /// </summary>
        public static bool CanEditProducts
        {
            get { return IsAdmin || IsEmployee; }
        }

        /// <summary>True for roles allowed to add brand-new products. Shopper is excluded - search/view only.</summary>
        public static bool CanAddProducts
        {
            get { return IsAdmin || IsEmployee; }
        }

        /// <summary>
        /// True only for the shopper role - the one role with a personal "My People" allergen circle. Only shopper has it.
        /// </summary>
        public static bool CanAccessMyPeople
        {
            get { return IsShopper; }
        }

        /// <summary>
        /// The full Recent Searches list, most-recently-viewed first. Session-only by design - never persisted to the database, so it
        /// resets whenever the browser session ends.
        /// </summary>
        public static List<RecentSearchItem> GetRecentSearches()
        {
            return HttpContext.Current.Session[RecentSearchesKey] as List<RecentSearchItem>
                   ?? new List<RecentSearchItem>();
        }

        /// <summary>
        /// The UPC of whichever product was most recently viewed, or  <c>null</c> if none this session. Drives <c>_Layout.cshtml</c>'s
        /// "Product Info" nav link/back arrow even after navigating away.
        /// </summary>
        public static decimal? CurrentProductUPC
        {
            get { return HttpContext.Current.Session[CurrentProductKey] as decimal?; }
        }

        /// <summary>
        /// Records that <paramref name="product"/> was just shown to the person - moves it to the front of Recent Searches (adding it if
        /// new), trims the list to <see cref="MaxRecentSearches"/>, and marks it as the "current" product for header navigation. 
        /// Call this from every code path that actually displays a product (search, clicking a Recent Searches entry, adding a new product,
        /// or Product Info loading directly).
        /// </summary>
        /// <param name="product">The product that was shown.</param>
        /// <param name="hasAllergens">Whether it has any Contains/May-Contain allergen flagged, for the Recent Searches warning badge.</param>
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

        /// <summary>
        /// Call this when a product is deleted, so it immediately disappears from Recent Searches and stops being tracked as the "current
        /// product" - without this it would keep showing up until the session next reset (e.g. on logout).
        /// </summary>
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