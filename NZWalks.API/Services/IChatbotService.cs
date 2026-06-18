using System.Security.Claims;
using NZWalks.API.Models.DTO.Chatbot;

namespace NZWalks.API.Services
{
    public interface IChatbotService
    {
        Task<ChatResponse> AskAsync(ChatRequest request, ClaimsPrincipal user);
    }
}
