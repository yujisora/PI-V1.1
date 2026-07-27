using System.Collections.Generic;

namespace PIV11.Models.ViewModels
{
    /* =====================================================================
       PersonListItem / PersonGroupViewModel
       Backs the My People list (Views/People/Index.cshtml) - one row per
       person, grouped by GroupName exactly like ProductController's
       "My People" danger/caution/safe panel already does, but here for
       full CRUD management instead of a status check against one product.
       ===================================================================== */
    public class PersonListItem
    {
        public int PersonID { get; set; }
        public string Name { get; set; }
        public List<string> Allergens { get; set; }

        public PersonListItem()
        {
            Allergens = new List<string>();
        }
    }

    public class PersonGroupViewModel
    {
        public string GroupName { get; set; }
        public List<PersonListItem> Members { get; set; }

        public PersonGroupViewModel()
        {
            Members = new List<PersonListItem>();
        }
    }

    /* =====================================================================
       MemberFormViewModel
       Backs the single shared Add/Edit Person form
       (Views/People/MemberForm.cshtml). PersonID is 0 for a brand-new
       person, or the real ID when editing an existing one - the
       controller uses that to decide whether to insert or update.
       SelectedAllergens binds directly from a group of identically-named
       checkboxes (standard ASP.NET MVC list-binding), and reuses
       AllergenHelper.GetPersonAllergens/ApplyToPerson - the same helpers
       already used elsewhere for a Person's allergy list, rather than
       introducing a new representation just for this form.
       ===================================================================== */
    public class MemberFormViewModel
    {
        public int PersonID { get; set; }
        public string Name { get; set; }
        public string GroupName { get; set; }
        public List<string> SelectedAllergens { get; set; }

        public MemberFormViewModel()
        {
            SelectedAllergens = new List<string>();
        }
    }
}