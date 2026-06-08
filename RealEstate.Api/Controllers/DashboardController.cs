using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using RealEstate.Application.Dashboard;

namespace RealEstate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Agent")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get()
    {
        var result = await _mediator.Send(new GetDashboardQuery());
        return Ok(result);
    }
}
