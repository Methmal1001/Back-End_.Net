using NZWalks.API.Models.DTO.Product;

namespace NZWalks.API.Models.DTO
{
    public class CommonDetailsDto<T>
    {
        public int StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public T Data { get; set; }
        public List<string>? Errors { get; set; }
    }
}
