using System.Globalization;
using System.Text.Json;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "API de Clima",
        Description = "Documentação da API de Clima com Swagger"
    });
});


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API de Clima V1");
    c.RoutePrefix = string.Empty;
});

app.MapGet("/v1/climate", async (string latMin, string latMax, string lonMin, string lonMax, string? startDate = null, string? endDate = null) =>
{
    double latMinValue = Convert.ToDouble(latMin, CultureInfo.InvariantCulture);
    double latMaxValue = Convert.ToDouble(latMax, CultureInfo.InvariantCulture);
    double lonMinValue = Convert.ToDouble(lonMin, CultureInfo.InvariantCulture);
    double lonMaxValue = Convert.ToDouble(lonMax, CultureInfo.InvariantCulture);

    if (latMaxValue - latMinValue < 2.0)
    {
        double adjustment = (2.0 - (latMaxValue - latMinValue)) / 2.0;
        latMinValue -= adjustment;
        latMaxValue += adjustment;
    }

    if (lonMaxValue - lonMinValue < 2.0)
    {
        double adjustment = (2.0 - (lonMaxValue - lonMinValue)) / 2.0;
        lonMinValue -= adjustment;
        lonMaxValue += adjustment;
    }

    string latMinStr = latMinValue.ToString(CultureInfo.InvariantCulture);
    string latMaxStr = latMaxValue.ToString(CultureInfo.InvariantCulture);
    string lonMinStr = lonMinValue.ToString(CultureInfo.InvariantCulture);
    string lonMaxStr = lonMaxValue.ToString(CultureInfo.InvariantCulture);

    string formattedDateNow = DateTime.Now.AddDays(-4).ToString("yyyyMMdd");
    string formattedDateLastYear = DateTime.Now.AddYears(-1).ToString("yyyyMMdd");

    string url = $"https://power.larc.nasa.gov/api/temporal/daily/regional?start={startDate ?? formattedDateLastYear}&end={endDate ?? formattedDateNow}" +
                 $"&latitude-min={latMinStr}&latitude-max={latMaxStr}&longitude-min={lonMinStr}&longitude-max={lonMaxStr}" +
                 $"&community=AG&parameters=T2M,T2M_MAX,T2M_MIN,PRECTOTCORR,RH2M,WS2M&format=json&user=DAVE";


    using HttpClient client = new();
    HttpResponseMessage response = await client.GetAsync(url);

    if (!response.IsSuccessStatusCode)
    {
        return Results.Problem("Erro ao chamar API externa");
    }

    var jsonString = await response.Content.ReadAsStringAsync();
    var jsonDoc = JsonDocument.Parse(jsonString);


    var features = jsonDoc.RootElement.GetProperty("features").EnumerateArray();


    var t2m = new Dictionary<string, List<double>>();
    var t2mMin = new Dictionary<string, List<double>>();
    var t2mMax = new Dictionary<string, List<double>>();
    var precTotCorrData = new Dictionary<string, List<double>>();
    var rh2mData = new Dictionary<string, List<double>>();
    var ws2mData = new Dictionary<string, List<double>>();

    foreach (var feature in features)
    {
        var parameters = feature.GetProperty("properties").GetProperty("parameter");

        ProcessData(parameters, "T2M", t2m);
        ProcessData(parameters, "T2M_MIN", t2mMin);
        ProcessData(parameters, "T2M_MAX", t2mMax);
        ProcessData(parameters, "PRECTOTCORR", precTotCorrData);
        ProcessData(parameters, "RH2M", rh2mData);
        ProcessData(parameters, "WS2M", ws2mData);
    }


    Dictionary<string, double> CalculateAverage(Dictionary<string, List<double>> data)
    {
        return data.ToDictionary(
            x => x.Key,
            x => x.Value.Average()
        );
    }


    var averageResults = new
    {
        T2M = CalculateAverage(t2m),
        T2M_MIN = CalculateAverage(t2mMin),
        T2M_MAX = CalculateAverage(t2mMax),
        PRECTOTCORR = CalculateAverage(precTotCorrData),
        RH2M = CalculateAverage(rh2mData),
        WS2M = CalculateAverage(ws2mData)
    };

    return Results.Json(averageResults);
});

app.MapGet("/v2/climate", async (string latMin, string latMax, string lonMin, string lonMax, string? startDate = null, string? endDate = null) =>
{
    double latMinDouble = Convert.ToDouble(latMin, CultureInfo.InvariantCulture);
    double latMaxDouble = Convert.ToDouble(latMax, CultureInfo.InvariantCulture);
    double lonMinDouble = Convert.ToDouble(lonMin, CultureInfo.InvariantCulture);
    double lonMaxDouble = Convert.ToDouble(lonMax, CultureInfo.InvariantCulture);

    double latMinValue = latMinDouble;
    double latMaxValue = latMaxDouble;
    double lonMinValue = lonMinDouble;
    double lonMaxValue = lonMaxDouble;

    if (latMaxValue - latMinValue < 2.0)
    {
        double adjustment = (2.0 - (latMaxValue - latMinValue)) / 2.0;
        latMinValue -= adjustment;
        latMaxValue += adjustment;
    }

    if (lonMaxValue - lonMinValue < 2.0)
    {
        double adjustment = (2.0 - (lonMaxValue - lonMinValue)) / 2.0;
        lonMinValue -= adjustment;
        lonMaxValue += adjustment;
    }

    string latMinStr = latMinValue.ToString(CultureInfo.InvariantCulture);
    string latMaxStr = latMaxValue.ToString(CultureInfo.InvariantCulture);
    string lonMinStr = lonMinValue.ToString(CultureInfo.InvariantCulture);
    string lonMaxStr = lonMaxValue.ToString(CultureInfo.InvariantCulture);

    string formattedDateNow = DateTime.Now.AddDays(-4).ToString("yyyyMMdd");
    string formattedDateLastYear = DateTime.Now.AddYears(-1).ToString("yyyyMMdd");

    string url = $"https://power.larc.nasa.gov/api/temporal/daily/regional?start={startDate ?? formattedDateLastYear}&end={endDate ?? formattedDateNow}" +
                 $"&latitude-min={latMinStr}&latitude-max={latMaxStr}&longitude-min={lonMinStr}&longitude-max={lonMaxStr}" +
                 $"&community=AG&parameters=T2M,T2M_MAX,T2M_MIN,PRECTOTCORR,RH2M,WS2M&format=json&user=DAVE";


    using HttpClient client = new();
    HttpResponseMessage response = await client.GetAsync(url);

    if (!response.IsSuccessStatusCode)
    {
        return Results.Problem("Erro ao chamar API externa");
    }

    var jsonString = await response.Content.ReadAsStringAsync();
    var jsonDoc = JsonDocument.Parse(jsonString);

    var t2m = new Dictionary<string, List<double>>();
    var t2mMin = new Dictionary<string, List<double>>();
    var t2mMax = new Dictionary<string, List<double>>();
    var precTotCorrData = new Dictionary<string, List<double>>();
    var rh2mData = new Dictionary<string, List<double>>();
    var ws2mData = new Dictionary<string, List<double>>();

    foreach (var feature in jsonDoc.RootElement.GetProperty("features").EnumerateArray())
    {
        var geometry = feature.GetProperty("geometry");
        var coordinates = geometry.GetProperty("coordinates").EnumerateArray().ToList();
        double lon = coordinates[0].GetDouble();
        double lat = coordinates[1].GetDouble();

        if (lon >= lonMinDouble && lon <= lonMaxDouble && lat >= latMinDouble && lat <= latMaxDouble)
        {
            var parameters = feature.GetProperty("properties").GetProperty("parameter");

            ProcessData(parameters, "T2M", t2m);
            ProcessData(parameters, "T2M_MIN", t2mMin);
            ProcessData(parameters, "T2M_MAX", t2mMax);
            ProcessData(parameters, "PRECTOTCORR", precTotCorrData);
            ProcessData(parameters, "RH2M", rh2mData);
            ProcessData(parameters, "WS2M", ws2mData);
        }
    }

    Dictionary<string, double> CalculateAverage(Dictionary<string, List<double>> data)
    {
        return data.ToDictionary(
            x => x.Key,
            x => x.Value.Average()
        );
    }

    var averageResults = new
    {
        T2M = CalculateAverage(t2m),
        T2M_MIN = CalculateAverage(t2mMin),
        T2M_MAX = CalculateAverage(t2mMax),
        PRECTOTCORR = CalculateAverage(precTotCorrData),
        RH2M = CalculateAverage(rh2mData),
        WS2M = CalculateAverage(ws2mData)
    };

    return Results.Json(averageResults);
});

app.Run();

static void ProcessData(JsonElement parameters, string parameterName, Dictionary<string, List<double>> dataStore)
{
    if (parameters.TryGetProperty(parameterName, out JsonElement parameterData))
    {
        foreach (var dataPoint in parameterData.EnumerateObject())
        {
            string date = dataPoint.Name;
            double value = dataPoint.Value.GetDouble();

            if (!dataStore.ContainsKey(date))
            {
                dataStore[date] = new List<double>();
            }

            dataStore[date].Add(value);
        }
    }
}