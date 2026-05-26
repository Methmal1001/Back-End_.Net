namespace NZWalks.API.Controllers
{
    internal class CommonApiResponse<T>
    {
        public int StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}