using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static SemaphoreSlim emptySlots = new SemaphoreSlim(5, 5);
    static SemaphoreSlim fullSlots = new SemaphoreSlim(0, 5);
    static Random random = new Random();

    static async Task Main(string[] args)
    {
        BlockingCollection<int> buffer = new BlockingCollection<int>(new ConcurrentQueue<int>());

        Console.CancelKeyPress += async (sender, eventArgs) =>
        {
            Console.WriteLine("Ctrl+C detected. Outputting remaining elements...");

            // Output the remaining elements in the buffer
            buffer.CompleteAdding();

            while (buffer.Count > 0)
            {
                if (buffer.TryTake(out var item))
                {
                    Console.WriteLine($"Remaining element: {item}");
                    await Task.Delay(1000);
                }
            }

            Console.WriteLine("Remaining elements have been taken out. Exiting...");
            Environment.Exit(0);
        };

        var producerTasks = new Task[2];
        for (int i = 0; i < 2; i++)
        {
            producerTasks[i] = Task.Run(() => Producer(buffer));
        }

        var consumerTasks = new Task[3];
        for (int i = 0; i < 3; i++)
        {
            consumerTasks[i] = Task.Run(() => Consumer(buffer));
        }

        await Task.WhenAll(producerTasks.Concat(consumerTasks));

        Console.ReadLine();
    }

    static async Task Producer(BlockingCollection<int> buffer)
    {
        while (true)
        {
            int item = random.Next(1, 100);

            await emptySlots.WaitAsync();
            buffer.Add(item);
            fullSlots.Release();

            await Task.Delay(1000);
        }
    }

    static async Task Consumer(BlockingCollection<int> buffer)
    {
        while (true)
        {
            await fullSlots.WaitAsync();
            int item = buffer.Take();
            emptySlots.Release();

            Console.WriteLine($"Consumed by Task {Task.CurrentId}: {item}");

            await Task.Delay(1500);
        }
    }
}



