using System.Collections.Generic;

namespace PIV11.Models.ViewModels
{
    /// <summary>
    /// Represents a single person in a list, including their ID, name, and allergens.
    /// </summary>
public class PersonListItem
    {
        /// <summary>The unique identifier for the person, corresponding to the primary key in the People table.</summary>
        public int PersonID { get; set; }
        /// <summary>The name of the person. This is a required field and should not be null or empty </summary>
        public string Name { get; set; }
        /// <summary>
        /// The list of allergens associated with this person. 
        /// This list is derived from the boolean allergen fields in the Person model and is never null.
        /// </summary>
        public List<string> Allergens { get; set; }

        /// <summary>a default constructor that initializes the Allergens list to an empty list, ensuring it is never null.</summary>
        public PersonListItem()
        {
            Allergens = new List<string>();
        }
    }

    /// <summary>Represents a group of people, including the group name and its members.</summary>
    public class PersonGroupViewModel
    {
        /// <summary>Label for the group, e.g., "Family" or "Friends". If null or empty, the group is considered "Ungrouped".</summary>
        public string GroupName { get; set; }
        /// <summary>List of members in this group, each represented by a <see cref="PersonListItem"/>. 
        /// If the group is empty, this list will be empty.
        /// </summary>
        public List<PersonListItem> Members { get; set; }

        /// <summary>a default constructor that initializes the Members list to an empty list, ensuring it is never null.</summary>
        public PersonGroupViewModel()
        {
            Members = new List<PersonListItem>();
        }
    }

    /// <summary>
    /// This view model is used for the Add/Edit Person form, containing the person's ID, name, group name, and a list of selected allergens.
    /// It is used to bind form data to the controller and vice versa, allowing for both adding new people and editing existing ones.
    /// It includes a list of selected allergens that is bound to a group of checkboxes in the form, and uses helper methods to convert
    /// between the boolean allergen fields in the Person model and this list representation.
    /// </summary>
    public class MemberFormViewModel
    {
        /// <summary>The unique identifier for the person, corresponding to the primary key in the People table.</summary>
        public int PersonID { get; set; }
        /// <summary>The name of the person. This is a required field and should not be null or empty </summary>
        public string Name { get; set; }
        /// <summary>The name of the group this person belongs to. If null or empty, the person is considered "Ungrouped".</summary>
        public string GroupName { get; set; }
        /// <summary>The list of allergens selected for this person. This list is derived from the boolean allergen fields 
        /// in the Person model and is never null.
        /// </summary>
        public List<string> SelectedAllergens { get; set; }

        /// <summary>
        /// The default constructor initializes the SelectedAllergens list to an empty list, ensuring it is never null.
        /// </summary>
        public MemberFormViewModel()
        {
            SelectedAllergens = new List<string>();
        }
    }
}