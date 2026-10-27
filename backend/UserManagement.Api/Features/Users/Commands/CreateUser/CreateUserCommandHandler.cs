using MediatR;
using UserManagement.Api.Data;
using UserManagement.Api.Models;

namespace UserManagement.Api.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, int>
{
    private readonly AppDbContext _db;

    public CreateUserCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Name = request.Name,
            Email = request.Email
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}