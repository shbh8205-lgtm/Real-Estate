using MediatR;
using RealEstate.Application.Properties.Dtos;

namespace RealEstate.Application.Chat;

public record ChatQuery(string Message, string ConversationId) : IRequest<ChatReply>;

public record ChatReply(
    string Reply,
    IReadOnlyList<PropertyDto> SuggestedProperties
);