using SimpelTransport.CodeTest.Application.Interfaces;
using SimpelTransport.CodeTest.Infrastructure.Contexts;

namespace SimpelTransport.CodeTest.Workers;

public class OrderWorker(IMessageQueue queue, MyDbContext dbContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var message = await queue.ReceiveMessageAsync();

            if (message == null)
            {
                // Optimize business of machine (and the costs)
                await Task.Delay(1000, cancellationToken);
                continue;
            }

            try
            {
                var order = await dbContext.Orders.FindAsync([message.OrderId], cancellationToken);
                // I do not get the purpose of this???
                // Why queue pending?? to again be pending and to infinite.
                // Maybe in this if you wanted to put processing logic
                // (altho then last line should be there too and maybe bad idea to do so like this
                // with fallback being "Processed order {id}" indicating success even when failed.
                // Will rewrite that part)?
                // if (order is { Status: "Pending" })
                // {
                //     await queue.SendMessageAsync(message);
                //     continue;
                // }

                if (order is null)
                {
                    // log null order and process this somehow
                    continue;
                }

                if (order is { Status: "Completed" })
                {
                    // already processed, nothing else to do
                    // Probably cause of multiple queue times
                    // It is okay without this even as our fallback
                    // is "do nothing" but in real case you would log
                    // in that fallback something like
                    // "You got to this point, meaning we didn't handle you,
                    // meaning you are bad and we need to take a look"
                    continue;
                }

                if (order is { Status: "Pending" })
                {
                    order.Status = "Completed";
                    await dbContext.SaveChangesAsync(cancellationToken);
                    Console.WriteLine($"Processed order {message.OrderId}");
                    continue;
                }

                // log wrong order status or update it or smth like that.
                continue;
            }
            catch (Exception e)
            {
                // I would re-queue message with some "tryNumber" and limit at how many tries is possible
                // after it fails again after max tries, log error and update status to "failed"
                continue;
            }
        }
    }
}
