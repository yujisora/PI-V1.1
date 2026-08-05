using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace PIV11.Models
{
    /// <summary>
    /// Maps to <c>dbo.People</c> - one member of a My People group (e.g. "Marie" in the "Family" group). <see cref="GroupName"/> is free text;
    /// there is no separate Groups table, so a group exists only as long as at least one <c>Person</c> row uses that name.
    /// <see cref="OwnerUsername"/> scopes every record to the account that created it.
    /// </summary>
    [Table("People")]
    public class Person
    {
        /// <summary>
        /// PersonID is the primary key for the People table and is an auto-incrementing integer.
        /// </summary>
        [Key]
        public int PersonID { get; set; }
        [Required]
        [StringLength(100)]
        /// <summary>
        /// NamePerson is a required field with a maximum length of 100 characters, representing the name of the person. 
        /// </summary>
        public string NamePerson { get; set; }
        [StringLength(100)]
        /// <summary>
        /// GroupName is an optional field with a maximum length of 100 characters, representing the
        /// name of the group this person belongs to. If null or empty, the person is considered "Ungrouped".
        /// </summary>
        public string GroupName { get; set; }
        [StringLength(50)]
        /// <summary> 
        /// OwnerUsername is a required field with a maximum length of 50 characters, 
        /// representing the username of the account that owns this record.
        /// </summary>
        public string OwnerUsername { get; set; }

        /// <summary>
        /// One <c>bool</c> column per allergen below - <c>true</c> means this person is allergic to that specific allergen. See
        /// <see cref="Infrastructure.AllergenHelper.GetPersonAllergens"/> / <see cref="Infrastructure.AllergenHelper.ApplyToPerson"/> for the
        /// code that reads/writes these as a simple name list instead.
        /// </summary
        #region AllergenList
        /// <summary>
        /// Indicates whether the person is allergic to crustaceans.
        /// </summary>
        public bool AllergicToCrustaceans { get; set; }
        /// <summary>
        /// Indicates whether the person is allergic to eggs.
        /// </summary>
        public bool AllergicToEgg { get; set; }
        /// <summary>
        /// Indicates whether the person is allergic to fish.
        /// </summary>
        public bool AllergicToFish { get; set; }
        /// <summary>
        /// Indicates whether the person is allergic to milk.
        /// </summary>
        public bool AllergicToMilk { get; set; }
        /// <summary>
        /// Indicates whether the person is allergic to peanuts.
        /// </summary>
        public bool AllergicToPeanut { get; set; }
        /// <summary>
        /// Indicates whether the person is allergic to other nuts, tree nuts, pecans, or even sesame.
        /// </summary>
        public bool AllergicToNuts { get; set; }
        /// <summary>
        /// Indicates whether the person is allergic to soy.
        /// </summary>
        public bool AllergicToSoy { get; set; }
        /// <summary>
        /// Indicates whether the person is allergic to wheat or gluten.
        /// </summary>
        public bool AllergicToGluten { get; set; }
        /// <summary>
        /// Indicates whether the person is allergic/sensitive to sulfites.
        /// </summary>
        public bool AllergicToSulfites { get; set; }
        /// <summary>
        /// Indicates whether the person is allergic to phenylalanine (PHE), which is relevant for those with phenylketonuria (PKU).
        /// </summary>
        public bool AllergicToPHE { get; set; }
        #endregion
    }
}