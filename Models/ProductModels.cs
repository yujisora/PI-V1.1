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
       ===================================================================== */
    [Table("Products")]
    public class Product
    {
        // UPC is a 13-digit barcode number, stored as DECIMAL(13,0) in SQL.
        // We use decimal here too so large 13-digit numbers fit safely
        // (a plain int/long could overflow for the largest EAN-13/UPC values).
        [Key]
        public decimal UPC { get; set; }

        [Required]
        [StringLength(250)]
        public string ProductName { get; set; }

        [StringLength(50)]
        public string Brand { get; set; }

        // Added by 01_Database_Additions.sql - path or URL to the product image.
        [StringLength(500)]
        public string ImageUrl { get; set; }

        // Navigation properties to the one-to-one child tables.
        // "virtual" enables EF lazy-loading.
        public virtual Foodstuff Foodstuff { get; set; }
        public virtual NutritionData NutritionData { get; set; }
        public virtual IngredientsAllergen IngredientsAllergens { get; set; }
        public virtual HealthAlert HealthAlert { get; set; }
    }

    /* =====================================================================
       Foodstuff
       Maps to dbo.Foodstuffs. Net content (weight/volume) + unit.
       ===================================================================== */
    [Table("Foodstuffs")]
    public class Foodstuff
    {
        // Shared primary key: same value as Products.UPC, and also the FK.
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
       ===================================================================== */
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

    /* =====================================================================
       IngredientsAllergen
       Maps to dbo.IngredientsAllergens. Each HasX column is a tri-state
       VARCHAR(1):
           NULL or "0"  -> does not contain
           "1"          -> contains
           "M"          -> may contain (cross-contamination warning)
       See PIV11.Infrastructure.AllergenHelper for the code that
       converts these values into the display lists the views use.
       ===================================================================== */
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

    /* =====================================================================
       HealthAlert
       Maps to dbo.HealthAlert. The warning seal checkboxes (Edit screen)
       and warning seal badges (Product Info screen).
       ===================================================================== */
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