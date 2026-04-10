using Microsoft.EntityFrameworkCore;
using SimpelTransport.CodeTest.Application.Dto;
using SimpelTransport.CodeTest.Application.Interfaces;
using SimpelTransport.CodeTest.Domain.Entities;
using SimpelTransport.CodeTest.Infrastructure.Contexts;
using SimpelTransport.CodeTest.Infrastructure.Services;
using SimpelTransport.CodeTest.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<MyDbContext>(
    options => options.UseInMemoryDatabase("TestDb"),
    ServiceLifetime.Singleton
);
builder.Services.AddSingleton<IMessageQueue, MockQueue>();
builder.Services.AddHostedService<OrderWorker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
    var queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();

    var order = new Order
    {
        Id = 1001,
        Status = "Pending",
        Price = 101m,
    };
    db.Orders.Add(order);
    db.SaveChanges();

    await queue.SendMessageAsync(new OrderMessageDto(order.Id));
}

Console.WriteLine("Worker started.");

await host.RunAsync();
