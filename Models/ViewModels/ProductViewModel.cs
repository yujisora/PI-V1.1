using System.Collections.Generic;

namespace PIV11.Models.ViewModels
{
    /* =====================================================================
       NutritionFactDisplay
       One row in the Nutrition Facts table (Product Info screen).
       ===================================================================== */
    public class NutritionFactDisplay
    {
        public string Label { get; set; }
        public int? Value { get; set; }
        public string Unit { get; set; }
        public bool Indented { get; set; }
        public int? ContainerValue { get; set; }
    }

    /* =====================================================================
       SealDisplay
       One warning seal badge shown on Product Info (only the active ones)
       or one checkbox row on the Edit screen (all 8, regardless of state).
       ===================================================================== */
    public class SealDisplay
    {
        public string Key { get; set; }         // matches HealthAlert column name
        public string Label { get; set; }        // may contain \n for a line break
        public string SubLabel { get; set; }      // small print under the label, rects only
        public bool IsOctagon { get; set; }        // octagon ("Excess X") vs rectangle (caffeine/sweeteners)
        public bool IsActive { get; set; }          // current on/off state
    }

    /* =====================================================================
       PersonStatus / PeopleGroupStatus
       "My People" panel on Product Info (user role only) - one row per
       person, grouped by GroupName, with a computed danger/caution/safe
       status against the current product's allergens.
       ===================================================================== */
    public class PersonStatus
    {
        public string Name { get; set; }
        public string Status { get; set; } // "danger" | "caution" | "safe"
    }

    public class PeopleGroupStatus
    {
        public string GroupName { get; set; }
        public List<PersonStatus> Members { get; set; }
        public int AffectedCount { get; set; }

        public PeopleGroupStatus()
        {
            Members = new List<PersonStatus>();
        }
    }

    /* =====================================================================
       ProductInfoViewModel
       Everything Views/Product/Info.cshtml needs.
       ===================================================================== */
    public class ProductInfoViewModel
    {
        public decimal UPC { get; set; }
        public string ProductName { get; set; }
        public string Brand { get; set; }
        public string NetVolume { get; set; }
        public string UnitMeasurement { get; set; }
        public string ImageUrl { get; set; }
        public string Ingredients { get; set; }
        public bool HasContainerToggle { get; set; }
        public string ContainerAmountLabel { get; set; }

        public List<string> AllergensContains { get; set; }
        public List<string> AllergensMayContain { get; set; }
        public List<NutritionFactDisplay> NutritionFacts { get; set; }
        public List<SealDisplay> ActiveSeals { get; set; }

        // Only populated when the viewer is logged in as "user".
        public List<PeopleGroupStatus> MyPeopleGroups { get; set; }

        // Only meaningful for admin - shows a badge on the Edit History button.
        public int PendingEditCount { get; set; }

        public ProductInfoViewModel()
        {
            AllergensContains = new List<string>();
            AllergensMayContain = new List<string>();
            NutritionFacts = new List<NutritionFactDisplay>();
            ActiveSeals = new List<SealDisplay>();
            MyPeopleGroups = new List<PeopleGroupStatus>();
        }
    }

    /* =====================================================================
       EditProductViewModel
       Backs the Edit screen form (Views/Product/Edit.cshtml). All values
       are strings/bools straight from form fields - parsing/validation
       happens in the controller and ProductEditHelper, matching the
       pattern already used by AddProductViewModel.
       ===================================================================== */
    public class EditProductViewModel
    {
        // Read-only on the Edit screen (see project notes on why UPC can't
        // be changed after a product is created) - still included here so
        // the view can display it without a second lookup.
        public decimal UPC { get; set; }

        public string ProductName { get; set; }
        public string Brand { get; set; }
        public string ImageUrl { get; set; }
        public string Weight { get; set; }
        public string Unit { get; set; }
        public string Ingredients { get; set; }

        // Two checkboxes per allergen (Contains_<Key> / MayContain_<Key>,
        // matching AllergenHelper.Keys) instead of a single tri-state
        // dropdown - the Edit form shows these as two checklist dropdowns
        // ("Allergens (Contains)" / "Allergens (May Contain)"). Only one
        // should ever be true for a given allergen - the Edit screen's JS
        // enforces that by disabling the May-Contain checkbox once its
        // Contains counterpart is checked - but GetAllergenValue below
        // still resolves it safely even if that were somehow bypassed
        // (Contains wins).
        public bool Contains_Crustaceans { get; set; }
        public bool Contains_Egg { get; set; }
        public bool Contains_Fish { get; set; }
        public bool Contains_Milk { get; set; }
        public bool Contains_Peanut { get; set; }
        public bool Contains_Nuts { get; set; }
        public bool Contains_Soy { get; set; }
        public bool Contains_Gluten { get; set; }
        public bool Contains_Sulfites { get; set; }
        public bool Contains_PHE { get; set; }

        public bool MayContain_Crustaceans { get; set; }
        public bool MayContain_Egg { get; set; }
        public bool MayContain_Fish { get; set; }
        public bool MayContain_Milk { get; set; }
        public bool MayContain_Peanut { get; set; }
        public bool MayContain_Nuts { get; set; }
        public bool MayContain_Soy { get; set; }
        public bool MayContain_Gluten { get; set; }
        public bool MayContain_Sulfites { get; set; }
        public bool MayContain_PHE { get; set; }

        // Nutrition facts - strings so an empty field is possible; parsed
        // to int? in ProductEditHelper.
        public string Calories { get; set; }
        public string Proteins { get; set; }
        public string Fats { get; set; }
        public string SaturatedFats { get; set; }
        public string TransFats { get; set; }
        public string Carbs { get; set; }
        public string Sugars { get; set; }
        public string AddedSugars { get; set; }
        public string Fiber { get; set; }
        public string Sodium { get; set; }

        // Warning seals - plain checkboxes.
        public bool ExCalories { get; set; }
        public bool ExSatFat { get; set; }
        public bool ExTrFat { get; set; }
        public bool ExSugars { get; set; }
        public bool ExSod { get; set; }
        public bool HasSweeteners { get; set; }
        public bool HasCaffeine { get; set; }

        // Reads the Contains_<Key> / MayContain_<Key> checkbox matching a
        // given key - used by the view to render each checkbox's initial
        // checked state.
        public bool GetContains(string key)
        {
            switch (key)
            {
                case "Crustaceans": return Contains_Crustaceans;
                case "Egg": return Contains_Egg;
                case "Fish": return Contains_Fish;
                case "Milk": return Contains_Milk;
                case "Peanut": return Contains_Peanut;
                case "Nuts": return Contains_Nuts;
                case "Soy": return Contains_Soy;
                case "Gluten": return Contains_Gluten;
                case "Sulfites": return Contains_Sulfites;
                case "PHE": return Contains_PHE;
                default: return false;
            }
        }

        public bool GetMayContain(string key)
        {
            switch (key)
            {
                case "Crustaceans": return MayContain_Crustaceans;
                case "Egg": return MayContain_Egg;
                case "Fish": return MayContain_Fish;
                case "Milk": return MayContain_Milk;
                case "Peanut": return MayContain_Peanut;
                case "Nuts": return MayContain_Nuts;
                case "Soy": return MayContain_Soy;
                case "Gluten": return MayContain_Gluten;
                case "Sulfites": return MayContain_Sulfites;
                case "PHE": return MayContain_PHE;
                default: return false;
            }
        }

        // Resolves the two checkboxes down to the single tri-state value
        // ("0"/"1"/"M") ProductEditHelper works with - Contains wins if
        // both were somehow set. Same public signature as before this
        // change, so ProductEditHelper/ProductController didn't need any
        // updates.
        public string GetAllergenValue(string key)
        {
            if (GetContains(key)) return "1";
            if (GetMayContain(key)) return "M";
            return "0";
        }

        // Used when populating the Edit form (GET) from the current
        // tri-state database value - sets whichever of the two checkboxes
        // matches.
        public void SetAllergenValue(string key, string value)
        {
            switch (key)
            {
                case "Crustaceans": Contains_Crustaceans = value == "1"; MayContain_Crustaceans = value == "M"; break;
                case "Egg": Contains_Egg = value == "1"; MayContain_Egg = value == "M"; break;
                case "Fish": Contains_Fish = value == "1"; MayContain_Fish = value == "M"; break;
                case "Milk": Contains_Milk = value == "1"; MayContain_Milk = value == "M"; break;
                case "Peanut": Contains_Peanut = value == "1"; MayContain_Peanut = value == "M"; break;
                case "Nuts": Contains_Nuts = value == "1"; MayContain_Nuts = value == "M"; break;
                case "Soy": Contains_Soy = value == "1"; MayContain_Soy = value == "M"; break;
                case "Gluten": Contains_Gluten = value == "1"; MayContain_Gluten = value == "M"; break;
                case "Sulfites": Contains_Sulfites = value == "1"; MayContain_Sulfites = value == "M"; break;
                case "PHE": Contains_PHE = value == "1"; MayContain_PHE = value == "M"; break;
            }
        }

        // Used when populating the Edit form (GET) from the current
        // NutritionData row - the inverse of the plain string properties
        // the form posts back.
        public void SetNutritionValue(string column, int? value)
        {
            string text = value.HasValue ? value.Value.ToString() : "";
            switch (column)
            {
                case "Calories": Calories = text; break;
                case "Proteins": Proteins = text; break;
                case "Fats": Fats = text; break;
                case "SaturatedFats": SaturatedFats = text; break;
                case "TransFats": TransFats = text; break;
                case "Carbs": Carbs = text; break;
                case "Sugars": Sugars = text; break;
                case "AddedSugars": AddedSugars = text; break;
                case "Fiber": Fiber = text; break;
                case "Sodium": Sodium = text; break;
            }
        }

        // Used when populating the Edit form (GET) from the current
        // HealthAlert row.
        public void SetSealValue(string key, bool value)
        {
            switch (key)
            {
                case "ExCalories": ExCalories = value; break;
                case "ExSatFat": ExSatFat = value; break;
                case "ExTrFat": ExTrFat = value; break;
                case "ExSugars": ExSugars = value; break;
                case "ExSod": ExSod = value; break;
                case "HasSweeteners": HasSweeteners = value; break;
                case "HasCaffeine": HasCaffeine = value; break;
            }
        }
    }

    /* =====================================================================
       EditHistoryViewModel
       Backs the Edit History page (Views/Product/History.cshtml).
       ===================================================================== */
    public class EditHistoryViewModel
    {
        public decimal UPC { get; set; }
        public string ProductName { get; set; }
        public List<EditHistoryRecord> Records { get; set; }

        public EditHistoryViewModel()
        {
            Records = new List<EditHistoryRecord>();
        }
    }
}