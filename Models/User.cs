using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PIV11.Models
{

    /// <summary>
    /// User: Maps to <c>dbo.Users</c>. <see cref="UserID"/> is the primary key and is only used for login. 
    /// <see cref="Role"/> is one of three fixed values: <c>"admin"</c>, <c>"employee"</c>, or <c>"shopper"</c>; 
    /// this value defines what the user can do in the system. <see cref="DisplayName"/> is optional and is used for a more friendly 
    /// display in the UI. <see cref="MemberID"/> is optional and is only used for shoppers to link to their membership record.
    /// </summary>

    [Table("Users")]
    public class User
    {
        [Key]
        [StringLength(50)]
        public string UserID { get; set; }
        /// <summary>
        /// UserID is the primary key and is only used for login.
        /// It is a required field with a maximum length of 50 characters.
        /// It is distinct from the DisplayName, which is used for a more friendly display in the UI.
        /// </summary>

        [Required]
        [StringLength(50)]
        public string Pass { get; set; }

        /// <summary>
        /// Role is one of three fixed values: "admin", "employee", or "shopper".
        /// This value defines what the user can do in the system.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Role { get; set; }

        /// <summary>
        /// DisplayName is an optional field used for a more friendly display in the UI.
        /// </summary>
        [StringLength(100)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Displays the user's membership ID, which is only used for shoppers to link to their membership record.
        /// This field is optional and can be null for non-shopper users.
        /// </summary>
        [StringLength(50)]
        public string MemberID { get; set; }
    }
}