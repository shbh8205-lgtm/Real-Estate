using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using RealEstate.Application.Leads.Commands;
using RealEstate.Application.Leads.Dtos;
using RealEstate.Application.Leads.Queries;

namespace RealEstate.Api.Controllers;

public record CreateLeadRequest(int PropertyId, string Message);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeadsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LeadsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateLeadRequest request)
    {
        var clientIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(clientIdClaim, out var clientId))
            return Unauthorized();

        var command = new CreateLeadCommand(request.PropertyId, clientId, request.Message);
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpGet]
    [Authorize(Roles = "Agent")]
    public async Task<ActionResult<IReadOnlyList<LeadDto>>> GetAll()
    {
        var result = await _mediator.Send(new GetLeadsQuery());
        return Ok(result);
    }
}
