using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using PIV11.Infrastructure;
using PIV11.Models;
using PIV11.Models.ViewModels;

namespace PIV11.Controllers
{
    /* =====================================================================
       PeopleController
       "My People" - groups and members with their own allergen profiles,
       scoped per logged-in account (People.OwnerUsername). Available to
       both "user" and "shopper" roles (SessionHelper.CanAccessMyPeople) -
       admin does not have a personal People list, matching the original
       design.

       There's no separate Groups table - GroupName is just a text field
       on each Person row, so a group only exists for as long as at least
       one person uses that name. Renaming a whole group isn't a separate
       action; editing each member's Group field individually covers it.
       ===================================================================== */
    public class PeopleController : Controller
    {
        // GET: /People/Index
        public ActionResult Index()
        {
            if (!SessionHelper.CanAccessMyPeople)
            {
                return RedirectToAction("Index", "Home");
            }

            using (var db = new NorteMartContext())
            {
                var people = db.People
                    .Where(p => p.OwnerUsername == SessionHelper.CurrentUsername)
                    .OrderBy(p => p.GroupName)
                    .ThenBy(p => p.NamePerson)
                    .ToList();

                var groups = people
                    .GroupBy(p => string.IsNullOrEmpty(p.GroupName) ? "Ungrouped" : p.GroupName)
                    .Select(g => new PersonGroupViewModel
                    {
                        GroupName = g.Key,
                        Members = g.Select(p => new PersonListItem
                        {
                            PersonID = p.PersonID,
                            Name = p.NamePerson,
                            Allergens = AllergenHelper.GetPersonAllergens(p)
                        }).ToList()
                    })
                    .ToList();

                ViewBag.ActiveScreen = "My People";
                ViewBag.Title = "My People";
                return View(groups);
            }
        }

        // GET: /People/MemberForm            -> add a brand-new person
        // GET: /People/MemberForm?groupName=X -> add a person, group prefilled
        // GET: /People/MemberForm?id=X        -> edit an existing person
        public ActionResult MemberForm(int? id, string groupName)
        {
            if (!SessionHelper.CanAccessMyPeople)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new MemberFormViewModel();
            ViewBag.Title = "Add Person";
            ViewBag.IsNew = true;

            if (id.HasValue)
            {
                using (var db = new NorteMartContext())
                {
                    var person = db.People.FirstOrDefault(p =>
                        p.PersonID == id.Value && p.OwnerUsername == SessionHelper.CurrentUsername);

                    if (person == null)
                    {
                        return RedirectToAction("Index");
                    }

                    model.PersonID = person.PersonID;
                    model.Name = person.NamePerson;
                    model.GroupName = person.GroupName;
                    model.SelectedAllergens = AllergenHelper.GetPersonAllergens(person);
                }
                ViewBag.Title = "Edit Person";
                ViewBag.IsNew = false;
            }
            else
            {
                model.GroupName = groupName;
            }

            ViewBag.ActiveScreen = "My People";
            ViewBag.ExistingGroups = GetExistingGroupNames();
            return View(model);
        }

        // POST: /People/MemberForm
        // model.PersonID being 0 (default) vs. a real ID is what decides
        // whether this creates a new person or updates an existing one.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MemberForm(MemberFormViewModel model)
        {
            if (!SessionHelper.CanAccessMyPeople)
            {
                return RedirectToAction("Index", "Home");
            }

            bool isNew = model.PersonID <= 0;
            ViewBag.ActiveScreen = "My People";
            ViewBag.Title = isNew ? "Add Person" : "Edit Person";
            ViewBag.IsNew = isNew;

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("", "Name is required.");
                ViewBag.ExistingGroups = GetExistingGroupNames();
                return View(model);
            }

            using (var db = new NorteMartContext())
            {
                Person person;
                if (isNew)
                {
                    person = new Person { OwnerUsername = SessionHelper.CurrentUsername };
                    db.People.Add(person);
                }
                else
                {
                    person = db.People.FirstOrDefault(p =>
                        p.PersonID == model.PersonID && p.OwnerUsername == SessionHelper.CurrentUsername);
                    if (person == null)
                    {
                        return RedirectToAction("Index");
                    }
                }

                person.NamePerson = model.Name.Trim();
                person.GroupName = string.IsNullOrWhiteSpace(model.GroupName)
                    ? "Ungrouped"
                    : model.GroupName.Trim();

                AllergenHelper.ApplyToPerson(person, model.SelectedAllergens ?? new List<string>());

                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // POST: /People/DeleteMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteMember(int id)
        {
            if (!SessionHelper.CanAccessMyPeople)
            {
                return RedirectToAction("Index", "Home");
            }

            using (var db = new NorteMartContext())
            {
                var person = db.People.FirstOrDefault(p =>
                    p.PersonID == id && p.OwnerUsername == SessionHelper.CurrentUsername);
                if (person != null)
                {
                    db.People.Remove(person);
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Index");
        }
        // Existing group names for the current account, used to populate
        // the Group dropdown on the Add/Edit Person form.
        private List<string> GetExistingGroupNames()
        {
            using (var db = new NorteMartContext())
            {
                return db.People
                    .Where(p => p.OwnerUsername == SessionHelper.CurrentUsername && !string.IsNullOrEmpty(p.GroupName))
                    .Select(p => p.GroupName)
                    .Distinct()
                    .OrderBy(g => g)
                    .ToList();
            }
        }
    }
}