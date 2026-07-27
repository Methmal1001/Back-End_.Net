namespace NZWalks.API.Models.DTO.Report
{
    public class ProductCatalogReportDto
    {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public decimal UnitCost { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal MarginPercent { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class PricingMarginReportDto
    {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal UnitCost { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal MarginAmount { get; set; }
        public decimal MarginPercent { get; set; }
    }

    public class ReorderThresholdReportDto
    {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int ReorderPoint { get; set; }
        public int ReorderQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public int MaxStockLevel { get; set; }
    }

    public class CategorySummaryReportDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public string? ParentCategoryName { get; set; }
        public bool IsActive { get; set; }
        public int ActiveProductCount { get; set; }
        public int InactiveProductCount { get; set; }
        public int SubCategoryCount { get; set; }
    }

    public class InactiveProductReportDto
    {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public string? LastUpdatedByUserName { get; set; }
    }
}
