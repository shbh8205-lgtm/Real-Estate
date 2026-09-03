using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RealEstate.Application.Chat;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Interfaces;

namespace RealEstate.Application.Properties.Commands;

public class ReindexPropertyEmbeddingsHandler : IRequestHandler<ReindexPropertyEmbeddingsCommand, int>
{
    private readonly IAsyncRepository<Property> _repository;
    private readonly IAiPropertyAnalyst _aiAnalyst;

    public ReindexPropertyEmbeddingsHandler(IAsyncRepository<Property> repository, IAiPropertyAnalyst aiAnalyst)
    {
        _repository = repository;
        _aiAnalyst = aiAnalyst;
    }

    public async Task<int> Handle(ReindexPropertyEmbeddingsCommand request, CancellationToken cancellationToken)
    {
        var properties = await _repository.ListAllAsync();
        var updated = 0;

        foreach (var property in properties)
        {
            if (VectorMath.IsUsable(property.DescriptionVector))
            {
                continue; // already has a real embedding - skip to save API calls
            }

            var textToEmbed = string.IsNullOrWhiteSpace(property.Description)
                ? property.Title
                : property.Description;

            var vector = await _aiAnalyst.GenerateEmbeddingAsync(textToEmbed);
            if (!VectorMath.IsUsable(vector))
            {
                continue; // embedding service unavailable/misconfigured - leave for a later retry
            }

            property.DescriptionVector = vector;
            await _repository.UpdateAsync(property);
            updated++;
        }

        return updated;
    }
}
