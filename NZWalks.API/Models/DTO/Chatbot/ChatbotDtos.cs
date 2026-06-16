namespace NZWalks.API.Models.DTO.Chatbot
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatHistoryEntry> History { get; set; } = new();
    }

    public class ChatHistoryEntry
    {
        public string Role { get; set; } = string.Empty;   // "user" or "model"
        public string Content { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public string Reply { get; set; } = string.Empty;
        public bool IsFile { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public string? FileBase64 { get; set; }
    }
}
