using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using System.Text.Json;

namespace ProductService.Services;

public class OrderPlacedConsumer : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(ServiceBusClient client, IConfiguration config,
        IServiceScopeFactory scopeFactory, ILogger<OrderPlacedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _processor = client.CreateProcessor(config["ServiceBus:QueueName"]!);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor.ProcessMessageAsync += OnMessageReceived;
        _processor.ProcessErrorAsync += OnError;
        await _processor.StartProcessingAsync(stoppingToken);
    }

    private async Task OnMessageReceived(ProcessMessageEventArgs args)
    {
        var json = args.Message.Body.ToString();
        _logger.LogInformation("Received message: {Message}", json);

        var order = JsonSerializer.Deserialize<OrderPlacedMessage>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (order is null) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var item in order.Items)
        {
            var product = await db.Products.FindAsync(item.ProductId);
            if (product is not null && product.Stock >= item.Quantity)
            {
                product.Stock -= item.Quantity;
                _logger.LogInformation("Reduced stock for ProductId {Id} by {Qty}",
                    item.ProductId, item.Quantity);
            }
        }

        await db.SaveChangesAsync();
        await args.CompleteMessageAsync(args.Message);
    }

    private Task OnError(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus error");
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.StopProcessingAsync(cancellationToken);
        await _processor.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}

public class OrderPlacedMessage
{
    public int OrderId { get; set; }
    public List<OrderItemMessage> Items { get; set; } = new();
}

public class OrderItemMessage
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}