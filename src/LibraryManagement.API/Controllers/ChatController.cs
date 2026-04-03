using LibraryManagement.Application.DTOs.Chat;
using LibraryManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService) => _chatService = chatService;

    /// <summary>Envía un mensaje al chatbot local de biblioteca.</summary>
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] ChatRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _chatService.AskAsync(request, cancellationToken);
        return Ok(response);
    }
}
