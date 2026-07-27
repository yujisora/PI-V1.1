using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using PIV11.Infrastructure;
using PIV11.Models;
using PIV11.Models.ViewModels;

namespace PIV11.Controllers
{
    /* =====================================================================
       ProductController
       Product Info (view), Edit (form), and Edit History (admin review
       queue) - everything that revolves around one specific product,
       identified by its UPC.
       ===================================================================== */
    public class ProductController : Controller
    {
        // GET: /Product/Info?upc=...
        public ActionResult Info(decimal upc)
        {
            using (var db = new NorteMartContext())
            {
                var product = db.Products
                    .Include(p => p.Foodstuff)
                    .Include(p => p.IngredientsAllergens)
                    .Include(p => p.NutritionData)
                    .Include(p => p.HealthAlert)
                    .FirstOrDefault(p => p.UPC == upc);

                if (product == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                List<string> contains, mayContain;
                AllergenHelper.SplitContainsAndMayContain(product.IngredientsAllergens, out contains, out mayContain);

                // Keep Session's "current product" / Recent Searches in sync
                // even when this product was reached without going through
                // Home's search (nav pill, Recent Searches list, My People, etc).
                SessionHelper.RecordProductView(product, contains.Count > 0 || mayContain.Count > 0);

                var vm = new ProductInfoViewModel
                {
                    UPC = product.UPC,
                    ProductName = product.ProductName,
                    Brand = product.Brand,
                    NetVolume = product.Foodstuff != null ? product.Foodstuff.NetVolume : "",
                    UnitMeasurement = product.Foodstuff != null ? product.Foodstuff.UnitMeasurement : "",
                    ImageUrl = product.ImageUrl,
                    Ingredients = product.IngredientsAllergens != null ? product.IngredientsAllergens.Ingredients : "",
                    AllergensContains = contains,
                    AllergensMayContain = mayContain
                };

                // Nutrition facts, in display order. Trans Fat is skipped
                // entirely when it's zero/empty - matches the mockup,
                // which hides a zero Trans Fat row in VIEW mode only (the
                // Edit screen always shows every field).
                foreach (var field in ProductEditHelper.NutritionFields)
                {
                    int? value = ProductEditHelper.GetNutritionValue(product.NutritionData, field.Column);
                    if (field.Column == "TransFats" && (!value.HasValue || value.Value == 0))
                    {
                        continue;
                    }
                    vm.NutritionFacts.Add(new NutritionFactDisplay
                    {
                        Label = field.Label,
                        Value = value,
                        Unit = field.Unit,
                        Indented = field.Indented
                    });
                }

                // Only the ACTIVE seals are shown on Product Info (the Edit
                // screen shows all 8 regardless of state).
                foreach (var seal in ProductEditHelper.SealDefinitions)
                {
                    bool isActive = ProductEditHelper.GetSealValue(product.HealthAlert, seal.Key);
                    if (isActive)
                    {
                        vm.ActiveSeals.Add(new SealDisplay
                        {
                            Key = seal.Key,
                            Label = seal.Label,
                            SubLabel = seal.SubLabel,
                            IsOctagon = seal.IsOctagon,
                            IsActive = true
                        });
                    }
                }

                // My People panel - user and shopper roles.
                if (SessionHelper.CanAccessMyPeople)
                {
                    vm.MyPeopleGroups = BuildMyPeopleGroups(db, contains, mayContain);
                }

                // Pending-edit badge - admin role only.
                if (SessionHelper.IsAdmin)
                {
                    vm.PendingEditCount = db.EditHistory.Count(e => e.UPC == upc && e.Status == "pending");
                }

                ViewBag.ActiveScreen = "Product Info";
                ViewBag.ShowBack = true;
                ViewBag.Title = product.ProductName;
                return View(vm);
            }
        }

        // GET: /Product/Edit?upc=...
        public ActionResult Edit(decimal upc)
        {
            if (!SessionHelper.IsLoggedIn)
            {
                return RedirectToAction("Login", "Account");
            }
            if (!SessionHelper.CanEditProducts)
            {
                return RedirectToAction("Info", new { upc = upc });
            }

            using (var db = new NorteMartContext())
            {
                var product = db.Products
                    .Include(p => p.Foodstuff)
                    .Include(p => p.IngredientsAllergens)
                    .Include(p => p.NutritionData)
                    .Include(p => p.HealthAlert)
                    .FirstOrDefault(p => p.UPC == upc);

                if (product == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var model = new EditProductViewModel
                {
                    UPC = product.UPC,
                    ProductName = product.ProductName,
                    Brand = product.Brand,
                    ImageUrl = product.ImageUrl,
                    Weight = product.Foodstuff != null ? product.Foodstuff.NetVolume : "",
                    Unit = product.Foodstuff != null ? product.Foodstuff.UnitMeasurement : "g",
                    Ingredients = product.IngredientsAllergens != null ? product.IngredientsAllergens.Ingredients : ""
                };

                foreach (var ak in AllergenHelper.Keys)
                {
                    model.SetAllergenValue(ak.Key, AllergenHelper.GetTriState(product.IngredientsAllergens, ak.Key));
                }
                foreach (var field in ProductEditHelper.NutritionFields)
                {
                    model.SetNutritionValue(field.Column, ProductEditHelper.GetNutritionValue(product.NutritionData, field.Column));
                }
                foreach (var seal in ProductEditHelper.SealDefinitions)
                {
                    model.SetSealValue(seal.Key, ProductEditHelper.GetSealValue(product.HealthAlert, seal.Key));
                }

                ViewBag.ActiveScreen = "Edit";
                ViewBag.ShowBack = true;
                ViewBag.ShowSave = true;
                ViewBag.SaveFormId = "editProductForm";
                ViewBag.BackToProductInfo = true;
                ViewBag.Title = "Edit " + product.ProductName;
                return View(model);
            }
        }

        // POST: /Product/Edit
        // Admin: every change applies immediately.
        // Logged-in "user": changes to fields that already had a value go
        // into EditHistory as "pending" instead of touching live data;
        // filling in previously-empty nutrition/allergen/seal data (e.g.
        // right after Add Product) applies immediately for anyone, since
        // there's nothing established yet to protect.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EditProductViewModel model)
        {
            if (!SessionHelper.IsLoggedIn)
            {
                return RedirectToAction("Login", "Account");
            }
            if (!SessionHelper.CanEditProducts)
            {
                return RedirectToAction("Info", new { upc = model.UPC });
            }

            using (var db = new NorteMartContext())
            {
                var product = db.Products.FirstOrDefault(p => p.UPC == model.UPC);
                if (product == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var foodstuff = db.Foodstuffs.FirstOrDefault(f => f.UPC == model.UPC);
                if (foodstuff == null)
                {
                    foodstuff = new Foodstuff { UPC = model.UPC };
                    db.Foodstuffs.Add(foodstuff);
                }

                var ia = db.IngredientsAllergens.FirstOrDefault(x => x.UPC == model.UPC);
                bool iaIsNew = ia == null;
                if (ia == null)
                {
                    ia = new IngredientsAllergen { UPC = model.UPC, Ingredients = "" };
                    db.IngredientsAllergens.Add(ia);
                }

                var nd = db.NutritionRecords.FirstOrDefault(x => x.UPC == model.UPC);
                bool ndIsNew = nd == null;
                if (nd == null)
                {
                    nd = new NutritionData { UPC = model.UPC };
                    db.NutritionRecords.Add(nd);
                }

                var ha = db.HealthAlerts.FirstOrDefault(x => x.UPC == model.UPC);
                bool haIsNew = ha == null;
                if (ha == null)
                {
                    ha = new HealthAlert { UPC = model.UPC };
                    db.HealthAlerts.Add(ha);
                }

                string username = SessionHelper.CurrentUsername;
                int pendingCount = 0;

                // Product name/brand/image/weight/unit always already have
                // real values (Add Product guarantees Products+Foodstuffs
                // exist), so these always go through the normal role-based
                // routing - never treated as "brand new".
                pendingCount += ApplyOrQueue(db,
                    ProductEditHelper.ComputeCoreChanges(product, foodstuff, model),
                    product, foodstuff, ia, nd, ha, bypassPending: false, username: username);

                pendingCount += ApplyOrQueue(db,
                    ProductEditHelper.ComputeIngredientsChanges(ia, model),
                    product, foodstuff, ia, nd, ha, bypassPending: iaIsNew, username: username);

                pendingCount += ApplyOrQueue(db,
                    ProductEditHelper.ComputeNutritionChanges(nd, model),
                    product, foodstuff, ia, nd, ha, bypassPending: ndIsNew, username: username);

                pendingCount += ApplyOrQueue(db,
                    ProductEditHelper.ComputeSealChanges(ha, model),
                    product, foodstuff, ia, nd, ha, bypassPending: haIsNew, username: username);

                db.SaveChanges();

                TempData["EditMessage"] = pendingCount > 0
                    ? "Some of your changes were submitted for admin review. The rest were saved."
                    : "Changes saved.";

                return RedirectToAction("Info", new { upc = model.UPC });
            }
        }

        // GET: /Product/History?upc=... (admin only)
        public ActionResult History(decimal upc)
        {
            if (!SessionHelper.IsAdmin)
            {
                return RedirectToAction("Info", new { upc = upc });
            }

            using (var db = new NorteMartContext())
            {
                var product = db.Products.FirstOrDefault(p => p.UPC == upc);
                if (product == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var vm = new EditHistoryViewModel
                {
                    UPC = upc,
                    ProductName = product.ProductName,
                    Records = db.EditHistory
                        .Where(e => e.UPC == upc)
                        .OrderByDescending(e => e.DateEdited)
                        .ToList()
                };

                ViewBag.ActiveScreen = "Product Info";
                ViewBag.ShowBack = true;
                ViewBag.BackToProductInfo = true;
                ViewBag.Title = "Edit History - " + product.ProductName;
                return View(vm);
            }
        }

        // POST: /Product/ApproveEdit (admin only)
        // Actually applies the pending change to the live product data.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApproveEdit(int editId, decimal upc)
        {
            if (!SessionHelper.IsAdmin)
            {
                return RedirectToAction("Info", new { upc = upc });
            }

            using (var db = new NorteMartContext())
            {
                var record = db.EditHistory.FirstOrDefault(e => e.EditID == editId);
                if (record != null && record.Status == "pending")
                {
                    var product = db.Products.FirstOrDefault(p => p.UPC == record.UPC);
                    if (product != null)
                    {
                        var foodstuff = db.Foodstuffs.FirstOrDefault(f => f.UPC == record.UPC)
                            ?? AddNew(db, new Foodstuff { UPC = record.UPC });
                        var ia = db.IngredientsAllergens.FirstOrDefault(x => x.UPC == record.UPC)
                            ?? AddNew(db, new IngredientsAllergen { UPC = record.UPC, Ingredients = "" });
                        var nd = db.NutritionRecords.FirstOrDefault(x => x.UPC == record.UPC)
                            ?? AddNew(db, new NutritionData { UPC = record.UPC });
                        var ha = db.HealthAlerts.FirstOrDefault(x => x.UPC == record.UPC)
                            ?? AddNew(db, new HealthAlert { UPC = record.UPC });

                        ProductEditHelper.ApplyChange(
                            new FieldChangeInfo { FieldKey = record.FieldChanged, NewValue = record.NewValue },
                            product, foodstuff, ia, nd, ha);

                        record.Status = "approved";
                        db.SaveChanges();
                    }
                }
            }

            return RedirectToAction("History", new { upc = upc });
        }

        // POST: /Product/DenyEdit (admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DenyEdit(int editId, decimal upc)
        {
            if (!SessionHelper.IsAdmin)
            {
                return RedirectToAction("Info", new { upc = upc });
            }

            using (var db = new NorteMartContext())
            {
                var record = db.EditHistory.FirstOrDefault(e => e.EditID == editId);
                if (record != null && record.Status == "pending")
                {
                    record.Status = "denied";
                    db.SaveChanges();
                }
            }

            return RedirectToAction("History", new { upc = upc });
        }

        // POST: /Product/Delete (admin only)
        // Deletes the Products row; the database cascades the delete to
        // Foodstuffs/NutritionData/IngredientsAllergens/HealthAlert/
        // EditHistory automatically (see 02_Add_Delete_Cascade.sql) - EF
        // only needs to remove the one row.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(decimal upc)
        {
            if (!SessionHelper.IsAdmin)
            {
                return RedirectToAction("Info", new { upc = upc });
            }

            using (var db = new NorteMartContext())
            {
                var product = db.Products.FirstOrDefault(p => p.UPC == upc);
                if (product != null)
                {
                    db.Products.Remove(product);
                    db.SaveChanges();
                    SessionHelper.ForgetProduct(upc);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /* ---------------- Helpers ---------------- */

        // Applies every change directly if bypassPending is true (a
        // brand-new row being filled in for the first time) OR the current
        // user is admin; otherwise queues each change as a pending
        // EditHistory record instead of touching the live data. Returns
        // how many changes were queued as pending.
        private int ApplyOrQueue(NorteMartContext db, List<FieldChangeInfo> changes,
            Product product, Foodstuff foodstuff, IngredientsAllergen ia, NutritionData nd, HealthAlert ha,
            bool bypassPending, string username)
        {
            int pendingCount = 0;
            foreach (var change in changes)
            {
                if (bypassPending || SessionHelper.IsAdmin)
                {
                    ProductEditHelper.ApplyChange(change, product, foodstuff, ia, nd, ha);
                }
                else
                {
                    db.EditHistory.Add(new EditHistoryRecord
                    {
                        UPC = product.UPC,
                        EditedByUser = username,
                        FieldChanged = change.FieldKey,
                        NewValue = change.NewValue ?? "",
                        DateEdited = DateTime.Now,
                        Status = "pending"
                    });
                    pendingCount++;
                }
            }
            return pendingCount;
        }

        // Small helper so ApproveEdit's null-coalescing "load or create"
        // pattern reads cleanly for each of the four child entities.
        private T AddNew<T>(NorteMartContext db, T entity) where T : class
        {
            db.Set<T>().Add(entity);
            return entity;
        }

        private List<PeopleGroupStatus> BuildMyPeopleGroups(NorteMartContext db, List<string> contains, List<string> mayContain)
        {
            var result = new List<PeopleGroupStatus>();
            string username = SessionHelper.CurrentUsername;

            var people = db.People
                .Where(p => p.OwnerUsername == username)
                .OrderBy(p => p.GroupName)
                .ThenBy(p => p.NamePerson)
                .ToList();

            foreach (var group in people.GroupBy(p => string.IsNullOrEmpty(p.GroupName) ? "Ungrouped" : p.GroupName))
            {
                var groupStatus = new PeopleGroupStatus { GroupName = group.Key };
                foreach (var person in group)
                {
                    var personAllergens = AllergenHelper.GetPersonAllergens(person);
                    string status = "safe";
                    if (personAllergens.Any(a => contains.Contains(a)))
                    {
                        status = "danger";
                    }
                    else if (personAllergens.Any(a => mayContain.Contains(a)))
                    {
                        status = "caution";
                    }
                    groupStatus.Members.Add(new PersonStatus { Name = person.NamePerson, Status = status });
                }
                groupStatus.AffectedCount = groupStatus.Members.Count(m => m.Status != "safe");
                result.Add(groupStatus);
            }
            return result;
        }
    }
}