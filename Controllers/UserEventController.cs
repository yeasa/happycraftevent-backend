using HappyCraftEvent.Contracts.DTOs.Users;
using HappyCraftEvent.Contracts.Enums;
using HappyCraftEvent.Contracts.StatusCodes;
using HappyCraftEvent.Helper.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyCraftEvent.Controllers;

[ApiController]
[Route("api/")]
[Authorize]
public class UserEventController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserEventController> _logger;

    public UserEventController(IUserService userService, ILogger<UserEventController> logger)
    {
        _userService = userService;
        _logger      = logger;
    }

    [HttpGet("user-list")]
    [Authorize(Policy = "Scope:UsersRead")]
    public async Task<IActionResult> GetUsers([FromQuery] UserListQueryDto query)
    {
        try
        {
            var (statusCode, result) = await _userService.GetUsersAsync(query);

            return statusCode switch
            {
                HappyCraftStatusCode.OK => Ok(result),
                _                       => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user list.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpPost("user-upsert")]
    [Authorize(Policy = "Scope:UsersWrite")]
    public async Task<IActionResult> UpsertUser(
        [FromQuery] OperationType operation,
        [FromBody]  UpsertUserRequestDto request)
    {
        try
        {
            if (request is null)
                return BadRequest("Request body is required.");

            var (statusCode, userId) = await _userService.UpsertUserAsync(operation, request);

            return statusCode switch
            {
                HappyCraftStatusCode.OK               => Ok(new { userId }),
                HappyCraftStatusCode.INVALID_REQUEST  => BadRequest("Email is required. FirstName and Password are required when adding. Role is required on Add."),
                HappyCraftStatusCode.DB_ERROR         => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Database error" }),
                HappyCraftStatusCode.INTERNAL_ERROR   => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" }),
                _                                     => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting user. Operation: {Operation}", operation);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}
