using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.Ml;

namespace RealEstate.Api.Controllers;

public record PriceEstimateRequest(
    string City,
    int Rooms,
    int SizeSqm,
    int Floor,
    int Age,
    bool HasParking,
    bool HasElevator);

public record PriceEstimateResponse(
    decimal EstimatedPrice,
    decimal LowerBound,
    decimal UpperBound,
    IReadOnlyList<string> KnownCities);

[ApiController]
[Route("api/price-estimate")]
[Authorize]
public class PriceEstimateController : ControllerBase
{
    private readonly IPriceEstimator _estimator;

    public PriceEstimateController(IPriceEstimator estimator)
    {
        _estimator = estimator;
    }

    [HttpGet("cities")]
    [AllowAnonymous]
    public ActionResult<IReadOnlyList<string>> GetCities() => Ok(_estimator.KnownCities);

    [HttpPost]
    public ActionResult<PriceEstimateResponse> Estimate([FromBody] PriceEstimateRequest req)
    {
        var input = new PriceEstimateInput(
            req.City, req.Rooms, req.SizeSqm, req.Floor, req.Age,
            req.HasParking, req.HasElevator);

        var result = _estimator.Predict(input);
        var price = Math.Round(result.EstimatedPrice / 1000m) * 1000m;
        var band = price * 0.10m;

        return Ok(new PriceEstimateResponse(
            EstimatedPrice: price,
            LowerBound: Math.Max(0, price - band),
            UpperBound: price + band,
            KnownCities: _estimator.KnownCities));
    }
}
