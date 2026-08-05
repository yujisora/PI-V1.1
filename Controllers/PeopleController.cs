using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using PIV11.Infrastructure;
using PIV11.Models;
using PIV11.Models.ViewModels;

namespace PIV11.Controllers
{
    /// <summary>
    /// PeopleController handles the "My People" feature, allowing users to manage groups and members with their own allergen profiles.
    /// It is scoped per logged-in account (<see cref="People.OwnerUsername"/>) and is available to both "user" and "shopper" roles 
    /// (<see cref="SessionHelper.CanAccessMyPeople"/>). Admins do not have a personal People list.
    /// </summary>
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
        /// <summary>
        /// MemberForm is used for both adding a new person and editing an existing one.
        /// The presence of the "<c>id</c>" parameter indicates an edit operation, while its absence indicates a new person addition. 
        /// The "<c>groupName</c>" parameter can be used to prefill the group name when adding a new person.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
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

        /// <summary>
        /// This action handles the submission of the MemberForm, either creating a new person or updating an existing one 
        /// based on the provided model.
        /// Model validation is performed, and the user is redirected back to the index page upon successful save.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        // POST: /People/MemberForm
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
        /// <summary>
        /// This action handles the deletion of a person from the "My People" list. It checks if the user has access to manage their people, 
        /// retrieves the person by ID and owner username, and removes them from the database if found.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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
        /// <summary>
        /// This private method retrieves a list of distinct group names associated with the current user's people.
        /// It queries the database for people owned by the current user.
        /// </summary>
        /// <returns>A list of distinct group names associated with the current user's people.</returns>
   
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