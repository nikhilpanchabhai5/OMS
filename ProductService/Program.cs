using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Service Bus
builder.Services.AddSingleton(new ServiceBusClient(
    builder.Configuration["ServiceBus:ConnectionString"]));

// Background service that listens to queue
builder.Services.AddHostedService<OrderPlacedConsumer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    db.Database.EnsureCreated();
//}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var retries = 10;
    while (retries > 0)
    {
        try
        {
            db.Database.EnsureCreated();
            break;
        }
        catch (Exception ex)
        {
            retries--;
            Console.WriteLine($"DB not ready, retrying... ({retries} left). {ex.Message}");
            Thread.Sleep(3000);
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


//this comment is for running pipeline without changes, ignore it