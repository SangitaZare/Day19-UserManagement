using MediatR;
using UserManagement.Api.Data;

namespace UserManagement.Api.Features.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteUserCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync(new object[] { request.Id }, cancellationToken);
        if (user is null) return false;

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}