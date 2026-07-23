using System.Text.Json.Serialization;

namespace NZWalks.API.Models.DTO.WhatsApp
{
    // Placeholder shape for the WhatsApp Cloud API webhook payload:
    // entry -> changes -> value -> messages[]. Top-level fields only for now —
    // full field mapping (contacts, statuses, media types, etc.) comes in a later step.

    public class WhatsAppWebhookPayload
    {
        [JsonPropertyName("object")]
        public string? Object { get; set; }

        [JsonPropertyName("entry")]
        public List<WhatsAppWebhookEntry> Entry { get; set; } = new();
    }

    public class WhatsAppWebhookEntry
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("changes")]
        public List<WhatsAppWebhookChange> Changes { get; set; } = new();
    }

    public class WhatsAppWebhookChange
    {
        [JsonPropertyName("field")]
        public string? Field { get; set; }

        [JsonPropertyName("value")]
        public WhatsAppWebhookValue? Value { get; set; }
    }

    public class WhatsAppWebhookValue
    {
        [JsonPropertyName("messaging_product")]
        public string? MessagingProduct { get; set; }

        [JsonPropertyName("messages")]
        public List<WhatsAppWebhookMessage> Messages { get; set; } = new();

        // TODO: metadata, contacts, statuses — later step
    }

    public class WhatsAppWebhookMessage
    {
        [JsonPropertyName("from")]
        public string? From { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        // TODO: text/image/document/etc. sub-objects — later step
    }
}
