using Microsoft.Extensions.Options;
using NZWalks.API.Configuration;

namespace NZWalks.API.Services
{
    public class WhatsAppMessagingService : IWhatsAppMessagingService
    {
        private readonly HttpClient _http;
        private readonly WhatsAppSettings _settings;
        private readonly ILogger<WhatsAppMessagingService> _logger;

        public WhatsAppMessagingService(
            HttpClient http,
            IOptions<WhatsAppSettings> settings,
            ILogger<WhatsAppMessagingService> logger)
        {
            _http = http;
            _settings = settings.Value;
            _logger = logger;
        }

        public Task SendTextMessageAsync(string toPhoneNumber, string message)
        {
            // TODO: POST to the WhatsApp Cloud API's /{ApiVersion}/{PhoneNumberId}/messages
            // endpoint using _settings.AccessToken as the Bearer token.
            throw new NotImplementedException();
        }
    }
}
