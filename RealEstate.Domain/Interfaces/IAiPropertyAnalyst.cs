using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstate.Domain.Interfaces
{
    public interface IAiPropertyAnalyst
    {
        // ניתוח טקסט חופשי והוצאת מאפיינים (למשל: "מרפסת", "נוף לים")
        Task<IEnumerable<string>> ExtractFeaturesAsync(string description);

        // קבלת ציון התאמה בין תיאור נכס לדרישות לקוח (Matching)
        Task<double> GetMatchScoreAsync(string propertyDetails, string userRequirements);

        // יצירת וקטור (Embedding) לצורך חיפוש סמנטי (RAG)
        Task<float[]> GenerateEmbeddingAsync(string text);
    }
}
