using MediatR;

namespace RealEstate.Application.Properties.Commands;

/// <summary>
/// Backfills real embeddings for properties that were created before the RAG pipeline had a
/// working embeddings service (or whose vector failed to generate at creation time).
/// Returns how many properties were re-indexed.
/// </summary>
public record ReindexPropertyEmbeddingsCommand : IRequest<int>;
