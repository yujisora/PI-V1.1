using System;
using System.Collections.Generic;
using PIV11.Models;
using PIV11.Models.ViewModels;

namespace PIV11.Infrastructure
{
    /* =====================================================================
       FieldChangeInfo
       One detected difference between the live data and a submitted Edit
       form. FieldKey is a stable identifier used both to store the change
       (EditHistory.FieldChanged) and to re-apply it later (ApplyChange
       switches on this same string) - so a pending change can always be
       traced back to exactly one column, with no guessing or parsing.

       FieldKey formats:
           "ProductName", "Brand", "ImageUrl", "NetVolume", "UnitMeasurement", "Ingredients"
           "Allergen:<Key>"   e.g. "Allergen:Milk"      (see AllergenHelper.Keys)
           "Nutrition:<Col>"  e.g. "Nutrition:Calories" (matches NutritionData column names)
           "Seal:<Col>"       e.g. "Seal:ExCalories"    (matches HealthAlert column names)
       ===================================================================== */
    public class FieldChangeInfo
    {
        public string FieldKey { get; set; }
        public string DisplayLabel { get; set; }
        public string NewValue { get; set; }
    }

    /* =====================================================================
       SealDefinition
       Static metadata for the 8 warning seals - shared by the Edit
       screen's checkbox list and Product Info's active-badge display, so
       both always agree on labels/shape without duplicating them.
       ===================================================================== */
    public class SealDefinition
    {
        public string Key;
        public string Label;
        public string SubLabel;
        public bool IsOctagon;
        public SealDefinition(string key, string label, string subLabel, bool isOctagon)
        {
            Key = key; Label = label; SubLabel = subLabel; IsOctagon = isOctagon;
        }
    }

    public static class ProductEditHelper
    {
        // The 7 seals, in the display order used throughout the app -
        // matches the mockup exactly (the earlier "Excess Fat" 8th seal
        // was removed per project feedback).
        public static readonly SealDefinition[] SealDefinitions =
        {
            new SealDefinition("ExCalories", "Excess\nCalories", null, true),
            new SealDefinition("ExSatFat", "Excess\nSaturated Fats", null, true),
            new SealDefinition("ExTrFat", "Excess\nTrans Fats", null, true),
            new SealDefinition("ExSugars", "Excess\nSugars", null, true),
            new SealDefinition("ExSod", "Excess\nSodium", null, true),
            new SealDefinition("HasCaffeine", "Contains Caffeine", "Avoid in children", false),
            new SealDefinition("HasSweeteners", "Contains Sweeteners", "Not recommended in children", false),
        };

        // The 10 core nutrition facts, in the mockup's display order, with
        // the matching NutritionData column name and display unit.
        public static readonly (string Column, string Label, string Unit, bool Indented)[] NutritionFields =
        {
            ("Calories", "Calories", "kcal", false),
            ("Fats", "Total Fat", "g", false),
            ("SaturatedFats", "Saturated Fat", "g", true),
            ("TransFats", "Trans Fat", "g", true),
            ("Carbs", "Carbohydrates", "g", false),
            ("Sugars", "Sugars", "g", false),
            ("AddedSugars", "Added Sugars", "g", true),
            ("Fiber", "Dietary Fiber", "g", false),
            ("Proteins", "Proteins", "g", false),
            ("Sodium", "Sodium", "mg", false),
        };

        /* ---------------- Compute (diff submitted vs current) ---------------- */

        public static List<FieldChangeInfo> ComputeCoreChanges(Product product, Foodstuff foodstuff, EditProductViewModel model)
        {
            var changes = new List<FieldChangeInfo>();

            string newName = (model.ProductName ?? "").Trim();
            if (product.ProductName != newName)
                changes.Add(new FieldChangeInfo { FieldKey = "ProductName", DisplayLabel = "Product Name", NewValue = newName });

            string newBrand = (model.Brand ?? "").Trim();
            if ((product.Brand ?? "") != newBrand)
                changes.Add(new FieldChangeInfo { FieldKey = "Brand", DisplayLabel = "Brand", NewValue = newBrand });

            string newImage = (model.ImageUrl ?? "").Trim();
            if ((product.ImageUrl ?? "") != newImage)
                changes.Add(new FieldChangeInfo { FieldKey = "ImageUrl", DisplayLabel = "Image URL", NewValue = newImage });

            string newWeight = (model.Weight ?? "").Trim();
            if ((foodstuff.NetVolume ?? "") != newWeight)
                changes.Add(new FieldChangeInfo { FieldKey = "NetVolume", DisplayLabel = "Net Weight/Volume", NewValue = newWeight });

            string newUnit = (model.Unit ?? "").Trim();
            if ((foodstuff.UnitMeasurement ?? "") != newUnit)
                changes.Add(new FieldChangeInfo { FieldKey = "UnitMeasurement", DisplayLabel = "Unit", NewValue = newUnit });

            return changes;
        }

        public static List<FieldChangeInfo> ComputeIngredientsChanges(IngredientsAllergen row, EditProductViewModel model)
        {
            var changes = new List<FieldChangeInfo>();

            string newIngredients = (model.Ingredients ?? "").Trim();
            if ((row.Ingredients ?? "") != newIngredients)
                changes.Add(new FieldChangeInfo { FieldKey = "Ingredients", DisplayLabel = "Ingredients", NewValue = newIngredients });

            foreach (var ak in AllergenHelper.Keys)
            {
                string oldValue = AllergenHelper.GetTriState(row, ak.Key);
                string newValue = model.GetAllergenValue(ak.Key) ?? "0";
                if (oldValue != newValue)
                {
                    changes.Add(new FieldChangeInfo
                    {
                        FieldKey = "Allergen:" + ak.Key,
                        DisplayLabel = ak.DisplayName,
                        NewValue = newValue
                    });
                }
            }

            return changes;
        }

        public static List<FieldChangeInfo> ComputeNutritionChanges(NutritionData row, EditProductViewModel model)
        {
            var changes = new List<FieldChangeInfo>();
            foreach (var field in NutritionFields)
            {
                int? oldValue = GetNutritionValue(row, field.Column);
                int? newValue = ParseIntOrNull(GetModelNutritionString(model, field.Column));
                if (oldValue != newValue)
                {
                    changes.Add(new FieldChangeInfo
                    {
                        FieldKey = "Nutrition:" + field.Column,
                        DisplayLabel = field.Label,
                        NewValue = newValue.HasValue ? newValue.Value.ToString() : ""
                    });
                }
            }
            return changes;
        }

        public static List<FieldChangeInfo> ComputeSealChanges(HealthAlert row, EditProductViewModel model)
        {
            var changes = new List<FieldChangeInfo>();
            foreach (var seal in SealDefinitions)
            {
                bool oldValue = GetSealValue(row, seal.Key);
                bool newValue = GetModelSealValue(model, seal.Key);
                if (oldValue != newValue)
                {
                    changes.Add(new FieldChangeInfo
                    {
                        FieldKey = "Seal:" + seal.Key,
                        DisplayLabel = seal.Label.Replace("\n", " "),
                        NewValue = newValue ? "1" : "0"
                    });
                }
            }
            return changes;
        }

        /* ---------------- Apply (write one change back to the entities) ---------------- */

        // Applies ONE change to whichever entity it belongs to. Used for:
        // direct admin saves, filling in brand-new rows, and approving a
        // single pending EditHistory record.
        public static void ApplyChange(FieldChangeInfo change, Product product, Foodstuff foodstuff, IngredientsAllergen ia, NutritionData nd, HealthAlert ha)
        {
            switch (change.FieldKey)
            {
                case "ProductName": product.ProductName = change.NewValue; return;
                case "Brand": product.Brand = string.IsNullOrEmpty(change.NewValue) ? null : change.NewValue; return;
                case "ImageUrl": product.ImageUrl = string.IsNullOrEmpty(change.NewValue) ? null : change.NewValue; return;
                case "NetVolume": foodstuff.NetVolume = change.NewValue; return;
                case "UnitMeasurement": foodstuff.UnitMeasurement = change.NewValue; return;
                case "Ingredients": ia.Ingredients = change.NewValue ?? ""; return;
            }

            if (change.FieldKey.StartsWith("Allergen:"))
            {
                AllergenHelper.SetTriState(ia, change.FieldKey.Substring("Allergen:".Length), change.NewValue);
            }
            else if (change.FieldKey.StartsWith("Nutrition:"))
            {
                SetNutritionValue(nd, change.FieldKey.Substring("Nutrition:".Length), ParseIntOrNull(change.NewValue));
            }
            else if (change.FieldKey.StartsWith("Seal:"))
            {
                SetSealValue(ha, change.FieldKey.Substring("Seal:".Length), change.NewValue == "1");
            }
        }

        /* ---------------- Small internal plumbing ---------------- */

        private static int? ParseIntOrNull(string text)
        {
            int result;
            if (string.IsNullOrWhiteSpace(text)) return null;
            return int.TryParse(text.Trim(), out result) ? (int?)result : null;
        }

        private static string GetModelNutritionString(EditProductViewModel m, string column)
        {
            switch (column)
            {
                case "Calories": return m.Calories;
                case "Proteins": return m.Proteins;
                case "Fats": return m.Fats;
                case "SaturatedFats": return m.SaturatedFats;
                case "TransFats": return m.TransFats;
                case "Carbs": return m.Carbs;
                case "Sugars": return m.Sugars;
                case "AddedSugars": return m.AddedSugars;
                case "Fiber": return m.Fiber;
                case "Sodium": return m.Sodium;
                default: return null;
            }
        }

        public static int? GetNutritionValue(NutritionData row, string column)
        {
            if (row == null) return null;
            switch (column)
            {
                case "Calories": return row.Calories;
                case "Proteins": return row.Proteins;
                case "Fats": return row.Fats;
                case "SaturatedFats": return row.SaturatedFats;
                case "TransFats": return row.TransFats;
                case "Carbs": return row.Carbs;
                case "Sugars": return row.Sugars;
                case "AddedSugars": return row.AddedSugars;
                case "Fiber": return row.Fiber;
                case "Sodium": return row.Sodium;
                default: return null;
            }
        }

        private static void SetNutritionValue(NutritionData row, string column, int? value)
        {
            switch (column)
            {
                case "Calories": row.Calories = value; break;
                case "Proteins": row.Proteins = value; break;
                case "Fats": row.Fats = value; break;
                case "SaturatedFats": row.SaturatedFats = value; break;
                case "TransFats": row.TransFats = value; break;
                case "Carbs": row.Carbs = value; break;
                case "Sugars": row.Sugars = value; break;
                case "AddedSugars": row.AddedSugars = value; break;
                case "Fiber": row.Fiber = value; break;
                case "Sodium": row.Sodium = value; break;
            }
        }

        private static bool GetModelSealValue(EditProductViewModel m, string key)
        {
            switch (key)
            {
                case "ExCalories": return m.ExCalories;
                case "ExSatFat": return m.ExSatFat;
                case "ExTrFat": return m.ExTrFat;
                case "ExSugars": return m.ExSugars;
                case "ExSod": return m.ExSod;
                case "HasSweeteners": return m.HasSweeteners;
                case "HasCaffeine": return m.HasCaffeine;
                default: return false;
            }
        }

        public static bool GetSealValue(HealthAlert row, string key)
        {
            if (row == null) return false;
            switch (key)
            {
                case "ExCalories": return row.ExCalories;
                case "ExSatFat": return row.ExSatFat;
                case "ExTrFat": return row.ExTrFat;
                case "ExSugars": return row.ExSugars;
                case "ExSod": return row.ExSod;
                case "HasSweeteners": return row.HasSweeteners;
                case "HasCaffeine": return row.HasCaffeine;
                default: return false;
            }
        }

        private static void SetSealValue(HealthAlert row, string key, bool value)
        {
            switch (key)
            {
                case "ExCalories": row.ExCalories = value; break;
                case "ExSatFat": row.ExSatFat = value; break;
                case "ExTrFat": row.ExTrFat = value; break;
                case "ExSugars": row.ExSugars = value; break;
                case "ExSod": row.ExSod = value; break;
                case "HasSweeteners": row.HasSweeteners = value; break;
                case "HasCaffeine": row.HasCaffeine = value; break;
            }
        }
    }
}