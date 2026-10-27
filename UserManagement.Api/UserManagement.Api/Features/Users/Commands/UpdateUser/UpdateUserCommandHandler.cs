using MediatR;
using UserManagement.Api.Data;

namespace UserManagement.Api.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, bool>
{
    private readonly AppDbContext _db;

    public UpdateUserCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync(new object[] { request.Id }, cancellationToken);
        if (user is null) return false;

        user.Name = request.Name;
        user.Email = request.Email;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}