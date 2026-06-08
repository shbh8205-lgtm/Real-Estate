using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using RealEstate.Domain.Interfaces;
using RealEstate.Domain.Entities;

namespace RealEstate.Application.Properties.Commands;

public class CreatePropertyHandler : IRequestHandler<CreatePropertyCommand, int>
{
    private readonly IAsyncRepository<Property> _repository;
    private readonly IAiPropertyAnalyst _aiAnalyst;

    public CreatePropertyHandler(IAsyncRepository<Property> repository, IAiPropertyAnalyst aiAnalyst)
    {
        _repository = repository;
        _aiAnalyst = aiAnalyst;
    }

    public async Task<int> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        // 1. שימוש ב-AI (גם אם הוא מחזיר דאטה קבוע כרגע, הלוגיקה מוכנה!)
        var features = await _aiAnalyst.ExtractFeaturesAsync(request.Description);
        var vector = await _aiAnalyst.GenerateEmbeddingAsync(request.Description);

        // 2. יצירת הישות עם הנתונים שהתקבלו
        var property = new Property
        {
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            Address = request.Address,
            // כאן אנחנו שומרים את מה שה-AI הפיק
            Tags = string.Join(",", features),
            DescriptionVector = vector // זה ישמש אותנו ל-Vector Search באפיון
        };

        // 3. שמירה בבסיס הנתונים דרך ה-Repository
        await _repository.AddAsync(property);

        return property.Id;
    }
}