using FleetManagement.Application.Access;
using FleetManagement.Application.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManagement.Api.Controllers;

[ApiController, Route("api/drivers"), Authorize(Policy = FleetPolicies.ManageFleet)]
public sealed class DriversController(IDriverService service) : ControllerBase
{
    [HttpGet]
    public Task<PageDto<DriverResponse>> List([FromQuery] MasterQuery query) => service.ListAsync(query);
    [HttpGet("{id:guid}")]
    public Task<DriverResponse> Get(Guid id, [FromQuery] bool includeDeleted = false) => service.GetAsync(id, includeDeleted);
    [HttpPost]
    public async Task<ActionResult<DriverResponse>> Create(DriverRequest request)
    {
        var result = await service.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }
    [HttpPut("{id:guid}")]
    public Task<DriverResponse> Update(Guid id, UpdateDriverRequest request) => service.UpdateAsync(id, request);
    [HttpPost("{id:guid}/deactivate")]
    public Task<DriverResponse> Deactivate(Guid id, ChangeActivationRequest request) => service.ActivateAsync(id, request.Version, false);
    [HttpPost("{id:guid}/reactivate")]
    public Task<DriverResponse> Reactivate(Guid id, ChangeActivationRequest request) => service.ActivateAsync(id, request.Version, true);
}
