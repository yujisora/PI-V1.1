using System;
using System.Collections.Generic;

namespace PIV11.Models.ViewModels
{

    /// <summary>
    /// The main view for the admin dashboard, which shows product totals, pending edits, recent activity, and a product lookup search box.
    /// </summary>
    public class AdminDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int PendingEditsCount { get; set; }
        public int ApprovedEditsCount { get; set; }
        public int DeniedEditsCount { get; set; }

        public List<AdminProductSummary> UneditedProducts { get; set; }
        public List<AdminActivityItem> RecentActivity { get; set; }
        public string SearchQuery { get; set; }
        /// <summary>
        /// SearchQuery is the search term entered by the admin.
        /// </summary>  
        public string SearchError { get; set; }
        /// <summary>
        /// SearchError is a message indicating any error that occurred during the search process, such as invalid input or no results found. 
        /// </summary>
        public string SearchNotFoundMessage { get; set; }
        /// <summary>
        /// SearchNotFoundMessage is a message displayed when no products match the search query.
        /// </summary>
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