using Microsoft.AspNetCore.Mvc;
using TBCarePlus.API.DTOs;
using TBCarePlus.API.Interfaces;

namespace TBCarePlus.API.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token."));

        var user = await _userService.GetByIdAsync(userId.Value);
        if (user is null)
            return NotFound(ApiResponse<object>.Fail("User not found."));

        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null)
            return NotFound(ApiResponse<object>.Fail("User not found."));

        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        if (await _userService.ExistsAsync(dto.Id))
            return Conflict(ApiResponse<object>.Fail("User already exists."));

        var user = await _userService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, ApiResponse<UserDto>.Ok(user, "User created."));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserDto dto)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token."));

        var user = await _userService.UpdateAsync(userId.Value, dto);
        if (user is null)
            return NotFound(ApiResponse<object>.Fail("User not found."));

        return Ok(ApiResponse<UserDto>.Ok(user, "User updated."));
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe()
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token."));

        var deleted = await _userService.DeleteAsync(userId.Value);
        if (!deleted)
            return NotFound(ApiResponse<object>.Fail("User not found."));

        return Ok(ApiResponse<object>.Ok(null!, "User deleted."));
    }

    private Guid? GetUserId()
    {
        var subClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?? User.FindFirst("sub");
        if (subClaim is null || !Guid.TryParse(subClaim.Value, out var userId))
            return null;
        return userId;
    }
}
