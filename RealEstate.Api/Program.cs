using Microsoft.EntityFrameworkCore;
using RealEstate.Infrastructure.Persistence;
using MediatR;
using RealEstate.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using RealEstate.Infrastructure.Repositories;
using RealEstate.Infrastructure.Services;
using RealEstate.Application.Ml;
using RealEstate.Infrastructure.Ml;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(
            new RealEstate.Api.UtcDateTimeConverter());
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.MigrationsAssembly("RealEstate.Infrastructure")));

// 1.AI Analyst
builder.Services.AddScoped<IAiPropertyAnalyst, OpenAiPropertyAnalyst>();

// 2.Repository
//   Repository  <T>,   :
builder.Services.AddScoped(typeof(IAsyncRepository<>), typeof(EfRepository<>));

// DI
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<ILeadRepository, LeadRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();

// Singleton: PredictionEngine is expensive to build. Wrapper locks around Predict() since the engine is not thread-safe.
builder.Services.AddSingleton<IPriceEstimator, MlNetPriceEstimator>();

// במקום הרישום הרגיל, אנחנו מוסיפים תמיכה ב-HttpClient עבור השירות הזה
builder.Services.AddHttpClient<IAiPropertyAnalyst, OpenAiPropertyAnalyst>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });


// 3.Properties ( IPropertyRepository)
// builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();

//      -Handlers   -Application
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RealEstate.Application.Properties.Commands.CreatePropertyCommand).Assembly));

builder.Services.AddMemoryCache();

// CORS for Angular dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AngularDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
