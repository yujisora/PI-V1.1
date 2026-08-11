using System.Linq;
using System.Web.Mvc;
using PIV11.Infrastructure;
using PIV11.Models;

namespace PIV11.Controllers
{
    /// <summary>
    /// Login/Logout, public shopper self-registration
    /// (<see cref="Register()"/>/<see cref="Register(string, string, string, string, string)"/>),
    /// admin-only employee/admin account creation
    /// (<see cref="CreateEmployee()"/>/<see cref="CreateEmployee(string, string, string, string, string, string)"/>),
    /// admin-only account management
    /// (<see cref="ManageAccounts"/>, <see cref="DeleteAccount"/>), a public
    /// Privacy Policy page, and change-name/change-password for any
    /// logged-in role. Any number of admin accounts can now exist, but
    /// the one literal seeded account named "admin" can never be deleted
    /// - that's what actually guarantees the app can never end up with
    /// zero admins, regardless of how many other admin accounts get
    /// created or removed.
    /// </summary>
    public class AccountController : Controller
    {
        /// <summary>GET: <c>/Account/Login</c>. Redirects to Home if already logged in; otherwise shows the login form.</summary>
        public ActionResult Login()
        {
            if (SessionHelper.IsLoggedIn)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        /// <summary>
        /// POST: <c>/Account/Login</c>. Looks up <paramref name="username"/>
        /// directly against the <c>Users</c> table (no fixed literal list
        /// anymore) and checks the password. On success, logs in via
        /// <see cref="SessionHelper.LogIn"/> using the account's real
        /// role/display name/member ID.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                ModelState.AddModelError("", "Please enter a username.");
                return View();
            }

            string normalizedUsername = username.Trim().ToLower();

            using (var db = new NorteMartContext())
            {
                var account = db.Users.FirstOrDefault(u => u.UserID == normalizedUsername);

                if (account == null || account.Pass != password)
                {
                    ModelState.AddModelError("", "Incorrect username or password. Try again.");
                    return View();
                }

                SessionHelper.LogIn(account.UserID, account.Role, account.DisplayName, account.MemberID);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>GET: <c>/Account/Logout</c>. Clears the session and redirects to Home. Safe to call whether or not a session is active.</summary>
        public ActionResult Logout()
        {
            SessionHelper.LogOut();
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        /// <summary>GET: <c>/Account/Register</c>. Public self-service signup form - always results in a <c>"shopper"</c> account. 
        /// Redirects to Home if already logged in.</summary>
        public ActionResult Register()
        {
            if (SessionHelper.IsLoggedIn)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        /// <summary>
        /// POST: <c>/Account/Register</c>. Validates via
        /// <see cref="ValidateNewAccountFields"/>, checks the username isn't
        /// taken, creates a new <c>Role = "shopper"</c> account, and logs it
        /// in immediately - no separate "now sign in" step.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string username, string password, string confirmPassword, string displayName, string memberId)
        {
            var error = ValidateNewAccountFields(username, password, confirmPassword);
            if (error != null)
            {
                ModelState.AddModelError("", error);
                return View();
            }

            string normalizedUsername = username.Trim().ToLower();

            using (var db = new NorteMartContext())
            {
                if (db.Users.Any(u => u.UserID == normalizedUsername))
                {
                    ModelState.AddModelError("", "That username is already taken.");
                    return View();
                }

                var newAccount = new User
                {
                    UserID = normalizedUsername,
                    Pass = password,
                    Role = "shopper",
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
                    MemberID = string.IsNullOrWhiteSpace(memberId) ? null : memberId.Trim()
                };
                db.Users.Add(newAccount);
                db.SaveChanges();

                SessionHelper.LogIn(newAccount.UserID, newAccount.Role, newAccount.DisplayName, newAccount.MemberID);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>GET: <c>/Account/CreateEmployee</c>. Admin only - redirects to Home for anyone else (covers both "not logged in" and "wrong role"). Lets admin choose the new account's role (employee or admin).</summary>
        public ActionResult CreateEmployee()
        {
            if (!SessionHelper.IsAdmin)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ActiveScreen = "Home";
            ViewBag.Title = "Create Account";
            return View();
        }

        /// <summary>
        /// POST: <c>/Account/CreateEmployee</c>. Admin only. Same validation
        /// pattern as <see cref="Register(string, string, string, string, string)"/>
        /// but creates either a <c>Role = "employee"</c> or <c>Role = "admin"</c>
        /// account depending on <paramref name="role"/> (unrecognized/missing
        /// values fall back to <c>"employee"</c> - the safer default), and
        /// does <b>not</b> log the new account in - the admin stays logged in
        /// as themselves and sees a confirmation on the Dashboard instead.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateEmployee(string username, string password, string confirmPassword, string displayName, string memberId, string role)
        {
            if (!SessionHelper.IsAdmin)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ActiveScreen = "Home";
            ViewBag.Title = "Create Account";

            var error = ValidateNewAccountFields(username, password, confirmPassword);
            if (error != null)
            {
                ModelState.AddModelError("", error);
                return View();
            }

            string normalizedUsername = username.Trim().ToLower();
            string normalizedRole = (role ?? "").Trim().ToLower() == "admin" ? "admin" : "employee";

            using (var db = new NorteMartContext())
            {
                if (db.Users.Any(u => u.UserID == normalizedUsername))
                {
                    ModelState.AddModelError("", "That username is already taken.");
                    return View();
                }

                var newAccount = new User
                {
                    UserID = normalizedUsername,
                    Pass = password,
                    Role = normalizedRole,
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
                    MemberID = string.IsNullOrWhiteSpace(memberId) ? null : memberId.Trim()
                };
                db.Users.Add(newAccount);
                db.SaveChanges();
            }

            string roleLabel = normalizedRole == "admin" ? "Admin" : "Employee";
            TempData["EmployeeCreatedMessage"] = roleLabel + " account '" + normalizedUsername + "' created successfully.";
            return RedirectToAction("Index", "Home");
        }

        /// <summary>GET: <c>/Account/ManageAccounts</c>. Admin only. Lists every account, ordered by role then username.</summary>
        public ActionResult ManageAccounts()
        {
            if (!SessionHelper.IsAdmin)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ActiveScreen = "Home";
            ViewBag.Title = "Manage Accounts";

            using (var db = new NorteMartContext())
            {
                var accounts = db.Users.OrderBy(u => u.Role).ThenBy(u => u.UserID).ToList();
                return View(accounts);
            }
        }

        // POST: /Account/DeleteAccount (admin only)
        /// <summary>
        /// POST: <c>/Account/DeleteAccount</c>. Admin only. Deletes any
        /// account except the one literally named <c>"admin"</c> - that
        /// specific account is hard-blocked (checked here, not just hidden
        /// in the UI), since it's the one guaranteed anchor that keeps the
        /// app from ever ending up with zero admins. Other admin-role
        /// accounts (created via <see cref="CreateEmployee(string, string, string, string, string, string)"/>)
        /// CAN be deleted like any other account. Cascades to that
        /// account's own My People data at the database level;
        /// <c>EditHistory</c> rows are untouched since <c>EditedByUser</c> is a
        /// plain string, not a foreign key.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAccount(string userId)
        {
            if (!SessionHelper.IsAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            string normalized = (userId ?? "").Trim().ToLower();

            if (normalized == "admin")
            {
                TempData["AccountDeleteError"] = "The 'admin' account can't be deleted.";
                return RedirectToAction("ManageAccounts");
            }

            using (var db = new NorteMartContext())
            {
                var account = db.Users.FirstOrDefault(u => u.UserID == normalized);
                if (account == null)
                {
                    return RedirectToAction("ManageAccounts");
                }

                // Cascades to that account's own My People data at the database level.
                // EditHistory rows are untouched - EditedByUser is a plain string, not a foreign key, so the historical record of
                // what this account did stays intact even after deletion.
                // Hace cascada en la información de Mi Gente de esa cuenta a nivel de base de datos. Las filas de EditHistory quedan
                // intactas - EditedByUser es una simple cadena de texto, no una llave foránea, así que el registro histórico de lo
                // que hizo esta cuenta permanece intacto incluso después de la eliminación.
                db.Users.Remove(account);
                db.SaveChanges();
                TempData["AccountDeleteMessage"] = "Account '" + normalized + "' deleted.";
            }

            return RedirectToAction("ManageAccounts");
        }

        /// <summary>GET: <c>/Account/PrivacyPolicy</c>. Public - no login required.</summary>
        public ActionResult PrivacyPolicy()
        {
            return View();
        }

        /// <summary>GET: <c>/Account/ChangePassword</c>. Any logged-in role. Redirects to Login if no session is active.</summary>
        public ActionResult ChangePassword()
        {
            if (!SessionHelper.IsLoggedIn)
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        /// <summary>
        /// POST: <c>/Account/ChangePassword</c>. Requires the correct current
        /// password before accepting a new one. Scoped to the logged-in
        /// account only (<see cref="SessionHelper.CurrentUsername"/>) -
        /// works identically for every role, since it just updates that
        /// account's own <c>Users</c> row.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmNewPassword)
        {
            if (!SessionHelper.IsLoggedIn)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                ModelState.AddModelError("", "Enter your current password.");
                return View();
            }
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError("", "Choose a new password.");
                return View();
            }
            if (newPassword != confirmNewPassword)
            {
                ModelState.AddModelError("", "New passwords do not match.");
                return View();
            }

            using (var db = new NorteMartContext())
            {
                var account = db.Users.FirstOrDefault(u => u.UserID == SessionHelper.CurrentUsername);
                if (account == null)
                {
                    SessionHelper.LogOut();
                    return RedirectToAction("Login", "Account");
                }

                if (account.Pass != currentPassword)
                {
                    ModelState.AddModelError("", "Current password is incorrect.");
                    return View();
                }

                account.Pass = newPassword;
                db.SaveChanges();
            }

            TempData["PasswordChangedMessage"] = "Your password has been updated.";
            return RedirectToAction("Index", "Home");
        }

        /// <summary>GET: <c>/Account/ChangeDisplayName</c>. Any logged-in role. Redirects to Login if no session is active.</summary>
        public ActionResult ChangeDisplayName()
        {
            if (!SessionHelper.IsLoggedIn)
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        /// <summary>
        /// POST: <c>/Account/ChangeDisplayName</c>. Updates the logged-in
        /// account's <c>DisplayName</c> - blank clears it back to falling
        /// through to the raw username. Re-calls <see cref="SessionHelper.LogIn"/>
        /// with the fresh value right after saving, so the header pill
        /// updates immediately instead of only after the next login.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeDisplayName(string displayName)
        {
            if (!SessionHelper.IsLoggedIn)
            {
                return RedirectToAction("Login", "Account");
            }

            using (var db = new NorteMartContext())
            {
                var account = db.Users.FirstOrDefault(u => u.UserID == SessionHelper.CurrentUsername);
                if (account == null)
                {
                    SessionHelper.LogOut();
                    return RedirectToAction("Login", "Account");
                }

                account.DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
                db.SaveChanges();

                SessionHelper.LogIn(account.UserID, account.Role, account.DisplayName, account.MemberID);
            }

            TempData["NameChangedMessage"] = "Your name has been updated.";
            return RedirectToAction("Index", "Home");
        }

        /* Helpers - Ayudantes */
        /// <summary>
        /// Shared field validation for <see cref="Register(string, string, string, string, string)"/>
        /// and <see cref="CreateEmployee(string, string, string, string, string, string)"/>:
        /// blank username, then blank password, then password/confirm
        /// mismatch, checked in that order.
        /// </summary>
        /// <returns><c>null</c> if everything's valid; otherwise the first applicable error message.</returns>
        private string ValidateNewAccountFields(string username, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return "Please choose a username.";
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                return "Please choose a password.";
            }
            if (password != confirmPassword)
            {
                return "Passwords do not match.";
            }
            return null;
        }
    }
}