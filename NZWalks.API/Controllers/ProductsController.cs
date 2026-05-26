using Azure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Models.Domain.Inventory;
using NZWalks.API.Models.DTO;
using NZWalks.API.Models.DTO.Product;
using NZWalks.API.Repositories;

namespace NZWalks.API.Controllers
{
    [Route("api/inventory")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepo;

        public ProductsController(IProductRepository productRepo)
        {
            _productRepo = productRepo;
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/inventory/products
        // Query params: search, categoryId, isActive, sortBy, isDescending, page, pageSize
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("GetAllProducts")]
        [ProducesResponseType(typeof(ProductListResponseDto), 200)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] Guid? categoryId,
            [FromQuery] bool? isActive,
            [FromQuery] string? sortBy,
            [FromQuery] bool isDescending = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var (products, totalCount) = await _productRepo.GetAllAsync(
                search, categoryId, isActive, sortBy, isDescending, page, pageSize);

            var response = new ProductListResponseDto
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Products = products.Select(MapToResponseDto).ToList()
            };

            var data = new CommonDetailsDto
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Products Loading Successful",
                Data = response
            };

            return Ok(data);
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/inventory/products/{id}
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("GetProductById")]
        [ProducesResponseType(typeof(ProductResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById([FromQuery] Guid id)
        {
            try
            {
                var product = await _productRepo.GetByIdAsync(id);

                if (product == null)
                {
                    return NotFound(new CommonApiResponse<object>
                    {
                        StatusCode = 404,
                        IsSuccess = false,
                        Message = $"Product with ID '{id}' was not found.",
                        Data = null
                    });
                }

                return Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = "Product retrieved successfully",
                    Data = MapToResponseDto(product)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = "An error occurred while retrieving product",
                    Data = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/inventory/products/sku/{sku}
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("GetProductBySku")]
        [ProducesResponseType(typeof(ProductResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetBySku([FromQuery] string sku)
        {
            var product = await _productRepo.GetBySkuAsync(sku);

            try
            {
                if (product == null)
                {
                    return NotFound(new CommonApiResponse<object>
                    {
                        StatusCode = 404,
                        IsSuccess = false,
                        Message = $"Product with SKU '{sku}' was not found.",
                        Data = null
                    });
                }

                return Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = "Product retrieved successfully",
                    Data = MapToResponseDto(product)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = "An error occurred while retrieving product",
                    Data = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // POST  api/inventory/products
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("AddProducts")]
        [ProducesResponseType(typeof(ProductResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Create([FromBody] CreateProductRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var existing = await _productRepo.GetBySkuAsync(dto.Sku);

                if (existing != null)
                    return Conflict(new
                    {
                        message = $"A product with SKU '{dto.Sku}' already exists."
                    });

                var product = new Product
                {
                    Sku = dto.Sku.Trim(),
                    Name = dto.Name.Trim(),
                    Description = dto.Description?.Trim(),
                    CategoryId = dto.CategoryId,
                    SupplierId = dto.SupplierId,
                    UnitCost = dto.UnitCost,
                    UnitPrice = dto.UnitPrice,
                    UnitOfMeasure = dto.UnitOfMeasure.Trim(),
                    ReorderPoint = dto.ReorderPoint,
                    ReorderQuantity = dto.ReorderQuantity,
                    MinStockLevel = dto.MinStockLevel,
                    MaxStockLevel = dto.MaxStockLevel,
                    Barcode = dto.Barcode?.Trim(),
                    ImageUrl = dto.ImageUrl?.Trim(),
                    IsActive = true
                };

                var created = await _productRepo.CreateAsync(product);

                var response = new CommonDetailsDto<ProductResponseDto>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = "Product Created Successfully",
                    Data = MapToResponseDto(created)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonDetailsDto<object>
                {
                    StatusCode = 201,
                    IsSuccess = false,
                    Message = "Product Creation Error Occuerred",
                    Data = null
                });
            }
            
        }

        // ══════════════════════════════════════════════════════════════════════
        // PUT  api/inventory/products/{id}
        // ══════════════════════════════════════════════════════════════════════
        [HttpPut("UpdateProducts")]
        [ProducesResponseType(typeof(ProductResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Update([FromBody] UpdateProductRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new CommonApiResponse<object>
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        Message = "Validation failed",
                        Data = ModelState
                    });
                }

                var id = dto.Id;

                var skuOwner = await _productRepo.GetBySkuAsync(dto.Sku);

                if (skuOwner != null && skuOwner.Id != id)
                {
                    return Conflict(new CommonApiResponse<object>
                    {
                        StatusCode = 409,
                        IsSuccess = false,
                        Message = $"SKU '{dto.Sku}' is already used by another product.",
                        Data = null
                    });
                }

                var updatedProduct = new Product
                {
                    Sku = dto.Sku.Trim(),
                    Name = dto.Name.Trim(),
                    Description = dto.Description?.Trim(),
                    CategoryId = dto.CategoryId,
                    SupplierId = dto.SupplierId,
                    UnitCost = dto.UnitCost,
                    UnitPrice = dto.UnitPrice,
                    UnitOfMeasure = dto.UnitOfMeasure.Trim(),
                    ReorderPoint = dto.ReorderPoint,
                    ReorderQuantity = dto.ReorderQuantity,
                    MinStockLevel = dto.MinStockLevel,
                    MaxStockLevel = dto.MaxStockLevel,
                    IsActive = dto.IsActive,
                    Barcode = dto.Barcode?.Trim(),
                    ImageUrl = dto.ImageUrl?.Trim()
                };

                var result = await _productRepo.UpdateAsync(id, updatedProduct);

                if (result == null)
                {
                    return NotFound(new CommonApiResponse<object>
                    {
                        StatusCode = 404,
                        IsSuccess = false,
                        Message = $"Product with ID '{id}' was not found.",
                        Data = null
                    });
                }

                return Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = "Product updated successfully",
                    Data = MapToResponseDto(result)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = "An error occurred while updating product",
                    Data = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // DELETE  api/inventory/DeleteProductsByID
        // Body: id + reason + user info
        // ══════════════════════════════════════════════════════════════════════
        [HttpDelete("DeleteProductsByID")]
        [ProducesResponseType(typeof(DeletedProductResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete([FromBody] DeleteProductRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new CommonApiResponse<object>
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        Message = "Validation failed",
                        Data = ModelState
                    });
                }

                var deletedRecord = await _productRepo.DeleteAsync(dto.Id, dto);

                if (deletedRecord == null)
                {
                    return NotFound(new CommonApiResponse<object>
                    {
                        StatusCode = 404,
                        IsSuccess = false,
                        Message = $"Product with ID '{dto.Id}' was not found.",
                        Data = null
                    });
                }

                return Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = "Product deleted successfully",
                    Data = MapToDeletedResponseDto(deletedRecord)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = "An error occurred while deleting product",
                    Data = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/inventory/GetDeletedProducts
        // Filters: id, name, categoryName, supplierName
        // If empty → returns all
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("GetDeletedProducts")]
        [ProducesResponseType(typeof(List<DeletedProductResponseDto>), 200)]
        public async Task<IActionResult> GetDeletedProducts(
            [FromQuery] Guid? id,
            [FromQuery] string? name,
            [FromQuery] string? categoryName,
            [FromQuery] string? supplierName)
        {
            try
            {
                var deletedProducts = await _productRepo.GetDeletedProductsAsync();

                // -----------------------------
                // APPLY FILTERS (in-memory or ideally repo level)
                // -----------------------------

                if (id.HasValue)
                {
                    deletedProducts = deletedProducts
                        .Where(x => x.OriginalProductId == id.Value)
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    deletedProducts = deletedProducts
                        .Where(x => x.Name != null &&
                                    x.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(categoryName))
                {
                    deletedProducts = deletedProducts
                        .Where(x => x.CategoryName != null &&
                                    x.CategoryName.Contains(categoryName, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(supplierName))
                {
                    deletedProducts = deletedProducts
                        .Where(x => x.SupplierName != null &&
                                    x.SupplierName.Contains(supplierName, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                var result = deletedProducts
                    .Select(MapToDeletedResponseDto)
                    .ToList();

                return Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = "Deleted products retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = "An error occurred while retrieving deleted products",
                    Data = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // PRIVATE MAPPERS
        // ══════════════════════════════════════════════════════════════════════
        private static ProductResponseDto MapToResponseDto(Product p) => new()
        {
            Id = p.Id,
            Sku = p.Sku,
            Name = p.Name,
            Description = p.Description,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? string.Empty,
            SupplierId = p.SupplierId,
            SupplierName = p.Supplier?.Name,
            UnitCost = p.UnitCost,
            UnitPrice = p.UnitPrice,
            UnitOfMeasure = p.UnitOfMeasure,
            ReorderPoint = p.ReorderPoint,
            ReorderQuantity = p.ReorderQuantity,
            MinStockLevel = p.MinStockLevel,
            MaxStockLevel = p.MaxStockLevel,
            IsActive = p.IsActive,
            Barcode = p.Barcode,
            ImageUrl = p.ImageUrl,
            CreatedAt = p.CreatedAt
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
    }
}