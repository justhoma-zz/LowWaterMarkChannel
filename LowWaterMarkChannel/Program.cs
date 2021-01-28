using System;
using System.Threading;
using System.Threading.Tasks;

namespace LowWaterMarkChannel
{
    internal class Program
    {
        private static async Task Main()
        {
            var fetchMin = 5;
            var fetchMax = 10;
            var nextSequenceProvider = new NextSequenceProvider(fetchMax, fetchMin);
            var cancellationTokenSource = new CancellationTokenSource();

            Console.WriteLine("Application started.");
            Console.WriteLine("Press the ENTER key to cancel...");

            // Add the ability to cancel
            _ = Task.Run(() =>
            {
                while (Console.ReadKey().Key != ConsoleKey.Enter) { }
                cancellationTokenSource.Cancel();
            });

            // Add a new read task every second until cancelled
            await Task.Run(async () =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                    _ = await nextSequenceProvider.ReadAsync();
                }
            });

            Console.WriteLine("Application ending.");
        }
    }
}
