using NZWalks.API.Models.DTO.Product;

namespace NZWalks.API.Controllers
{
    internal class CommonDetailsDto
    {
        public int StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public ProductListResponseDto Data { get; set; }
    }
}