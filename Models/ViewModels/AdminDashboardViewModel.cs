using System;
using System.Collections.Generic;

namespace PIV11.Models.ViewModels
{
    /* =====================================================================
       AdminDashboardViewModel
       Backs the admin-only dashboard shown in place of the normal search
       Home page. Everything here is derived from existing Products/
       EditHistory data - no new columns or tables. Notably, Products has
       no creation-date column, so there's no way to show genuinely
       "recently added" products chronologically - UneditedProducts (zero
       EditHistory rows) is the closest honest proxy available.
       ===================================================================== */
    public class AdminDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int PendingEditsCount { get; set; }
        public int ApprovedEditsCount { get; set; }
        public int DeniedEditsCount { get; set; }

        public List<AdminProductSummary> UneditedProducts { get; set; }
        public List<AdminActivityItem> RecentActivity { get; set; }

        // Populated only when a search was just attempted from the
        // dashboard's compact "Product Lookup" box and didn't resolve
        // straight to a single product (exact matches redirect directly
        // to Product Info and never reach this viewmodel at all).
        public string SearchQuery { get; set; }
        public string SearchError { get; set; }
        public string SearchNotFoundMessage { get; set; }
        public List<ProductSearchResultViewModel> SearchResults { get; set; }

        public AdminDashboardViewModel()
        {
            UneditedProducts = new List<AdminProductSummary>();
            RecentActivity = new List<AdminActivityItem>();
            SearchResults = new List<ProductSearchResultViewModel>();
        }
    }

    public class AdminProductSummary
    {
        public decimal UPC { get; set; }
        public string Name { get; set; }
        public string Brand { get; set; }
    }

    public class AdminActivityItem
    {
        public decimal UPC { get; set; }
        public string ProductName { get; set; }
        public string FieldChanged { get; set; }
        public string NewValue { get; set; }
        public string EditedByUser { get; set; }
        public DateTime DateEdited { get; set; }
        public string Status { get; set; }
    }
}