using MediatR;

namespace UserManagement.Api.Features.Users.Commands.UpdateUser;

public record UpdateUserCommand(int Id, string Name, string Email) : IRequest<bool>;