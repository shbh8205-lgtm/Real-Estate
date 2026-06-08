namespace RealEstate.Application.Ml;

public record PriceEstimateInput(
    string City,
    int Rooms,
    int SizeSqm,
    int Floor,
    int Age,
    bool HasParking,
    bool HasElevator);

public record PriceEstimateResult(decimal EstimatedPrice);

public interface IPriceEstimator
{
    PriceEstimateResult Predict(PriceEstimateInput input);
    IReadOnlyList<string> KnownCities { get; }
}
