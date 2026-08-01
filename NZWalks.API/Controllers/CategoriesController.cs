using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Helpers;
using NZWalks.API.Models.Domain.Inventory;
using NZWalks.API.Models.DTO;
using NZWalks.API.Models.DTO.Category;
using NZWalks.API.Repositories;

namespace NZWalks.API.Controllers
{
    [Route("api/inventory")]
    [ApiController]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepo;

        public CategoriesController(ICategoryRepository categoryRepo)
        {
            _categoryRepo = categoryRepo;
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/inventory/GetAllCategories
        // Query params: isActive (optional)
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("GetAllCategories")]
        [RequirePermission("Products", "View")]
        [ProducesResponseType(typeof(List<CategoryResponseDto>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] bool? isActive)
        {
            try
            {
                var (categories, subCategoryCounts, productCounts) = await _categoryRepo.GetAllAsync(isActive);

                var result = categories
                    .Select(c => MapToResponseDto(c, subCategoryCounts, productCounts))
                    .ToList();

                return Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = "Categories retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = "An error occurred while retrieving categories",
                    Data = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/inventory/GetCategoryById
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("GetCategoryById")]
        [RequirePermission("Products", "View")]
        [ProducesResponseType(typeof(CategoryResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById([FromQuery] Guid id)
        {
            try
            {
                var category = await _categoryRepo.GetByIdAsync(id);

                if (category == null)
                {
                    return NotFound(new CommonApiResponse<object>
                    {
                        StatusCode = 404,
                        IsSuccess = false,
                        Message = $"Category with ID '{id}' was not found.",
                        Data = null
                    });
                }

                var subCategoryCount = await _categoryRepo.GetSubCategoryCountAsync(id);
                var productCount = await _categoryRepo.GetProductCountAsync(id);

                return Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = "Category retrieved successfully",
                    Data = MapToResponseDto(category, subCategoryCount, productCount)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = "An error occurred while retrieving category",
                    Data = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // POST  api/inventory/AddCategory
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("AddCategory")]
        [RequirePermission("Products", "ManageCategories")]
        [ProducesResponseType(typeof(CategoryResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new CommonApiResponse<object>
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        Message = "Validation failed",
                        Data = ModelState
                    });

                if (dto.ParentCategoryId.HasValue)
                {
                    var parent = await _categoryRepo.GetByIdAsync(dto.ParentCategoryId.Value);
                    if (parent == null)
                    {
                        return NotFound(new CommonApiResponse<object>
                        {
                            StatusCode = 404,
                            IsSuccess = false,
                            Message = $"Parent category with ID '{dto.ParentCategoryId}' was not found.",
                            Data = null
                        });
                    }
                }

                var category = new Category
                {
                    Name = dto.Name.Trim(),
                    Description = dto.Description?.Trim(),
                    ParentCategoryId = dto.ParentCategoryId,
                    IsActive = true
                };

                var created = await _categoryRepo.CreateAsync(category);

                return Ok(new CommonDetailsDto<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = "Category Created Successfully",
                    Data = MapToResponseDto(created, 0, 0)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonDetailsDto<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = "Category Creation Error Occurred",
                    Data = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // PUT  api/inventory/UpdateCategory
        // ══════════════════════════════════════════════════════════════════════
        [HttpPut("UpdateCategory")]
        [RequirePermission("Products", "ManageCategories")]
        [ProducesResponseType(typeof(CategoryResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update([FromBody] UpdateCategoryRequestDto dto)
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

                if (dto.ParentCategoryId.HasValue)
                {
                    if (dto.ParentCategoryId.Value == dto.Id)
                    {
                        return BadRequest(new CommonApiResponse<object>
                        {
                            StatusCode = 400,
                            IsSuccess = false,
                            Message = "A category cannot be its own parent.",
                            Data = null
                        });
                    }

                    var parent = await _categoryRepo.GetByIdAsync(dto.ParentCategoryId.Value);
                    if (parent == null)
                    {
                        return NotFound(new CommonApiResponse<object>
                        {
                            StatusCode = 404,
                            IsSuccess = false,
                            Message = $"Parent category with ID '{dto.ParentCategoryId}' was not found.",
                            Data = null
                        });
                    }
                }

                var updatedCategory = new Category
                {
                    Name = dto.Name.Trim(),
                    Description = dto.Description?.Trim(),
                    ParentCategoryId = dto.ParentCategoryId,
                    IsActive = dto.IsActive
                };

                var result = await _categoryRepo.UpdateAsync(dto.Id, updatedCategory);

                if (result == null)
                {
                    return NotFound(new CommonApiResponse<object>
                    {
                        StatusCode = 404,
                        IsSuccess = false,
                        Message = $"Category with ID '{dto.Id}' was not found.",
                        Data = null
                    });
                }

                var subCategoryCount = await _categoryRepo.GetSubCategoryCountAsync(dto.Id);
                var productCount = await _categoryRepo.GetProductCountAsync(dto.Id);

                return Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = "Category updated successfully",
                    Data = MapToResponseDto(result, subCategoryCount, productCount)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = "An error occurred while updating category",
                    Data = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // DELETE  api/inventory/DeleteCategoryByID
        // Behavior: soft-deletes (IsActive = false) when the category is still
        // referenced by products or sub-categories; otherwise removes it outright.
        // ══════════════════════════════════════════════════════════════════════
        [HttpDelete("DeleteCategoryByID")]
        [RequirePermission("Products", "ManageCategories")]
        [ProducesResponseType(typeof(CategoryResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete([FromQuery] Guid id)
        {
            try
            {
                var deleteResult = await _categoryRepo.DeleteAsync(id);

                if (deleteResult == null)
                {
                    return NotFound(new CommonApiResponse<object>
                    {
                        StatusCode = 404,
                        IsSuccess = false,
                        Message = $"Category with ID '{id}' was not found.",
                        Data = null
                    });
                }

                var message = deleteResult.WasSoftDeleted
                    ? $"Category is referenced by {deleteResult.LinkedProductCount} product(s) and {deleteResult.LinkedSubCategoryCount} sub-category(ies); it has been deactivated (soft-deleted) instead of removed."
                    : "Category deleted successfully.";

                return Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = message,
                    Data = MapToResponseDto(deleteResult.Category, deleteResult.LinkedSubCategoryCount, deleteResult.LinkedProductCount)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = "An error occurred while deleting category",
                    Data = ex.Message
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // PRIVATE MAPPERS
        // ══════════════════════════════════════════════════════════════════════
        private static CategoryResponseDto MapToResponseDto(
            Category c,
            Dictionary<Guid, int> subCategoryCounts,
            Dictionary<Guid, int> productCounts) => MapToResponseDto(
                c,
                subCategoryCounts.GetValueOrDefault(c.Id),
                productCounts.GetValueOrDefault(c.Id));

        private static CategoryResponseDto MapToResponseDto(Category c, int subCategoryCount, int productCount) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            ParentCategoryId = c.ParentCategoryId,
            ParentCategoryName = c.ParentCategory?.Name,
            SubCategoryCount = subCategoryCount,
            ProductCount = productCount,
            IsActive = c.IsActive
        };
    }
}
