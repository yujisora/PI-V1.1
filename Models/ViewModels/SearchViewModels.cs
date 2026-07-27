using System;
using System.Collections.Generic;

namespace PIV11.Models.ViewModels
{
	/* =====================================================================
       RecentSearchItem
       A small, lightweight snapshot of a product - NOT the full Product
       entity - stored in Session as the "Recent Searches" list. Marked
       [Serializable] as good practice in case Session storage mode ever
       changes from the default in-process mode.
       ===================================================================== */
	[Serializable]
	public class RecentSearchItem
	{
		public decimal UPC { get; set; }
		public string Name { get; set; }
		public string Brand { get; set; }
		public bool HasAllergens { get; set; }
	}

	/* =====================================================================
       ProductSearchResultViewModel
       One row in the "multiple products matched your search" list shown
       on the Home page when a name/brand search isn't specific enough to
       pick a single product automatically.
       ===================================================================== */
	public class ProductSearchResultViewModel
	{
		public decimal UPC { get; set; }
		public string Name { get; set; }
		public string Brand { get; set; }
		public bool HasAllergens { get; set; }
	}

	/* =====================================================================
       HomeSearchViewModel
       Everything the Home view (Views/Home/Index.cshtml) needs to render
       both the initial empty search box AND the result of a search
       (error, not-found, or an ambiguous multi-result list).
       ===================================================================== */
	public class HomeSearchViewModel
	{
		// Re-shown in the search box so the person doesn't lose what they typed.
		public string Query { get; set; }

		// Set when the input failed barcode validation or was empty.
		public string ErrorMessage { get; set; }

		// Set when the barcode/name was valid but matched no product.
		public string NotFoundMessage { get; set; }

		// If a barcode search came back empty, this carries the normalized
		// UPC forward so the "Add it here" link can prefill the Add
		// Product form.
		public string NotFoundUpc { get; set; }

		// Populated only when a name/brand search matched more than one
		// product - the person picks which one they meant.
		public List<ProductSearchResultViewModel> Results { get; set; }

		public HomeSearchViewModel()
		{
			Results = new List<ProductSearchResultViewModel>();
		}
	}

	/* =====================================================================
       AddProductViewModel
       Backs the Add Product form (Views/Home/AddProduct.cshtml). Fields
       are all plain strings (not decimal/int) because they come straight
       from form input and need custom validation/parsing in the
       controller - matches the pattern already used in AccountController.
       ===================================================================== */
	public class AddProductViewModel
	{
		public string UPC { get; set; }
		public string ProductName { get; set; }
		public string Brand { get; set; }
		public string Weight { get; set; }
		public string Unit { get; set; }

		public AddProductViewModel()
		{
			Unit = "g";
		}
	}
}