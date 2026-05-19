using Microsoft.EntityFrameworkCore;
using TBCarePlus.API.Data;
using TBCarePlus.API.DTOs;
using TBCarePlus.API.Interfaces;
using TBCarePlus.API.Models;

namespace TBCarePlus.API.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    public UserService(AppDbContext db) => _db = db;

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        return user is null ? null : Map(user);
    }

    public async Task<bool> ExistsAsync(Guid id) => await _db.Users.AnyAsync(u => u.Id == id);

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        var user = new User
        {
            Id = dto.Id,
            FullName = dto.FullName,
            Email = dto.Email,
            Age = dto.Age,
            Gender = dto.Gender,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Map(user);
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return null;

        if (dto.FullName is not null) user.FullName = dto.FullName;
        if (dto.Age is not null) user.Age = dto.Age;
        if (dto.Gender is not null) user.Gender = dto.Gender;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Map(user);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return false;
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return true;
    }

    private static UserDto Map(User u) => new()
    {
        Id = u.Id,
        FullName = u.FullName,
        Email = u.Email,
        Age = u.Age,
        Gender = u.Gender,
        Role = u.Role,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt,
        UpdatedAt = u.UpdatedAt,
    };
}
