using FleetManagement.Application.Access;
using FleetManagement.Application.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManagement.Api.Controllers;

[ApiController, Route("api/customers"), Authorize(Policy = FleetPolicies.ManageFleet)]
public sealed class CustomersController(ICustomerService service) : ControllerBase
{
    [HttpGet]
    public Task<PageDto<CustomerResponse>> List([FromQuery] MasterQuery query) => service.ListAsync(query);
    [HttpGet("{id:guid}")]
    public Task<CustomerResponse> Get(Guid id, [FromQuery] bool includeDeleted = false) => service.GetAsync(id, includeDeleted);
    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(CustomerRequest request)
    {
        var result = await service.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }
    [HttpPut("{id:guid}")]
    public Task<CustomerResponse> Update(Guid id, UpdateCustomerRequest request) => service.UpdateAsync(id, request);
    [HttpPost("{id:guid}/deactivate")]
    public Task<CustomerResponse> Deactivate(Guid id, ChangeActivationRequest request) => service.ActivateAsync(id, request.Version, false);
    [HttpPost("{id:guid}/reactivate")]
    public Task<CustomerResponse> Reactivate(Guid id, ChangeActivationRequest request) => service.ActivateAsync(id, request.Version, true);
}
