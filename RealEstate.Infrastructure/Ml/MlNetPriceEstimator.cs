using Microsoft.ML;
using Microsoft.ML.Data;
using RealEstate.Application.Ml;

namespace RealEstate.Infrastructure.Ml;

public class MlNetPriceEstimator : IPriceEstimator
{
    private readonly PredictionEngine<ModelInput, ModelOutput> _engine;
    private readonly object _lock = new();

    public IReadOnlyList<string> KnownCities { get; } = new[]
    {
        "TelAviv", "Jerusalem", "RamatGan", "Herzliya", "Haifa",
        "PetahTikva", "Netanya", "BatYam", "BeerSheva", "Rishon"
    };

    public MlNetPriceEstimator()
    {
        var modelPath = Path.Combine(AppContext.BaseDirectory, "Ml", "PriceModel.mlnet");
        var mlContext = new MLContext();
        var model = mlContext.Model.Load(modelPath, out _);
        _engine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(model);
    }

    public PriceEstimateResult Predict(PriceEstimateInput input)
    {
        var mlInput = new ModelInput
        {
            City = input.City,
            Rooms = input.Rooms,
            SizeSqm = input.SizeSqm,
            Floor = input.Floor,
            Age = input.Age,
            HasParking = input.HasParking ? 1f : 0f,
            HasElevator = input.HasElevator ? 1f : 0f,
        };

        ModelOutput output;
        lock (_lock)
        {
            output = _engine.Predict(mlInput);
        }
        return new PriceEstimateResult((decimal)output.Score);
    }

    private class ModelInput
    {
        [ColumnName("City")] public string City { get; set; } = string.Empty;
        [ColumnName("Rooms")] public float Rooms { get; set; }
        [ColumnName("SizeSqm")] public float SizeSqm { get; set; }
        [ColumnName("Floor")] public float Floor { get; set; }
        [ColumnName("Age")] public float Age { get; set; }
        [ColumnName("HasParking")] public float HasParking { get; set; }
        [ColumnName("HasElevator")] public float HasElevator { get; set; }
        [ColumnName("Price")] public float Price { get; set; }
    }

    private class ModelOutput
    {
        [ColumnName("Score")] public float Score { get; set; }
    }
}
