using PIV11.Models;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace PIV11.Infrastructure
{
    /* AllergenHelper
    The database stores allergens as one column per allergen (e.g. HasMilk, HasNuts...) instead of a list. This helper converts back
    and forth between that column layout and the simple list-of-names the views use.
    
    Tri-state convention used by IngredientsAllergens.HasX columns:
       NULL or "0"  -> does not contain
       "1"          -> contains
       "M"          -> may contain
    */

    /* AllergenHelper
    La base de datos almacena los alérgenos como una columna por  alérgeno (por ejemplo, HasMilk, HasNuts...) en lugar de una
    lista. Este ayudante convierte entre ese diseño de columnas y la simple lista de nombres que usan las vistas.
    
    Convención de tres estados usada por las columnas HasX de IngredientsAllergens:
        NULL o "0"  -> no contiene
        "1"         -> contiene
        "M"         -> puede contener
    */

    /// <summary>
    /// Converts between the database's one-column-per-allergen layout
    /// (<c>IngredientsAllergens.HasX</c> / <c>People.AllergicToX</c>) and the
    /// simple string-list shape used throughout the views and change-tracking
    /// logic. Also exposes a <see cref="Keys"/> table for form-field/history
    /// lookups that need a stable short identifier rather than a display name.
    /// </summary>
    public static class AllergenHelper
    {
        // The 10 allergens tracked by the app, in display order.
            // Los 10 alérgenos que rastrea la aplicación, en orden de visualización.
        /// <summary>
        /// The 10 allergen display names tracked by the app, in the order they should be shown in any list or grid.
        /// </summary>
        public static readonly string[] AllNames =
        {
            "Crustacean", "Egg", "Fish", "Milk", "Peanut",
            "Nuts", "Soy", "Gluten", "Sulfites", "Aspartame (Phenylalanine)"
        };

        // Reads all 10 HasX columns off an IngredientsAllergen row and splits them into two lists: definitely contains, and may contain.
            // Lee las 10 columnas HasX de una fila de IngredientsAllergen y las separa en dos listas: definitivamente contiene, y puede contener.
        /// <summary>
        /// Reads all 10 <c>HasX</c> tri-state columns off an
        /// <see cref="Models.IngredientsAllergen"/> row and splits them into two simple name lists.
        /// </summary>
        /// <param name="row">
        /// The product's allergen row. Passing <c>null</c> is safe - both output lists come back empty.
        /// </param>
        /// <param name="contains">Allergens flagged as definitely present ("1").</param>
        /// <param name="mayContain">Allergens flagged as a cross-contamination risk ("M").</param>
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

        /// <summary>
        /// Adds <paramref name="allergenName"/> to <paramref name="contains"/> or
        /// <paramref name="mayContain"/> depending on the raw tri-state
        /// <paramref name="value"/>, or does nothing for "0"/null/unrecognized values.
        /// </summary>
        private static void AddIfSet(string value, string allergenName, List<string> contains, List<string> mayContain)
        {
            if (value == "1") contains.Add(allergenName);
            else if (value == "M") mayContain.Add(allergenName);
            // NULL or "0" (or anything else) -> not added to either list
                // NULL o "0" (o cualquier otro valor) -> no se agrega a ninguna lista
        }

        // Given the two lists (contains / may contain), returns the single tri-state character to store for one allergen name.
            // Dadas las dos listas (contiene / puede contener), devuelve el único carácter de tri-estado que se debe guardar para un
            // nombre de alérgeno.
        /// <summary>
        /// Given the two display-name lists, returns the single tri-state character ("0"/"1"/"M") that should be stored for one allergen.
        /// </summary>
        public static string ToDbValue(string allergenName, List<string> contains, List<string> mayContain)
        {
            if (contains.Contains(allergenName)) return "1";
            if (mayContain.Contains(allergenName)) return "M";
            return "0";
        }

        // Applies a full set of Contains/MayContain lists onto an IngredientsAllergen row's HasX columns (used when saving the Edit screen).
            // Aplica un conjunto completo de listas Contains/MayContain a las columnas HasX de una fila de IngredientsAllergen (se usa al guardar la pantalla de Edición).
        /// <summary>
        /// Writes a full set of Contains/MayContain lists onto an
        /// <see cref="Models.IngredientsAllergen"/> row's <c>HasX</c> columns. Used when saving the Edit screen.
        /// </summary>
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

        // Reads a Person's 10 AllergicToX booleans into a simple name list used by the My People danger/caution/safe comparison logic).
            // Lee los 10 booleanos AllergicToX de una Person y los convierte en una simple lista de nombres (se usa en la lógica de
            // comparación de peligro/precaución/seguro de Mi Gente).
        /// <summary>
        /// Reads a <see cref="Models.Person"/>'s 10 <c>AllergicToX</c> booleans
        /// into a simple allergen-name list, for the My People danger/caution/safe comparison logic.
        /// </summary>
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

        // Sets a Person's 10 AllergicToX booleans from a simple name list (used when saving a member on the My People screen).
            // Establece los 10 booleanos AllergicToX de una Persona (Person) a partir de una simple lista de nombres (se usa al guardar un
            // miembro en la pantalla de Mi Gente).
        /// <summary>
        /// Sets a <see cref="Models.Person"/>'s 10 <c>AllergicToX</c> booleans from a simple allergen-name list.
        /// Used when saving a member on the My People screen.
        /// </summary>
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

   
        /// <summary>
        /// A short, DB-column-matching key paired with its Spanish/English
        /// display name. <see cref="Key"/> is the stable identifier used in
        /// Edit form field names and <c>EditHistory.FieldChanged</c> values;
        /// <see cref="DisplayName"/> is what a person actually reads.
        /// </summary>
        public struct AllergenKey
        {
            public string Key;         // matches the HasX column suffix
                                           // coincide con el sufijo de la columna HasX
            public string DisplayName; // shown to people (matches AllNames)
                                           // se muestra a las personas (coincide con AllNames)
            public AllergenKey(string key, string displayName) { Key = key; DisplayName = displayName; }
        }

        /// <summary>
        /// All 10 allergens as (Key, DisplayName) pairs, in the same order as
        /// <see cref="AllNames"/>.
        /// </summary>
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
        // Lee el valor de tres estados ("0"/"1"/"M") de un alérgeno, por clave.
        /// <summary>Reads the tri-state value ("0"/"1"/"M") for one allergen, by <see cref="AllergenKey.Key"/>.</summary>
        /// <param name="row">The allergen row to read from; <c>null</c> safely returns "0".</param>
        /// <param name="key">One of the <see cref="AllergenKey.Key"/> values from <see cref="Keys"/>.</param>
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
            // Escribe un valor de tri-estado ("0"/"1"/"M") para un alérgeno, por clave.
        /// <summary>Writes a tri-state value ("0"/"1"/"M") for one allergen, by <see cref="AllergenKey.Key"/>.</summary>
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

        // Treats NULL/anything-unexpected the same as "0" so comparisons are always well-defined.
            // Trata NULL/cualquier valor inesperado igual que "0" para que las comparaciones siempre estén bien definidas.
        /// <summary>Normalizes any unrecognized/null value down to "0" so comparisons are always well-defined.</summary>
        private static string Normalize(string value)
        {
            return value == "1" || value == "M" ? value : "0";
        }
    }
}