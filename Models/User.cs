using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PIV11.Models
{
    /* =====================================================================
       User
       Maps to dbo.Users. The username (UserID) is just a login
       credential now - Role is what actually determines permissions
       ("admin" / "employee" / "shopper"), decoupled so any number of
       accounts can share a role. DisplayName is the person's real name
       (shown in the header as "DisplayName | Role"); MemberID is an
       optional Worker ID (employee) or Shopper ID (shopper) - a second
       identifier distinct from UserID, since two different people could
       share the same DisplayName.
       ===================================================================== */
    [Table("Users")]
    public class User
    {
        [Key]
        [StringLength(50)]
        public string UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string Pass { get; set; }

        // Always one of: "admin", "employee", "shopper".
        [Required]
        [StringLength(20)]
        public string Role { get; set; }

        [StringLength(100)]
        public string DisplayName { get; set; }

        [StringLength(50)]
        public string MemberID { get; set; }
    }
}