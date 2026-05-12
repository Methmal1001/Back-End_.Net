using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Models.Domain.Inventory;
using NZWalks.API.Models.DTO.Product;
using NZWalks.API.Repositories;

namespace NZWalks.API.Controllers
{
    [Route("api/inventory/[controller]")]
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
        [HttpGet]
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

            return Ok(response);
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/inventory/products/{id}
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ProductResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var product = await _productRepo.GetByIdAsync(id);

            if (product == null)
                return NotFound(new { message = $"Product with ID '{id}' was not found." });

            return Ok(MapToResponseDto(product));
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/inventory/products/sku/{sku}
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("sku/{sku}")]
        [ProducesResponseType(typeof(ProductResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetBySku([FromRoute] string sku)
        {
            var product = await _productRepo.GetBySkuAsync(sku);

            if (product == null)
                return NotFound(new { message = $"Product with SKU '{sku}' was not found." });

            return Ok(MapToResponseDto(product));
        }

        // ══════════════════════════════════════════════════════════════════════
        // POST  api/inventory/products
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ProducesResponseType(typeof(ProductResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create([FromBody] CreateProductRequestDto dto)
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

            return Ok(MapToResponseDto(created));
        }

        // ══════════════════════════════════════════════════════════════════════
        // PUT  api/inventory/products/{id}
        // ══════════════════════════════════════════════════════════════════════
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ProductResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Update(
            [FromRoute] Guid id,
            [FromBody] UpdateProductRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Duplicate SKU check (exclude current product)
            var skuOwner = await _productRepo.GetBySkuAsync(dto.Sku);
            if (skuOwner != null && skuOwner.Id != id)
                return Conflict(new { message = $"SKU '{dto.Sku}' is already used by another product." });

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
                return NotFound(new { message = $"Product with ID '{id}' was not found." });

            return Ok(MapToResponseDto(result));
        }

        // ══════════════════════════════════════════════════════════════════════
        // DELETE  api/inventory/products/{id}
        // Body: optional reason + who deleted it
        // ══════════════════════════════════════════════════════════════════════
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(DeletedProductResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid id,
            [FromBody] DeleteProductRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var deletedRecord = await _productRepo.DeleteAsync(id, dto);

            if (deletedRecord == null)
                return NotFound(new { message = $"Product with ID '{id}' was not found." });

            return Ok(new
            {
                message = "Product deleted and archived successfully.",
                deletedRecord = MapToDeletedResponseDto(deletedRecord)
            });
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/inventory/products/deleted
        // View the deleted products archive
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("deleted")]
        [ProducesResponseType(typeof(List<DeletedProductResponseDto>), 200)]
        public async Task<IActionResult> GetDeletedProducts()
        {
            var deletedProducts = await _productRepo.GetDeletedProductsAsync();
            return Ok(deletedProducts.Select(MapToDeletedResponseDto));
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/inventory/products/deleted/{id}
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("deleted/{id:guid}")]
        [ProducesResponseType(typeof(DeletedProductResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetDeletedProductById([FromRoute] Guid id)
        {
            var deleted = await _productRepo.GetDeletedProductByIdAsync(id);

            if (deleted == null)
                return NotFound(new { message = $"No deleted product record found with ID '{id}'." });

            return Ok(MapToDeletedResponseDto(deleted));
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