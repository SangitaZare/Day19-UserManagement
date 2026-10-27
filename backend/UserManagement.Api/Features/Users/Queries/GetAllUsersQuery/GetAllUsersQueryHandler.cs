using MediatR;
using Microsoft.EntityFrameworkCore;
using UserManagement.Api.Data;
using UserManagement.Api.Features.Users.Dtos;

namespace UserManagement.Api.Features.Users.Queries.GetAllUsers;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, List<UserDto>>
{
    private readonly AppDbContext _db;

    public GetAllUsersQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<UserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        return await _db.Users
            .AsNoTracking()
            .Select(u => new UserDto(u.Id, u.Name, u.Email))
            .ToListAsync(cancellationToken);
    }
}