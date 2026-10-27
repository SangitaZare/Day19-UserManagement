using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UserManagement.Day19.Data;
using UserManagement.Day19.Dtos;
using UserManagement.Day19.Models;

namespace UserManagement.Day19.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext db, IMapper mapper, ILogger<UserService> logger)
    {
        _db = db;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _db.Users.AsNoTracking().ToListAsync();
        _logger.LogInformation("Fetched {UserCount} users", users.Count);
        return _mapper.Map<List<UserDto>>(users);
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
        {
            _logger.LogWarning("User {UserId} was not found", id);
            return null;
        }

        _logger.LogInformation("Fetched user {UserId}", id);
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        var user = _mapper.Map<User>(dto);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created user {UserId} with email {Email}", user.Id, user.Email);
        return _mapper.Map<UserDto>(user);
    }
}