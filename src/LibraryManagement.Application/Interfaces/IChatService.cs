using LibraryManagement.Application.DTOs.Chat;

namespace LibraryManagement.Application.Interfaces;

public interface IChatService
{
    Task<ChatResponseDto> AskAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
}
