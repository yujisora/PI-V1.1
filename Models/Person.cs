using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PIV11.Models
{
    /* =====================================================================
       Person
       Maps to dbo.People. Represents one member of a "My People" group
       (e.g. "Marie" in the "Family" group). GroupName is a plain text
       field - there is no separate Groups table, so a group only exists
       for as long as at least one Person row uses that GroupName.
       OwnerUsername ties this record to whichever account ("user" or
       "admin") created it, added by 01_Database_Additions.sql.
       ===================================================================== */
    [Table("People")]
    public class Person
    {
        [Key]
        public int PersonID { get; set; }

        [Required]
        [StringLength(100)]
        public string NamePerson { get; set; }

        [StringLength(100)]
        public string GroupName { get; set; }

        [StringLength(50)]
        public string OwnerUsername { get; set; }

        // One bool column per allergen - true means this person is
        // allergic to that specific allergen.
        public bool AllergicToCrustaceans { get; set; }
        public bool AllergicToEgg { get; set; }
        public bool AllergicToFish { get; set; }
        public bool AllergicToMilk { get; set; }
        public bool AllergicToPeanut { get; set; }
        public bool AllergicToNuts { get; set; }
        public bool AllergicToSoy { get; set; }
        public bool AllergicToGluten { get; set; }
        public bool AllergicToSulfites { get; set; }
        public bool AllergicToPHE { get; set; }
    }
}
