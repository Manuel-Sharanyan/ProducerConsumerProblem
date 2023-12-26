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

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            Console.WriteLine("Ctrl+C detected. Exiting...");

            buffer.CompleteAdding();

            while (buffer.Count > 0)
            {
                if (buffer.TryTake(out _))
                {
                    Thread.Sleep(1000);
                }
            }

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

            Thread.Sleep(1000);
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

            Thread.Sleep(1500);
        }
    }
}

