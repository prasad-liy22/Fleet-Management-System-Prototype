using FleetManagement.Application.Access;
using FleetManagement.Application.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManagement.Api.Controllers;

[ApiController, Route("api/vehicles"), Authorize(Policy = FleetPolicies.ManageFleet)]
public sealed class VehiclesController(IVehicleService service) : ControllerBase
{
    [HttpGet]
    public Task<PageDto<VehicleResponse>> List([FromQuery] MasterQuery query) => service.ListAsync(query);
    [HttpGet("{id:guid}")]
    public Task<VehicleResponse> Get(Guid id, [FromQuery] bool includeDeleted = false) => service.GetAsync(id, includeDeleted);
    [HttpPost]
    public async Task<ActionResult<VehicleResponse>> Create(VehicleRequest request)
    {
        var result = await service.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }
    [HttpPut("{id:guid}")]
    public Task<VehicleResponse> Update(Guid id, UpdateVehicleRequest request) => service.UpdateAsync(id, request);
    [HttpPost("{id:guid}/deactivate")]
    public Task<VehicleResponse> Deactivate(Guid id, ChangeActivationRequest request) => service.ActivateAsync(id, request.Version, false);
    [HttpPost("{id:guid}/reactivate")]
    public Task<VehicleResponse> Reactivate(Guid id, ChangeActivationRequest request) => service.ActivateAsync(id, request.Version, true);
}
