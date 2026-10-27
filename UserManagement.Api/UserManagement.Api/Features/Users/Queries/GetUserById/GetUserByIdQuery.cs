using MediatR;
using UserManagement.Api.Features.Users.Dtos;

namespace UserManagement.Api.Features.Users.Queries.GetUserById;

public record GetUserByIdQuery(int Id) : IRequest<UserDto?>;