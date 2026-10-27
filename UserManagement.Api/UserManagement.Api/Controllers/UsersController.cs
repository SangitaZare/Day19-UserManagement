using MediatR;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Api.Features.Users.Commands.CreateUser;
using UserManagement.Api.Features.Users.Commands.DeleteUser;
using UserManagement.Api.Features.Users.Commands.UpdateUser;
using UserManagement.Api.Features.Users.Dtos;
using UserManagement.Api.Features.Users.Queries.GetAllUsers;
using UserManagement.Api.Features.Users.Queries.GetUserById;

namespace UserManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // QUERY: get all users
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var users = await _mediator.Send(new GetAllUsersQuery());
        return Ok(users);
    }

    // QUERY: get one user
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var user = await _mediator.Send(new GetUserByIdQuery(id));
        return user is null ? NotFound() : Ok(user);
    }

    // COMMAND: create a user
    [HttpPost]
    public async Task<ActionResult> Create(CreateUserCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    // COMMAND: update a user
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateUserCommand command)
    {
        if (id != command.Id) return BadRequest("Id in the URL and body must match.");

        var updated = await _mediator.Send(command);
        return updated ? NoContent() : NotFound();
    }

    // COMMAND: delete a user
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _mediator.Send(new DeleteUserCommand(id));
        return deleted ? NoContent() : NotFound();
    }
}