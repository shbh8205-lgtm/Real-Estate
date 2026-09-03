using System;

namespace RealEstate.Application.Chat
{
    /// <summary>
    /// Small helper for comparing embedding vectors. This is the "retrieval" half of RAG:
    /// given a query vector and a set of document vectors (property description embeddings),
    /// rank the documents by semantic similarity to the query.
    /// </summary>
    public static class VectorMath
    {
        /// <summary>
        /// Cosine similarity between two vectors, in the range [-1, 1] (higher = more similar).
        /// Returns 0 if either vector is missing/empty/mismatched, so callers can safely treat
        /// properties that don't have a (valid) embedding yet as "no signal" instead of crashing.
        /// </summary>
        public static double CosineSimilarity(float[]? a, float[]? b)
        {
            if (a is null || b is null || a.Length == 0 || b.Length == 0 || a.Length != b.Length)
            {
                return 0d;
            }

            double dot = 0, normA = 0, normB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA == 0 || normB == 0)
            {
                return 0d;
            }

            return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        /// <summary>
        /// A vector is only usable for retrieval if it's non-empty and not all zeros
        /// (the mocked implementation used to return new float[1536], which is a degenerate
        /// "no embedding" placeholder rather than real content).
        /// </summary>
        public static bool IsUsable(float[]? vector)
        {
            if (vector is null || vector.Length == 0) return false;
            for (int i = 0; i < vector.Length; i++)
            {
                if (vector[i] != 0f) return true;
            }
            return false;
        }
    }
}
