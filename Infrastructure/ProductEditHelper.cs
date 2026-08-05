using System;
using System.Collections.Generic;
using PIV11.Models;
using PIV11.Models.ViewModels;

namespace PIV11.Infrastructure
{

    /// <summary>
    /// One detected difference between the live product data and a submitted Edit form - either applied immediately or stored as a pending
    /// <c>EditHistory</c> record, depending on who's saving and whether the field already had a value.
    /// See <see cref="ProductEditHelper.ApplyChange"/>.
    /// </summary>
    public class FieldChangeInfo
    {
        /// <summary>
        /// Stable identifier for exactly which column this change belongs to (e.g. <c>"ProductName"</c>, <c>"Allergen:Milk"</c>,
        /// <c>"Nutrition:Calories"</c>, <c>"Seal:ExCalories"</c>). Used both to persist the change and to re-apply it later.
        /// </summary>
        public string FieldKey { get; set; }

        /// <summary>Human-readable label for this field, shown in Edit History.</summary>
        public string DisplayLabel { get; set; }

        /// <summary>The new value, as a string, ready to store or apply.</summary>
        public string NewValue { get; set; }
    }

    /// <summary>
    /// Static metadata for one warning seal (label, optional sub-label, and whether it renders as an octagon or a rounded rectangle). 
    /// Shared by the Edit screen's checkbox list and Product Info's active-badge display
    /// via <see cref="ProductEditHelper.SealDefinitions"/>.
    /// </summary>
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

    /// <summary>
    /// The core of the product edit/approval system: static definitions for the warning seals and nutrition fields, functions that diff a
    /// submitted Edit form against the live data (<c>Compute*Changes</c>), and <see cref="ApplyChange"/> - the single method that knows how to
    /// write each field back, reused identically by a direct admin save, filling in a brand-new row, and approving one pending edit.
    /// </summary>
    public static class ProductEditHelper
    {
        /// <summary>
        /// The 7 warning seals, in display order.
        /// </summary>
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

        /// <summary>
        /// The 10 core nutrition facts, in display order, each paired with its matching <c>NutritionData</c> column name,
        /// display unit, and whether it renders indented (a sub-item of the row above).
        /// </summary>
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
        // ---------------- Calcular (comparar lo enviado contra lo actual) ----------------

        /// <summary>
        /// Diffs the core product fields (name, brand, image, weight, unit)
        /// on <paramref name="model"/> against the live <paramref name="product"/>/
        /// <paramref name="foodstuff"/> data.
        /// </summary>
        /// <returns>One <see cref="FieldChangeInfo"/> per field that actually changed.</returns>
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

        /// <summary>
        /// Diffs the ingredients text and all 10 allergen tri-state values on
        /// <paramref name="model"/> against the live <paramref name="row"/>.
        /// </summary>
        /// <returns>One <see cref="FieldChangeInfo"/> per field/allergen that actually changed.</returns>
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

        /// <summary>
        /// Diffs all 10 nutrition fields on <paramref name="model"/> against the live <paramref name="row"/>.
        /// </summary>
        /// <returns>One <see cref="FieldChangeInfo"/> per nutrition field that actually changed.</returns>
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

        /// <summary>
        /// Diffs all 7 warning seal checkboxes on <paramref name="model"/> against
        /// the live <paramref name="row"/>.
        /// </summary>
        /// <returns>One <see cref="FieldChangeInfo"/> per seal whose checked state actually changed.</returns>
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

        //Apply (write one change back to the entities

        // Applies ONE change to whichever entity it belongs to. Used for:
        // direct admin saves, filling in brand-new rows, and approving a
        // single pending EditHistory record.

            // Aplicar (escribir un cambio de vuelta en las entidades)

            // Aplica UN cambio a la entidad a la que pertenece. Se usa
            // para: guardados directos de admin, completar filas
            // recién creadas, y aprobar un único registro pendiente de
            // EditHistory.

        /// <summary>
        /// Writes one <paramref name="change"/> to whichever entity it belongs to, resolved by <see cref="FieldChangeInfo.FieldKey"/>. 
        /// This is the single place that knows how to read/write each field - reused identically by a direct admin save, 
        /// filling in a brand-new row, and approving one pending <c>EditHistory</c> record, so the
        /// diff/apply logic never has to change when the routing decision (who saves directly vs. who gets queued) changes.
        /// </summary>
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

        /// <summary>Parses a nullable int, returning <c>null</c> for blank/unparsable text instead of throwing.</summary>
        private static int? ParseIntOrNull(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            return int.TryParse(text.Trim(), out int result) ? (int?)result : null;
        }

        /// <summary>Reads the raw submitted string for one nutrition column off the Edit form model.</summary>
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

        /// <summary>Reads the current value of one nutrition column off a <see cref="NutritionData"/> row, by column name.</summary>
        /// <param name="row">The row to read from; <c>null</c> safely returns <c>null</c>.</param>
        /// <param name="column">One of <see cref="NutritionFields"/>' <c>Column</c> values.</param>
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

        /// <summary>Writes one nutrition column's value onto a <see cref="NutritionData"/> row, by column name.</summary>
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

        /// <summary>Reads the checked state of one seal checkbox off the Edit form model, by seal key.</summary>
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

        /// <summary>Reads whether one seal is currently active on a <see cref="HealthAlert"/> row, by seal key.</summary>
        /// <param name="row">The row to read from; <c>null</c> safely returns <c>false</c>.</param>
        /// <param name="key">One of <see cref="SealDefinitions"/>' <c>Key</c> values.</param>
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

        /// <summary>Writes one seal's active state onto a <see cref="HealthAlert"/> row, by seal key.</summary>
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