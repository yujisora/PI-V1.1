using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PIV11.Models
{
    /* =====================================================================
       Product
       Maps to dbo.Products. This is the "core" product record - every
       other product-related table (Foodstuffs, NutritionData,
       IngredientsAllergens, HealthAlert) shares the same UPC as its
       own primary key, so each of those is a one-to-one child of Product.
           // Product
           // Se mapea a dbo.Products. Este es el registro "principal"
           // del producto - cada otra tabla relacionada con el producto
           // (Foodstuffs, NutritionData, IngredientsAllergens,
           // HealthAlert) comparte el mismo UPC como su propia llave
           // primaria, así que cada una de ellas es una hija uno-a-uno
           // de Product.
       ===================================================================== */
    /// <summary>
    /// Maps to <c>dbo.Products</c> - the core product record. Every other
    /// product-related table shares this same <see cref="UPC"/> as its own
    /// primary key, making each a one-to-one child of <c>Product</c>.
    /// </summary>
    [Table("Products")]
    public class Product
    {
        // UPC is a 13-digit barcode number, stored as DECIMAL(13,0) in SQL.
        // We use decimal here too so large 13-digit numbers fit safely
        // (a plain int/long could overflow for the largest EAN-13/UPC values).
        // UPC es un número de código de barras de 13 dígitos,
        // almacenado como DECIMAL(13,0) en SQL. Aquí también usamos
        // decimal para que los números grandes de 13 dígitos quepan
        // de forma segura (un simple int/long podría desbordarse
        // para los valores más grandes de EAN-13/UPC).
        /// <summary>The 13-digit barcode, and this table's primary key. Stored as <c>decimal</c> so the largest EAN-13/UPC values fit safely without overflow.</summary>
        [Key]
        public decimal UPC { get; set; }

        [Required]
        [StringLength(250)]
        public string ProductName { get; set; }

        [StringLength(50)]
        public string Brand { get; set; }

        // Added by 01_Database_Additions.sql - path or URL to the product image.
        // Agregado por 01_Database_Additions.sql - ruta o URL a la
        // imagen del producto.
        /// <summary>Path or URL to the product image; there is no real file upload, this is just a text field.</summary>
        [StringLength(500)]
        public string ImageUrl { get; set; }

        // Navigation properties to the one-to-one child tables.
        // "virtual" enables EF lazy-loading.
        // Propiedades de navegación hacia las tablas hijas
        // uno-a-uno. "virtual" habilita la carga diferida (lazy
        // loading) de EF.
        /// <summary>Navigation property to this product's <see cref="Foodstuff"/> row (<c>virtual</c> for EF lazy-loading).</summary>
        public virtual Foodstuff Foodstuff { get; set; }
        /// <summary>Navigation property to this product's <see cref="NutritionData"/> row.</summary>
        public virtual NutritionData NutritionData { get; set; }
        /// <summary>Navigation property to this product's <see cref="IngredientsAllergen"/> row.</summary>
        public virtual IngredientsAllergen IngredientsAllergens { get; set; }
        /// <summary>Navigation property to this product's <see cref="HealthAlert"/> row.</summary>
        public virtual HealthAlert HealthAlert { get; set; }
    }

    /* =====================================================================
       Foodstuff
       Maps to dbo.Foodstuffs. Net content (weight/volume) + unit.
           // Foodstuff
           // Se mapea a dbo.Foodstuffs. Contenido neto (peso/volumen) +
           // unidad.
       ===================================================================== */
    /// <summary>Maps to <c>dbo.Foodstuffs</c> - a product's net content (weight/volume) and unit.</summary>
    [Table("Foodstuffs")]
    public class Foodstuff
    {
        // Shared primary key: same value as Products.UPC, and also the FK.
        // Llave primaria compartida: mismo valor que Products.UPC, y
        // también la FK.
        /// <summary>Shared primary key - same value as <see cref="Product.UPC"/>, and also the foreign key back to it.</summary>
        [Key]
        [ForeignKey("Product")]
        public decimal UPC { get; set; }

        [StringLength(100)]
        public string NetVolume { get; set; }

        [StringLength(5)]
        public string UnitMeasurement { get; set; }

        public virtual Product Product { get; set; }
    }

    /* =====================================================================
       NutritionData
       Maps to dbo.NutritionData. The 10 core nutrition facts shown in
       the Nutrition Facts table on the Product Info / Edit screens.
       All values are stored as whole numbers (per 100 g/ml), matching
       the mockup's rounded integer inputs.
           // NutritionData
           // Se mapea a dbo.NutritionData. Los 10 datos nutricionales
           // principales que se muestran en la tabla de Datos
           // Nutricionales en las pantallas de Información del Producto
           // / Edición. Todos los valores se almacenan como números
           // enteros (por 100 g/ml), coincidiendo con las entradas de
           // enteros redondeados del mockup.
       ===================================================================== */
    /// <summary>
    /// Maps to <c>dbo.NutritionData</c> - the 10 core nutrition facts, all
    /// stored as nullable whole numbers per 100 g/ml. See
    /// <see cref="Infrastructure.ProductEditHelper.NutritionFields"/> for
    /// display labels/units/order.
    /// </summary>
    [Table("NutritionData")]
    public class NutritionData
    {
        [Key]
        [ForeignKey("Product")]
        public decimal UPC { get; set; }

        public int? Calories { get; set; }
        public int? Proteins { get; set; }
        public int? Fats { get; set; }
        public int? SaturatedFats { get; set; }
        public int? TransFats { get; set; }
        public int? Carbs { get; set; }
        public int? Sugars { get; set; }
        public int? AddedSugars { get; set; }
        public int? Fiber { get; set; }
        public int? Sodium { get; set; }

        public virtual Product Product { get; set; }
    }

    /// <summary>
    /// Maps to <c>dbo.IngredientsAllergens</c>. Each <c>HasX</c> column is a
    /// tri-state <c>VARCHAR(1)</c>: <c>NULL</c>/<c>"0"</c> = does not contain,
    /// <c>"1"</c> = contains, <c>"M"</c> = may contain (cross-contamination).
    /// See <see cref="Infrastructure.AllergenHelper"/> for the code that
    /// converts these into the display lists the views use.
    /// </summary>
    [Table("IngredientsAllergens")]
    public class IngredientsAllergen
    {
        [Key]
        [ForeignKey("Product")]
        public decimal UPC { get; set; }

        [StringLength(500)]
        public string Ingredients { get; set; }

        [StringLength(1)] public string HasCrustaceans { get; set; }
        [StringLength(1)] public string HasEgg { get; set; }
        [StringLength(1)] public string HasFish { get; set; }
        [StringLength(1)] public string HasMilk { get; set; }
        [StringLength(1)] public string HasPeanut { get; set; }
        [StringLength(1)] public string HasNuts { get; set; }
        [StringLength(1)] public string HasSoy { get; set; }
        [StringLength(1)] public string HasGluten { get; set; }
        [StringLength(1)] public string HasSulfites { get; set; }
        [StringLength(1)] public string HasPHE { get; set; }

        public virtual Product Product { get; set; }
    }
    /// <summary>
    /// Maps to <c>dbo.HealthAlert</c> - the 7 warning seal flags. See
    /// <see cref="Infrastructure.ProductEditHelper.SealDefinitions"/> for
    /// display labels/order.
    /// </summary>
    [Table("HealthAlert")]
    public class HealthAlert
    {
        [Key]
        [ForeignKey("Product")]
        public decimal UPC { get; set; }

        public bool ExCalories { get; set; }
        public bool ExSatFat { get; set; }
        public bool ExTrFat { get; set; }
        public bool ExSugars { get; set; }
        public bool ExSod { get; set; }
        public bool HasSweeteners { get; set; }
        public bool HasCaffeine { get; set; }

        public virtual Product Product { get; set; }
    }
}