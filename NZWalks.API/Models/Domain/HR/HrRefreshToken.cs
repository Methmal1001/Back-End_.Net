namespace NZWalks.API.Models.Domain.HR
{
    public class HrRefreshToken
    {
        public Guid Id { get; set; }
        public Guid AppUserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}