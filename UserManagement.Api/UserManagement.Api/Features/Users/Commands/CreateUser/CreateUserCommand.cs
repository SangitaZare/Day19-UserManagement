using MediatR;

namespace UserManagement.Api.Features.Users.Commands.CreateUser;

public record CreateUserCommand(string Name, string Email) : IRequest<int>;