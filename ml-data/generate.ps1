# Generates synthetic Israeli apartment data for ML.NET Model Builder training.
# Output: ml-data/properties.csv

$ErrorActionPreference = "Stop"
$rng = New-Object System.Random 42

# City -> base price per square meter (ILS), realistic ranges as of recent years.
$cities = @{
    "TelAviv"    = 45000
    "Jerusalem"  = 30000
    "RamatGan"   = 35000
    "Herzliya"   = 40000
    "Haifa"      = 18000
    "PetahTikva" = 24000
    "Netanya"    = 26000
    "BatYam"     = 25000
    "BeerSheva"  = 12000
    "Rishon"     = 26000
}

$rows = @()
$rows += "City,Rooms,SizeSqm,Floor,Age,HasParking,HasElevator,Price"

foreach ($i in 1..300) {
    $cityName = ($cities.Keys | Get-Random -SetSeed ($rng.Next()))
    $basePerSqm = $cities[$cityName]

    $rooms = $rng.Next(2, 7)                    # 2..6 rooms
    $sizeSqm = 35 + $rooms * 18 + $rng.Next(-10, 15)
    if ($sizeSqm -lt 30) { $sizeSqm = 30 }

    $floor = $rng.Next(0, 21)                   # 0..20
    $age = $rng.Next(0, 81)                     # 0..80 years
    $hasParking = $rng.Next(0, 2)
    $hasElevator = if ($floor -ge 4) { 1 } else { $rng.Next(0, 2) }

    # Price formula: base * size, then multiplicative adjustments + Gaussian noise.
    $price = $basePerSqm * $sizeSqm

    # Age depreciation: -0.6% per year, floor at -35%.
    $ageMul = [Math]::Max(0.65, 1.0 - ($age * 0.006))
    $price *= $ageMul

    # Floor: small premium up to floor 8, then plateau.
    $floorMul = 1.0 + [Math]::Min($floor, 8) * 0.012
    $price *= $floorMul

    # Amenities.
    if ($hasParking -eq 1)  { $price *= 1.05 }
    if ($hasElevator -eq 1) { $price *= 1.04 }

    # Gaussian-ish noise +-12%.
    $noise = 1.0 + (($rng.NextDouble() - 0.5) * 0.24)
    $price *= $noise

    $price = [Math]::Round($price / 1000) * 1000  # round to nearest 1k

    $rows += "$cityName,$rooms,$sizeSqm,$floor,$age,$hasParking,$hasElevator,$price"
}

$out = Join-Path $PSScriptRoot "properties.csv"
$rows | Set-Content -Path $out -Encoding UTF8
Write-Output "Wrote $($rows.Count - 1) rows to $out"
