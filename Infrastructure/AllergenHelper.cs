using System.Collections.Generic;

namespace PIV11.Infrastructure
{
    /* =====================================================================
       AllergenHelper
       The database stores allergens as one column per allergen (e.g.
       HasMilk, HasNuts...) instead of a list. This helper converts back
       and forth between that column layout and the simple list-of-names
       the views use (matching the mockup's allergenList).

       Tri-state convention used by IngredientsAllergens.HasX columns:
           NULL or "0"  -> does not contain
           "1"          -> contains
           "M"          -> may contain
       =====================================================================*/
    public static class AllergenHelper
    {
        // The 10 allergens tracked by the app, in display order.
        // This order matches the mockup's allergenList exactly.
        public static readonly string[] AllNames =
        {
            "Crustacean", "Egg", "Fish", "Milk", "Peanut",
            "Nuts", "Soy", "Gluten", "Sulfites", "Aspartame (Phenylalanine)"
        };

        // Reads all 10 HasX columns off an IngredientsAllergen row and
        // splits them into two lists: definitely contains, and may contain.
        public static void SplitContainsAndMayContain(
            Models.IngredientsAllergen row,
            out List<string> contains,
            out List<string> mayContain)
        {
            contains = new List<string>();
            mayContain = new List<string>();
            if (row == null) return;

            AddIfSet(row.HasCrustaceans, "Crustacean", contains, mayContain);
            AddIfSet(row.HasEgg, "Egg", contains, mayContain);
            AddIfSet(row.HasFish, "Fish", contains, mayContain);
            AddIfSet(row.HasMilk, "Milk", contains, mayContain);
            AddIfSet(row.HasPeanut, "Peanut", contains, mayContain);
            AddIfSet(row.HasNuts, "Nuts", contains, mayContain);
            AddIfSet(row.HasSoy, "Soy", contains, mayContain);
            AddIfSet(row.HasGluten, "Gluten", contains, mayContain);
            AddIfSet(row.HasSulfites, "Sulfites", contains, mayContain);
            AddIfSet(row.HasPHE, "Aspartame (Phenylalanine)", contains, mayContain);
        }

        private static void AddIfSet(string value, string allergenName, List<string> contains, List<string> mayContain)
        {
            if (value == "1") contains.Add(allergenName);
            else if (value == "M") mayContain.Add(allergenName);
            // NULL or "0" (or anything else) -> not added to either list
        }

        // Given the two lists (contains / may contain), returns the
        // single tri-state character to store for one allergen name.
        public static string ToDbValue(string allergenName, List<string> contains, List<string> mayContain)
        {
            if (contains.Contains(allergenName)) return "1";
            if (mayContain.Contains(allergenName)) return "M";
            return "0";
        }

        // Applies a full set of Contains/MayContain lists onto an
        // IngredientsAllergen row's HasX columns (used when saving the
        // Edit screen).
        public static void ApplyToRow(Models.IngredientsAllergen row, List<string> contains, List<string> mayContain)
        {
            row.HasCrustaceans = ToDbValue("Crustacean", contains, mayContain);
            row.HasEgg = ToDbValue("Egg", contains, mayContain);
            row.HasFish = ToDbValue("Fish", contains, mayContain);
            row.HasMilk = ToDbValue("Milk", contains, mayContain);
            row.HasPeanut = ToDbValue("Peanut", contains, mayContain);
            row.HasNuts = ToDbValue("Nuts", contains, mayContain);
            row.HasSoy = ToDbValue("Soy", contains, mayContain);
            row.HasGluten = ToDbValue("Gluten", contains, mayContain);
            row.HasSulfites = ToDbValue("Sulfites", contains, mayContain);
            row.HasPHE = ToDbValue("Aspartame (Phenylalanine)", contains, mayContain);
        }

        // Reads a Person's 10 AllergicToX booleans into a simple name list
        // (used by the My People danger/caution/safe comparison logic).
        public static List<string> GetPersonAllergens(Models.Person person)
        {
            var list = new List<string>();
            if (person == null) return list;
            if (person.AllergicToCrustaceans) list.Add("Crustacean");
            if (person.AllergicToEgg) list.Add("Egg");
            if (person.AllergicToFish) list.Add("Fish");
            if (person.AllergicToMilk) list.Add("Milk");
            if (person.AllergicToPeanut) list.Add("Peanut");
            if (person.AllergicToNuts) list.Add("Nuts");
            if (person.AllergicToSoy) list.Add("Soy");
            if (person.AllergicToGluten) list.Add("Gluten");
            if (person.AllergicToSulfites) list.Add("Sulfites");
            if (person.AllergicToPHE) list.Add("Aspartame (Phenylalanine)");
            return list;
        }

        // Sets a Person's 10 AllergicToX booleans from a simple name list
        // (used when saving a member on the My People screen).
        public static void ApplyToPerson(Models.Person person, List<string> allergenNames)
        {
            person.AllergicToCrustaceans = allergenNames.Contains("Crustacean");
            person.AllergicToEgg = allergenNames.Contains("Egg");
            person.AllergicToFish = allergenNames.Contains("Fish");
            person.AllergicToMilk = allergenNames.Contains("Milk");
            person.AllergicToPeanut = allergenNames.Contains("Peanut");
            person.AllergicToNuts = allergenNames.Contains("Nuts");
            person.AllergicToSoy = allergenNames.Contains("Soy");
            person.AllergicToGluten = allergenNames.Contains("Gluten");
            person.AllergicToSulfites = allergenNames.Contains("Sulfites");
            person.AllergicToPHE = allergenNames.Contains("Aspartame (Phenylalanine)");
        }

        /* ---------------------------------------------------------------
           Key-based access (used by the Edit form and change-tracking).
           "Key" is a short identifier matching the HasX column's suffix
           (e.g. "Crustaceans" for HasCrustaceans, "PHE" for HasPHE) - the
           same key is reused as part of the Edit form's field names and
           as part of an EditHistory record's FieldChanged value
           ("Allergen:Milk" etc), so a change can always be traced back to
           exactly one column without any guesswork.
           --------------------------------------------------------------- */
        public struct AllergenKey
        {
            public string Key;         // matches the HasX column suffix
            public string DisplayName; // shown to people (matches AllNames)
            public AllergenKey(string key, string displayName) { Key = key; DisplayName = displayName; }
        }

        public static readonly AllergenKey[] Keys =
        {
            new AllergenKey("Crustaceans", "Crustacean"),
            new AllergenKey("Egg", "Egg"),
            new AllergenKey("Fish", "Fish"),
            new AllergenKey("Milk", "Milk"),
            new AllergenKey("Peanut", "Peanut"),
            new AllergenKey("Nuts", "Nuts"),
            new AllergenKey("Soy", "Soy"),
            new AllergenKey("Gluten", "Gluten"),
            new AllergenKey("Sulfites", "Sulfites"),
            new AllergenKey("PHE", "Aspartame (Phenylalanine)"),
        };

        // Reads the tri-state value ("0"/"1"/"M") for one allergen, by key.
        public static string GetTriState(Models.IngredientsAllergen row, string key)
        {
            if (row == null) return "0";
            switch (key)
            {
                case "Crustaceans": return Normalize(row.HasCrustaceans);
                case "Egg": return Normalize(row.HasEgg);
                case "Fish": return Normalize(row.HasFish);
                case "Milk": return Normalize(row.HasMilk);
                case "Peanut": return Normalize(row.HasPeanut);
                case "Nuts": return Normalize(row.HasNuts);
                case "Soy": return Normalize(row.HasSoy);
                case "Gluten": return Normalize(row.HasGluten);
                case "Sulfites": return Normalize(row.HasSulfites);
                case "PHE": return Normalize(row.HasPHE);
                default: return "0";
            }
        }

        // Writes a tri-state value ("0"/"1"/"M") for one allergen, by key.
        public static void SetTriState(Models.IngredientsAllergen row, string key, string value)
        {
            switch (key)
            {
                case "Crustaceans": row.HasCrustaceans = value; break;
                case "Egg": row.HasEgg = value; break;
                case "Fish": row.HasFish = value; break;
                case "Milk": row.HasMilk = value; break;
                case "Peanut": row.HasPeanut = value; break;
                case "Nuts": row.HasNuts = value; break;
                case "Soy": row.HasSoy = value; break;
                case "Gluten": row.HasGluten = value; break;
                case "Sulfites": row.HasSulfites = value; break;
                case "PHE": row.HasPHE = value; break;
            }
        }

        // Treats NULL/anything-unexpected the same as "0" so comparisons
        // are always well-defined.
        private static string Normalize(string value)
        {
            return value == "1" || value == "M" ? value : "0";
        }
    }
}