using MediatR;
using UserManagement.Api.Features.Users.Dtos;

namespace UserManagement.Api.Features.Users.Queries.GetAllUsers;

public record GetAllUsersQuery() : IRequest<List<UserDto>>;