using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using PIV11.Infrastructure;
using PIV11.Models;
using PIV11.Models.ViewModels;

namespace PIV11.Controllers
{

    // ProductController

    // Product Info (view), Edit (form), and Edit History (admin review queue) - everything that revolves around one specific product,
    // identified by its UPC.

    // ProductController

    // Información del Producto (vista), Editar (formulario), e Historial de Ediciones (cola de revisión de admin) - todo
    // lo que gira en torno a un producto específico, identificado por su UPC.

    /// <summary>
    /// Everything that revolves around one specific product: viewing it (<see cref="Info"/>), editing it (<see cref="Edit(decimal)"/> /
    /// <see cref="Edit(EditProductViewModel)"/>), and admin's review queue for pending edits (<see cref="History"/>, <see cref="ApproveEdit"/>,
    /// <see cref="DenyEdit"/>).
    /// </summary>
    public class ProductController : Controller
    {
        /// <summary>
        /// GET: <c>/Product/Info?upc=...</c>. Shows the full product detail page - image, allergens, ingredients, nutrition table, active
        /// warning seals, and (shopper only) the My People danger/caution/ safe panel. No login required. Also records this as the
        /// "current product" for Recent Searches/header nav via <see cref="SessionHelper.RecordProductView"/>, regardless of how
        /// the product was reached.
        /// </summary>
        /// <param name="upc">The product's barcode. Redirects to Home if no matching product exists.</param>
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

                AllergenHelper.SplitContainsAndMayContain(product.IngredientsAllergens, out List<string> contains, out List<string> mayContain);

                // Keep Session's "current product" / Recent Searches in sync even when this product was reached without going through
                // Home's search (nav pill, Recent Searches list, My People, etc).

                // Mantiene el "producto actual" de Session / Búsquedas Recientes sincronizados incluso cuando se llegó a
                // este producto sin pasar por la búsqueda de Inicio (píldora de navegación, lista de Búsquedas Recientes, Mi Gente, etc).
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

                // Per-100g/ml vs. per-whole-container toggle: only offered when NetVolume actually parses as a real positive number
                // (e.g. "250") - if it's blank, zero, or non-numeric, the toggle simply isn't shown and the view falls back to
                // per-100 only, same as before this feature existed.

                // Alternar entre por 100 g/ml y por envase completo: solo se ofrece cuando NetVolume realmente se puede
                // interpretar como un número positivo real (por ejemplo, "250") - si está vacío, es cero, o no es numérico,
                // el alternador simplemente no se muestra y la vista recae en solo por-100, igual que antes de esta función.
                bool hasContainerAmount = double.TryParse(vm.NetVolume, NumberStyles.Any, CultureInfo.InvariantCulture, out double containerMultiplier)
                    && containerMultiplier > 0;
                if (hasContainerAmount)
                {
                    vm.HasContainerToggle = true;
                    vm.ContainerAmountLabel = vm.NetVolume + " " + vm.UnitMeasurement;
                    containerMultiplier = containerMultiplier / 100.0;
                }

                // Nutrition facts, in display order. Trans Fat is skipped entirely when it's zero/empty - optional nutrient,
                // which hides a zero Trans Fat row in VIEW mode only (the Edit screen always shows every field). This check is
                // always against the per-100 value, regardless of which way the toggle is currently set - it's about whether
                // the fact is meaningful at all, not about scaling.

                // Datos nutricionales, en orden de visualización. Grasas Trans se omite por completo cuando es
                // cero/vacío - nutrimento opcional, que oculta una fila de Grasas Trans en cero solo en modo VISTA (la
                // pantalla de Edición siempre muestra todos los campos). Esta verificación siempre es contra el valor
                // por-100, sin importar cómo esté puesto el alternador actualmente - se trata de si el dato es
                // significativo, no de la escala.
                foreach (var (Column, Label, Unit, Indented) in ProductEditHelper.NutritionFields)
                {
                    int? value = ProductEditHelper.GetNutritionValue(product.NutritionData, Column);
                    if (Column == "TransFats" && (!value.HasValue || value.Value == 0))
                    {
                        continue;
                    }
                    vm.NutritionFacts.Add(new NutritionFactDisplay
                    {
                        Label = Label,
                        Value = value,
                        ContainerValue = (hasContainerAmount && value.HasValue)
                            ? (int?)Math.Round(value.Value * containerMultiplier)
                            : null,
                        Unit = Unit,
                        Indented = Indented
                    });
                }

                // Only the ACTIVE seals are shown on Product Info (the Edit screen shows all 8 regardless of state).
                // Solo se muestran los sellos ACTIVOS en Información del Producto (la pantalla de Edición muestra los 8 sin importar su estado).
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
                // Panel de Mi Gente - roles user y shopper.
                if (SessionHelper.CanAccessMyPeople)
                {
                    vm.MyPeopleGroups = BuildMyPeopleGroups(db, contains, mayContain);
                }

                // Pending-edit badge - admin role only.
                // Insignia de edición pendiente - solo rol admin.
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

        /// <summary>
        /// GET: <c>/Product/Edit?upc=...</c>. Shows the Edit form pre-filled with the product's current data. Restricted to
        /// <see cref="SessionHelper.CanEditProducts"/> roles - redirects to Login if not logged in, or back to Info (not Home) if logged in
        /// but lacking permission, so a shopper stays in context rather than being bounced somewhere unrelated.
        /// </summary>
        public ActionResult Edit(decimal upc)
        {
            if (!SessionHelper.IsLoggedIn)
            {
                return RedirectToAction("Login", "Account");
            }
            if (!SessionHelper.CanEditProducts)
            {
                return RedirectToAction("Info", new { upc });
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
                foreach (var (Column, Label, Unit, Indented) in ProductEditHelper.NutritionFields)
                {
                    model.SetNutritionValue(Column, ProductEditHelper.GetNutritionValue(product.NutritionData, Column));
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
        // Logged-in "user": changes to fields that already had a value go into EditHistory as "pending" instead of touching live data;
        // filling in previously-empty nutrition/allergen/seal data (e.g. right after Add Product) applies immediately for anyone, since
        // there's nothing established yet to protect.

        // POST: /Product/Edit

        // Admin: cada cambio se aplica de inmediato.
        // "user" con sesión iniciada: los cambios a campos que ya tenían un valor van a EditHistory como "pending" en lugar
        // de tocar los datos en vivo; completar datos de nutrición/alérgeno/sello previamente vacíos (por ejemplo,
        // justo después de Agregar Producto) se aplica de inmediato para cualquiera, ya que no hay nada establecido todavía que proteger.

        /// <summary>
        /// POST: <c>/Product/Edit</c>. Diffs the submitted form against the live data for all four child entities (creating any that don't
        /// exist yet) and either applies or queues each detected change via <see cref="ApplyOrQueue"/>. Core fields (name/brand/image/weight/
        /// unit) always route normally; ingredients/nutrition/seals bypass the pending queue entirely if this is the first time that section
        /// has ever been saved for this product (nothing established yet to protect). Sets <c>TempData["EditMessage"]</c> depending on whether
        /// anything actually went to pending review, then redirects to Info.
        /// </summary>
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

                // Product name/brand/image/weight/unit always already have real values (Add Product guarantees Products+Foodstuffs
                // exist), so these always go through the normal role-based routing - never treated as "brand new".

                // El nombre/marca/imagen/peso/unidad del producto siempre tienen ya valores reales (Agregar Producto
                // garantiza que Products+Foodstuffs existan), así que estos siempre pasan por el enrutamiento normal
                // basado en rol - nunca se tratan como "recién creados".
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

        /// <summary>
        /// GET: <c>/Product/History?upc=...</c>. Admin only. Lists every <see cref="EditHistoryRecord"/> for this product (any status),
        /// newest first, with Approve/Deny actions on pending rows.
        /// </summary>
        public ActionResult History(decimal upc)
        {
            if (!SessionHelper.IsAdmin)
            {
                return RedirectToAction("Info", new { upc });
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

        /// <summary>
        /// POST: <c>/Product/ApproveEdit</c>. Admin only. Actually applies the pending change to the live product data (via
        /// <see cref="ProductEditHelper.ApplyChange"/> - the same method a direct admin save uses), creating any missing child entity first,
        /// then marks the record <c>"approved"</c>. Does nothing if the record isn't found or isn't still pending.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApproveEdit(int editId, decimal upc)
        {
            if (!SessionHelper.IsAdmin)
            {
                return RedirectToAction("Info", new { upc });
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

            return RedirectToAction("History", new { upc });
        }

        /// <summary>
        /// POST: <c>/Product/DenyEdit</c>. Admin only. Marks a pending record <c>"denied"</c> without touching any product data. Does nothing if
        /// the record isn't found or isn't still pending.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DenyEdit(int editId, decimal upc)
        {
            if (!SessionHelper.IsAdmin)
            {
                return RedirectToAction("Info", new { upc });
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

            return RedirectToAction("History", new { upc });
        }

        // POST: /Product/Delete (solo admin)
        // Elimina la fila de Products; la base de datos hace cascada en la eliminación (On Delete Cascade) de todas las tablas hijas
        // automáticamente, así que EF solo elimina esa fila. También llama a SessionHelper.ForgetProduct para que el producto eliminado
        // desaparezca de Búsquedas Recientes / navegación de encabezado inmediatamente.

        /// <summary>
        /// POST: <c>/Product/Delete</c>. Admin only. Removes the <c>Products</c> row - the database cascades the delete to every
        /// child table automatically, so EF only removes the one row. Also calls <see cref="SessionHelper.ForgetProduct"/> so the deleted
        /// product disappears from Recent Searches/header nav immediately.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(decimal upc)
        {
            if (!SessionHelper.IsAdmin)
            {
                return RedirectToAction("Info", new { upc });
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

        // Ayudantes

        // Aplica cada cambio directamente si bypassPending es true (una fila recién creada que se está completando por
        // primera vez) O el usuario actual es admin; de lo contrario, pone en cola cada cambio como un registro
        // pendiente de EditHistory en lugar de tocar los datos en vivo. Devuelve cuántos cambios se pusieron en cola como pendientes.

        /// <summary>
        /// For each detected <paramref name="changes"/>: applies it directly if <paramref name="bypassPending"/> is <c>true</c> (a brand-new
        /// row being filled in for the first time) or the current user is admin; otherwise queues it as a pending <see cref="EditHistoryRecord"/>
        /// instead of touching the live data.
        /// </summary>
        /// <returns>How many changes were queued as pending (0 if everything applied directly).</returns>
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

        /// <summary>Adds <paramref name="entity"/> to the context and returns it - lets a <c>?? AddNew(...)</c> "load or create" one-liner read cleanly.</summary>
        private T AddNew<T>(NorteMartContext db, T entity) where T : class
        {
            db.Set<T>().Add(entity);
            return entity;
        }

        /// <summary>
        /// Builds the My People danger/caution/safe status for every person the current account owns, grouped by <c>GroupName</c>, 
        /// against one product's Contains/May-Contain allergen lists.
        /// </summary>
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