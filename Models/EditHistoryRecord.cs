using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PIV11.Models
{
    /* =====================================================================
       EditHistoryRecord
       Maps to the new dbo.EditHistory table (created by
       01_Database_Additions.sql). One row per proposed change to a
       product. Status starts as "pending" and becomes "approved" or
       "denied" when an admin acts on it from the Edit History panel.
       ===================================================================== */
    [Table("EditHistory")]
    public class EditHistoryRecord
    {
        [Key]
        public int EditID { get; set; }

        // Not marked [ForeignKey] as a navigation property here to keep
        // this class simple to query on its own; UPC is still a real FK
        // at the database level (see the SQL script).
        public decimal UPC { get; set; }

        [Required]
        [StringLength(50)]
        public string EditedByUser { get; set; }

        [Required]
        [StringLength(100)]
        public string FieldChanged { get; set; }

        [Required]
        [StringLength(500)]
        public string NewValue { get; set; }

        public DateTime DateEdited { get; set; }

        // Always one of: "pending", "approved", "denied"
        [Required]
        [StringLength(10)]
        public string Status { get; set; }
    }
}
