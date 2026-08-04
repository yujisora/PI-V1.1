using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PIV11.Models
{
    // User: Maps to dbo.Users. (Mapea a dbo.Users.)

    // UserID is only used for login, so it must be unique and is the primary key.
    // Role is one of three fixed values: "admin", "employee", or "shopper"; this value defines what the user can do in the system.
        // UserID solo es para iniciar sesión, por lo que debe ser único y es la clave primaria.
        // Role es uno de tres valores fijos: "admin", "employee" o "shopper"; este valor define lo que el usuario puede hacer en el sistema.

    // DisplayName is optional and is used for a more friendly display in the UI.
        // DisplayName es opcional y se utiliza para una visualización más amigable en la interfaz de usuario.

    // MemberID is optional and is only used for shoppers to link to their membership record.
        // MemberID es opcional y solo se utiliza para los compradores para vincularlos a su registro de membresía.

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
            // Siempre será uno de: "admin", "employee", "shopper".
        [Required]
        [StringLength(20)]
        public string Role { get; set; }

        [StringLength(100)]
        public string DisplayName { get; set; }

        [StringLength(50)]
        public string MemberID { get; set; }
    }
}