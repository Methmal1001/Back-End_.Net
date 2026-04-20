using NZWalks.API.Models.Domain;

namespace NZWalks.API.Controllers
{
    internal class ApiResponseFormat<T>
    {
        public int ResultCode { get; set; }
        public string ResultDesc { get; set; }
        public List<Region> Data { get; set; }
    }
}