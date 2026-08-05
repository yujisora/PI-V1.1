using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace PIV11.Models
{
    /// <summary>
    /// Maps to <c>dbo.EditHistory</c> - one row per proposed change to a product. <see cref="Status"/> starts <c>"pending"</c> and becomes
    /// <c>"approved"</c> or <c>"denied"</c> when admin acts on it (see
    /// <see cref="Controllers.ProductController.ApproveEdit"/> /
    /// <see cref="Controllers.ProductController.DenyEdit"/>). See
    /// <see cref="Infrastructure.FieldChangeInfo"/> for the
    /// <see cref="FieldChanged"/>/<see cref="NewValue"/> format.
    /// </summary>
    [Table("EditHistory")]
    public class EditHistoryRecord
    {
        /// <summary>
        /// The primary key of the edit history record. This is an auto-incrementing integer that 
        /// uniquely identifies each proposed change to a product.
        /// This is the primary key of the edit history record and is used to reference specific edits in the system.
        /// </summary>
        [Key]
        public int EditID { get; set; }
        /// <summary>
        /// The product this change belongs to. Not modeled as an EF navigation property (kept as a plain <c>decimal</c>) so this class
        /// stays simple to query on its own - it's still a real foreign key at the database level.
        /// </summary>
        public decimal UPC { get; set; }
        [Required]
        [StringLength(50)]
        /// <summary>The username of the user who made the edit.</summary>
        public string EditedByUser { get; set; }
        [Required]
        [StringLength(100)]
        /// <summary>The name of the field that was changed, in the format <c>ClassName.PropertyName</c>.</summary>
        public string FieldChanged { get; set; }
        [Required]
        [StringLength(500)]
        /// <summary>The new value that was proposed for the field.</summary>
        public string NewValue { get; set; }
        /// <summary>Always one of <c>"pending"</c>, <c>"approved"</c>, or <c>"denied"</c>. </summary>
        public DateTime DateEdited { get; set; }
        /// <summary>The status of the edit request.</summary>
        [Required]
        [StringLength(10)]
        public string Status { get; set; }
    }
}