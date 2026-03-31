using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ProductServiceClient _productClient;
    private readonly OrderEventPublisher _publisher;

    public OrdersController(AppDbContext db, ProductServiceClient productClient, OrderEventPublisher publisher)
    {
        _db = db;
        _productClient = productClient;
        _publisher = publisher;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Orders.Include(o => o.Items).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(Order order)
    {
        // Sync: only CHECK stock via HTTP
        foreach (var item in order.Items)
        {
            var hasStock = await _productClient.CheckStockAsync(item.ProductId, item.Quantity);
            if (!hasStock)
                return BadRequest($"Insufficient stock for ProductId {item.ProductId}");
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Async: publish event → ProductService will reduce stock
        var items = order.Items.Select(i => new OrderItemMessage
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity
        }).ToList();

        await _publisher.PublishOrderPlacedAsync(order.Id, items);

        return CreatedAtAction(nameof(GetAll), new { id = order.Id }, order);
    }
}