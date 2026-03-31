using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace OrderService.Services;

public class OrderEventPublisher
{
    private readonly ServiceBusSender _sender;

    public OrderEventPublisher(ServiceBusClient client, IConfiguration config)
    {
        var queueName = config["ServiceBus:QueueName"]!;
        _sender = client.CreateSender(queueName);
    }

    public async Task PublishOrderPlacedAsync(int orderId, List<OrderItemMessage> items)
    {
        var message = new
        {
            OrderId = orderId,
            Items = items,
            PlacedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(message);
        var busMessage = new ServiceBusMessage(json);
        await _sender.SendMessageAsync(busMessage);
    }
}

public class OrderItemMessage
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}