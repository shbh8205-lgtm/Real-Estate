using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using RealEstate.Application.Properties.Commands;
using RealEstate.Application.Properties.Dtos;
using RealEstate.Application.Properties.Queries;

namespace RealEstate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PropertiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PropertiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PropertyDto>>> GetAll()
    {
        var result = await _mediator.Send(new GetPropertiesQuery());
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PropertyDto>> GetById(int id)
    {
        var result = await _mediator.Send(new GetPropertyByIdQuery(id));
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Agent")]
    public async Task<ActionResult<int>> Create([FromBody] CreatePropertyCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    // Backfills real embeddings for properties that don't have a usable vector yet
    // (e.g. created before the RAG pipeline was wired up). Safe to call repeatedly -
    // properties that already have a real embedding are skipped.
    [HttpPost("reindex-embeddings")]
    [Authorize(Roles = "Agent")]
    public async Task<ActionResult<int>> ReindexEmbeddings()
    {
        var updatedCount = await _mediator.Send(new ReindexPropertyEmbeddingsCommand());
        return Ok(new { updatedCount });
    }
}
