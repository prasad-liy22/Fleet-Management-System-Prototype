using System.ComponentModel.DataAnnotations;
using FleetManagement.Application.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManagement.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = FleetPolicies.ManageUsers)]
public sealed class UsersController(IUserAdministrationService users) : ControllerBase
{
    [HttpGet]
    public Task<PageDto<UserAdminDto>> List([FromQuery] UserQuery query) => users.ListAsync(query);

    [HttpGet("{id:guid}")]
    public Task<UserAdminDto> Get(Guid id) => users.GetAsync(id);

    [HttpPost]
    public async Task<ActionResult<UserAdminDto>> Create(CreateUserRequest request)
    {
        var user = await users.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPut("{id:guid}")]
    public Task<UserAdminDto> Update(Guid id, UpdateUserRequest request) => users.UpdateAsync(id, request);

    // Read-only choices for account linking; not a Driver CRUD module.
    [HttpGet("driver-options")]
    public Task<IReadOnlyList<DriverOptionDto>> DriverOptions([FromQuery] Guid? userId,
        [FromQuery, MaxLength(160)] string? search) => users.DriverOptionsAsync(userId, search);
}
