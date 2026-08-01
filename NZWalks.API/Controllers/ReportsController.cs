using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Filters;
using NZWalks.API.Helpers;
using NZWalks.API.Models.Domain.Inventory;
using NZWalks.API.Models.DTO.Product;
using NZWalks.API.Models.DTO.Report;
using NZWalks.API.Repositories;

namespace NZWalks.API.Controllers
{
    [Route("api/inventory/Reports")]
    [ApiController]
    [Authorize]
    [TypeFilter(typeof(ReportAccessLogFilter))]
    public class ReportsController : ControllerBase
    {
        private readonly IReportRepository _reportRepo;

        public ReportsController(IReportRepository reportRepo)
        {
            _reportRepo = reportRepo;
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/inventory/Reports/ProductCatalog
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("ProductCatalog")]
        [RequirePermission("Reports", "View")]
        [ProducesResponseType(typeof(List<ProductCatalogReportDto>), 200)]
        public async Task<IActionResult> ProductCatalog(
            [FromQuery] Guid? categoryId,
            [FromQuery] bool? isActive,
            [FromQuery] string format = "json")
        {
            var exportCheck = CheckExportPermission(format);
            if (exportCheck != null) return exportCheck;

            try
            {
                var products = await _reportRepo.GetProductCatalogAsync(categoryId, isActive);
                var rows = products.Select(MapToCatalogDto).ToList();

                if (IsCsv(format))
                {
                    return CsvFile("product-catalog",
                        new[] { "sku", "name", "categoryName", "barcode", "unitCost", "unitPrice", "marginPercent", "unitOfMeasure", "isActive" },
                        rows.Select(r => new[]
                        {
                            r.Sku, r.Name, r.CategoryName, r.Barcode,
                            r.UnitCost.ToString(CultureInfo.InvariantCulture),
                            r.UnitPrice.ToString(CultureInfo.InvariantCulture),
                            r.MarginPercent.ToString(CultureInfo.InvariantCulture),
                            r.UnitOfMeasure, r.IsActive.ToString()
                        }));
                }

                return Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = $"Retrieved {rows.Count} product(s) for the catalog report.",
                    Data = rows
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = "An error occurred while generating the product catalog report",
                    Data = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/inventory/Reports/PricingMargin
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("PricingMargin")]
        [RequirePermission("Reports", "View")]
        [ProducesResponseType(typeof(List<PricingMarginReportDto>), 200)]
        public async Task<IActionResult> PricingMargin(
            [FromQuery] Guid? categoryId,
            [FromQuery] decimal? minMarginPercent,
            [FromQuery] decimal? maxMarginPercent,
            [FromQuery] string format = "json")
        {
            var exportCheck = CheckExportPermission(format);
            if (exportCheck != null) return exportCheck;

            try
            {
                var products = await _reportRepo.GetPricingDataAsync(categoryId);

                IEnumerable<PricingMarginReportDto> rows = products.Select(p => new PricingMarginReportDto
                {
                    Sku = p.Sku,
                    Name = p.Name,
                    CategoryName = p.Category?.Name ?? string.Empty,
                    UnitCost = p.UnitCost,
                    UnitPrice = p.UnitPrice,
                    MarginAmount = p.UnitPrice - p.UnitCost,
                    MarginPercent = ComputeMarginPercent(p.UnitCost, p.UnitPrice)
                });

                if (minMarginPercent.HasValue)
                    rows = rows.Where(r => r.MarginPercent >= minMarginPercent.Value);

                if (maxMarginPercent.HasValue)
                    rows = rows.Where(r => r.MarginPercent <= maxMarginPercent.Value);

                var result = rows.OrderBy(r => r.MarginPercent).ToList();

                if (IsCsv(format))
                {
                    return CsvFile("pricing-margin",
                        new[] { "sku", "name", "categoryName", "unitCost", "unitPrice", "marginAmount", "marginPercent" },
                        result.Select(r => new[]
                        {
                            r.Sku, r.Name, r.CategoryName,
                            r.UnitCost.ToString(CultureInfo.InvariantCulture),
                            r.UnitPrice.ToString(CultureInfo.InvariantCulture),
                            r.MarginAmount.ToString(CultureInfo.InvariantCulture),
                            r.MarginPercent.ToString(CultureInfo.InvariantCulture)
                        }));
                }

                return Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = $"Retrieved {result.Count} product(s) for the pricing/margin report.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = "An error occurred while generating the pricing/margin report",
                    Data = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/inventory/Reports/ReorderThresholds
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("ReorderThresholds")]
        [RequirePermission("Reports", "View")]
        [ProducesResponseType(typeof(List<ReorderThresholdReportDto>), 200)]
        public async Task<IActionResult> ReorderThresholds(
            [FromQuery] Guid? categoryId,
            [FromQuery] string format = "json")
        {
            var exportCheck = CheckExportPermission(format);
            if (exportCheck != null) return exportCheck;

            try
            {
                var products = await _reportRepo.GetReorderThresholdsAsync(categoryId);

                var rows = products.Select(p => new ReorderThresholdReportDto
                {
                    Sku = p.Sku,
                    Name = p.Name,
                    CategoryName = p.Category?.Name ?? string.Empty,
                    ReorderPoint = p.ReorderPoint,
                    ReorderQuantity = p.ReorderQuantity,
                    MinStockLevel = p.MinStockLevel,
                    MaxStockLevel = p.MaxStockLevel
                }).ToList();

                if (IsCsv(format))
                {
                    return CsvFile("reorder-thresholds",
                        new[] { "sku", "name", "categoryName", "reorderPoint", "reorderQuantity", "minStockLevel", "maxStockLevel" },
                        rows.Select(r => new[]
                        {
                            r.Sku, r.Name, r.CategoryName,
                            r.ReorderPoint.ToString(CultureInfo.InvariantCulture),
                            r.ReorderQuantity.ToString(CultureInfo.InvariantCulture),
                            r.MinStockLevel.ToString(CultureInfo.InvariantCulture),
                            r.MaxStockLevel.ToString(CultureInfo.InvariantCulture)
                        }));
                }

                return Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = "Reference thresholds only — live on-hand quantity is not yet tracked.",
                    Data = rows
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = "An error occurred while generating the reorder thresholds report",
                    Data = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/inventory/Reports/CategorySummary
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("CategorySummary")]
        [RequirePermission("Reports", "View")]
        [ProducesResponseType(typeof(List<CategorySummaryReportDto>), 200)]
        public async Task<IActionResult> CategorySummary([FromQuery] string format = "json")
        {
            var exportCheck = CheckExportPermission(format);
            if (exportCheck != null) return exportCheck;

            try
            {
                var summary = await _reportRepo.GetCategorySummaryAsync();

                var rows = summary.Select(s => new CategorySummaryReportDto
                {
                    CategoryName = s.Category.Name,
                    ParentCategoryName = s.Category.ParentCategory?.Name,
                    IsActive = s.Category.IsActive,
                    ActiveProductCount = s.ActiveProductCount,
                    InactiveProductCount = s.InactiveProductCount,
                    SubCategoryCount = s.SubCategoryCount
                }).ToList();

                if (IsCsv(format))
                {
                    return CsvFile("category-summary",
                        new[] { "categoryName", "parentCategoryName", "isActive", "activeProductCount", "inactiveProductCount", "subCategoryCount" },
                        rows.Select(r => new[]
                        {
                            r.CategoryName, r.ParentCategoryName, r.IsActive.ToString(),
                            r.ActiveProductCount.ToString(CultureInfo.InvariantCulture),
                            r.InactiveProductCount.ToString(CultureInfo.InvariantCulture),
                            r.SubCategoryCount.ToString(CultureInfo.InvariantCulture)
                        }));
                }

                return Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = $"Retrieved summary for {rows.Count} categor{(rows.Count == 1 ? "y" : "ies")}.",
                    Data = rows
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = "An error occurred while generating the category summary report",
                    Data = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/inventory/Reports/InactiveProducts
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("InactiveProducts")]
        [RequirePermission("Reports", "View")]
        [ProducesResponseType(typeof(List<InactiveProductReportDto>), 200)]
        public async Task<IActionResult> InactiveProducts([FromQuery] string format = "json")
        {
            var exportCheck = CheckExportPermission(format);
            if (exportCheck != null) return exportCheck;

            try
            {
                var products = await _reportRepo.GetInactiveProductsAsync();

                var rows = products.Select(p => new InactiveProductReportDto
                {
                    Sku = p.Sku,
                    Name = p.Name,
                    CategoryName = p.Category?.Name ?? string.Empty,
                    UnitPrice = p.UnitPrice,
                    LastUpdatedAt = p.UpdatedAt,
                    LastUpdatedByUserName = p.UpdatedByUserName
                }).ToList();

                if (IsCsv(format))
                {
                    return CsvFile("inactive-products",
                        new[] { "sku", "name", "categoryName", "unitPrice", "lastUpdatedAt", "lastUpdatedByUserName" },
                        rows.Select(r => new[]
                        {
                            r.Sku, r.Name, r.CategoryName,
                            r.UnitPrice.ToString(CultureInfo.InvariantCulture),
                            r.LastUpdatedAt?.ToString("O"), r.LastUpdatedByUserName
                        }));
                }

                return Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = $"Retrieved {rows.Count} inactive product(s).",
                    Data = rows
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = "An error occurred while generating the inactive products report",
                    Data = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/inventory/Reports/DeletedProducts
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("DeletedProducts")]
        [RequirePermission("Reports", "View")]
        [ProducesResponseType(typeof(List<DeletedProductResponseDto>), 200)]
        public async Task<IActionResult> DeletedProducts(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string format = "json")
        {
            var exportCheck = CheckExportPermission(format);
            if (exportCheck != null) return exportCheck;

            try
            {
                var deleted = await _reportRepo.GetDeletedProductsAsync(fromDate, toDate);
                var rows = deleted.Select(MapToDeletedResponseDto).ToList();

                if (IsCsv(format))
                {
                    return CsvFile("deleted-products",
                        new[] { "sku", "name", "deletionReason", "deletedAt", "deletedByUserName" },
                        rows.Select(r => new[]
                        {
                            r.Sku, r.Name, r.DeletionReason, r.DeletedAt.ToString("O"), r.DeletedByUserName
                        }));
                }

                return Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = $"Retrieved {rows.Count} deleted product record(s).",
                    Data = rows
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = "An error occurred while generating the deleted products report",
                    Data = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/inventory/Reports/ProductAuditTrail
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("ProductAuditTrail")]
        [RequirePermission("Reports", "View")]
        [ProducesResponseType(typeof(List<ProductAuditLogDto>), 200)]
        public async Task<IActionResult> ProductAuditTrail(
            [FromQuery] Guid? productId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string format = "json")
        {
            var exportCheck = CheckExportPermission(format);
            if (exportCheck != null) return exportCheck;

            try
            {
                var logs = await _reportRepo.GetProductAuditTrailAsync(productId, fromDate, toDate);
                var rows = logs.Select(MapToAuditLogDto).ToList();

                if (IsCsv(format))
                {
                    return CsvFile("product-audit-trail",
                        new[] { "id", "productId", "action", "userId", "userName", "timestamp", "details" },
                        rows.Select(r => new[]
                        {
                            r.Id.ToString(), r.ProductId.ToString(), r.Action,
                            r.UserId.ToString(), r.UserName, r.Timestamp.ToString("O"), r.Details
                        }));
                }

                return Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = $"Retrieved {rows.Count} audit log entr{(rows.Count == 1 ? "y" : "ies")}.",
                    Data = rows
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = "An error occurred while generating the product audit trail report",
                    Data = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════════════════
        private static bool IsCsv(string format) => string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase);

        // "View" (checked via [RequirePermission]) covers on-screen access to every
        // report; csv downloads additionally require the elevated "Export" permission.
        private IActionResult? CheckExportPermission(string format)
        {
            if (!IsCsv(format)) return null;
            if (HrTierRoles.IsFullAccess(User)) return null;

            var hasExport = User.Claims.Any(c =>
                c.Type == "permission" && c.Value.Equals("Reports.Export", StringComparison.OrdinalIgnoreCase));

            if (hasExport) return null;

            return StatusCode(403, new
            {
                StatusCode = 403,
                IsSuccess = false,
                Message = "Access denied. Required permission: Reports.Export"
            });
        }

        private FileContentResult CsvFile(string reportName, IEnumerable<string> headers, IEnumerable<IEnumerable<string?>> rows)
        {
            var bytes = CsvWriter.Write(headers, rows);
            var fileName = $"{reportName}-{DateTime.UtcNow:yyyyMMdd}.csv";
            return File(bytes, "text/csv", fileName);
        }

        private static decimal ComputeMarginPercent(decimal unitCost, decimal unitPrice) =>
            unitPrice == 0 ? 0 : Math.Round((unitPrice - unitCost) / unitPrice * 100, 2);

        // ══════════════════════════════════════════════════════════════════════
        // PRIVATE MAPPERS
        // ══════════════════════════════════════════════════════════════════════
        private static ProductCatalogReportDto MapToCatalogDto(Product p) => new()
        {
            Sku = p.Sku,
            Name = p.Name,
            CategoryName = p.Category?.Name ?? string.Empty,
            Barcode = p.Barcode,
            UnitCost = p.UnitCost,
            UnitPrice = p.UnitPrice,
            MarginPercent = ComputeMarginPercent(p.UnitCost, p.UnitPrice),
            UnitOfMeasure = p.UnitOfMeasure,
            IsActive = p.IsActive
        };

        private static DeletedProductResponseDto MapToDeletedResponseDto(DeletedProduct d) => new()
        {
            Id = d.Id,
            OriginalProductId = d.OriginalProductId,
            Sku = d.Sku,
            Name = d.Name,
            Description = d.Description,
            CategoryName = d.CategoryName,
            SupplierName = d.SupplierName,
            UnitCost = d.UnitCost,
            UnitPrice = d.UnitPrice,
            UnitOfMeasure = d.UnitOfMeasure,
            Barcode = d.Barcode,
            OriginalCreatedAt = d.OriginalCreatedAt,
            DeletedByUserId = d.DeletedByUserId,
            DeletedByUserName = d.DeletedByUserName,
            DeletionReason = d.DeletionReason,
            DeletedAt = d.DeletedAt
        };

        private static ProductAuditLogDto MapToAuditLogDto(ProductAuditLog l) => new()
        {
            Id = l.Id,
            ProductId = l.ProductId,
            Action = l.Action.ToString(),
            UserId = l.UserId,
            UserName = l.UserName,
            Timestamp = l.Timestamp,
            Details = l.Details
        };
    }
}
