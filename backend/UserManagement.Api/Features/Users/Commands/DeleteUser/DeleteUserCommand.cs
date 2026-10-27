using MediatR;

namespace UserManagement.Api.Features.Users.Commands.DeleteUser;

public record DeleteUserCommand(int Id) : IRequest<bool>;