using System.Linq;
using System.Web.Mvc;
using PIV11.Infrastructure;
using PIV11.Models;

namespace PIV11.Controllers
{
    /* =====================================================================
       AccountController
       Login/Logout, public shopper self-registration, and admin-only
       employee account creation. Real accounts now - any number of
       employee or shopper accounts can exist, each with its own
       username/password/display name/optional member ID, all
       distinguished by the Role column (see Models/User.cs).

       Admin accounts are NOT creatable through any UI here - only the
       one seeded admin account exists, by design (keeps the highest
       privilege level from being self-service).
       ===================================================================== */
    public class AccountController : Controller
    {
        // GET: /Account/Login
        public ActionResult Login()
        {
            if (SessionHelper.IsLoggedIn)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Login
        // Any username can now be tried (no more hardcoded literal list) -
        // the account either exists in the Users table or it doesn't.
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

                // Deliberately the same generic message whether the
                // username doesn't exist or the password is wrong - now
                // that accounts are self-registerable, revealing which
                // one was incorrect would let someone enumerate valid
                // usernames.
                if (account == null || account.Pass != password)
                {
                    ModelState.AddModelError("", "Incorrect username or password. Try again.");
                    return View();
                }

                SessionHelper.LogIn(account.UserID, account.Role, account.DisplayName, account.MemberID);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Logout
        public ActionResult Logout()
        {
            SessionHelper.LogOut();
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register (public - anyone, logged in or not... but
        // redirect away if already logged in, same as Login)
        // Public self-service signup - always creates a "shopper" account.
        public ActionResult Register()
        {
            if (SessionHelper.IsLoggedIn)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Register
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

        // GET: /Account/CreateEmployee (admin only)
        public ActionResult CreateEmployee()
        {
            if (!SessionHelper.IsAdmin)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ActiveScreen = "Home";
            ViewBag.Title = "Create Employee Account";
            return View();
        }

        // POST: /Account/CreateEmployee (admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateEmployee(string username, string password, string confirmPassword, string displayName, string memberId)
        {
            if (!SessionHelper.IsAdmin)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ActiveScreen = "Home";
            ViewBag.Title = "Create Employee Account";

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
                    Role = "employee",
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
                    MemberID = string.IsNullOrWhiteSpace(memberId) ? null : memberId.Trim()
                };
                db.Users.Add(newAccount);
                db.SaveChanges();
            }

            TempData["EmployeeCreatedMessage"] = "Employee account '" + normalizedUsername + "' created successfully.";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/ManageAccounts (admin only)
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
        // Admin accounts can never be deleted through this - there's no
        // UI to create additional ones, so deleting the only admin would
        // permanently lock everyone out of admin features with no way
        // back short of editing the database directly.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAccount(string userId)
        {
            if (!SessionHelper.IsAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            string normalized = (userId ?? "").Trim().ToLower();

            using (var db = new NorteMartContext())
            {
                var account = db.Users.FirstOrDefault(u => u.UserID == normalized);
                if (account == null)
                {
                    return RedirectToAction("ManageAccounts");
                }

                if (account.Role == "admin")
                {
                    TempData["AccountDeleteError"] = "Admin accounts can't be deleted.";
                    return RedirectToAction("ManageAccounts");
                }

                // Cascades to that account's own My People data at the
                // database level (see 06_Add_People_Delete_Cascade.sql).
                // EditHistory rows are untouched - EditedByUser is a plain
                // string, not a foreign key, so the historical record of
                // what this account did stays intact even after deletion.
                db.Users.Remove(account);
                db.SaveChanges();
                TempData["AccountDeleteMessage"] = "Account '" + normalized + "' deleted.";
            }

            return RedirectToAction("ManageAccounts");
        }

        /* ---------------- Helpers ---------------- */

        // Shared validation for both Register and CreateEmployee - returns
        // null when everything's fine, or an error message to show.
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