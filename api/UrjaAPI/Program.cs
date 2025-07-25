// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
#pragma warning disable SA1124 // Do not use regions
#region CodingObservation relevant to core framework

// manually added the controllers folder , and the controller class
#endregion

builder.Services.AddControllers(options =>
{
    options.ReturnHttpNotAcceptable=true;
}).AddXmlDataContractSerializerFormatters();

#pragma warning restore SA1124 // Do not use regions
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// configuring the problem details
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions.Add("ServerName", Environment.MachineName);
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

#pragma warning disable SA1124 // Do not use regions
#region CodingObservation relevant to core framework
// warning ASP0014: Suggest using top level route registrations instead of UseEndpoints (https://aka.ms/aspnet/analyzers) [D:\L2E\binaryJuiceLabs\UrjaBroker\api\UrjaAPI\UrjaAPI.
//csproj]
//app.UseRouting();
//app.UseEndpoints(endpoints =>endpoints.MapControllers());
#endregion
app.MapControllers();
#pragma warning restore SA1124 // Do not use regions

#region defaultCodeCommented

//var summaries = new[]
//{
//    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
//};

//app.MapGet("/weatherforecast", () =>
//{
//    var forecast =  Enumerable.Range(1, 5).Select(index =>
//        new WeatherForecast
//        (
//            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//            Random.Shared.Next(-20, 55),
//            summaries[Random.Shared.Next(summaries.Length)]
//        ))
//        .ToArray();
//    return forecast;
//})
//.WithName("GetWeatherForecast")
//.WithOpenApi();
#endregion

app.Run();

//record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
//{
//    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
//}
