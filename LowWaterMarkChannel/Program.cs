using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LowWaterMarkChannel
{
    internal class Program
    {
        private static async Task Main()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var nextSequenceProvider = new NextSequenceProvider
                (
                    capacity: 20,
                    loadChannelAction: async (batchSize) => await LoadAction(batchSize).ConfigureAwait(false)
                );

            // If you don't supply the low water mark you should see a 3 second pause every 20 items
            // If you set the low water mark there should be no pause
            nextSequenceProvider.LowWaterMark = 15;

            // Add the ability to cancel
            _ = Task.Run(() =>
            {
                Console.WriteLine($"Low Water Mark Enabled: {nextSequenceProvider.LowWaterMark.HasValue}. Press the ENTER key to cancel...");
                while (Console.ReadKey().Key != ConsoleKey.Enter) { }
                cancellationTokenSource.Cancel();
            });

            // Add 5 get tasks every second until cancelled
            await Task.Run(async () =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    var tasks = new List<Task>();
                    for (var i = 0; i < 5; i++)
                    {
                        tasks.Add(GetSequenceAsync(nextSequenceProvider));
                    }
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
            });
        }

        // The code that will wait 1 second and then read a value
        private static async Task GetSequenceAsync(NextSequenceProvider nextSequenceProvider)
        {
            await Task.Delay(1000);
            var sequence = await nextSequenceProvider.ReadAsync().ConfigureAwait(false);
            Console.WriteLine($"{DateTime.Now:ss:f} Got a sequence {sequence}");
        }

        // The code to will wait 3 seconds then return a block of sequences
        private static int _nextSequenceStartingValue = 1;
        private static async Task<int[]> LoadAction(int batchSize)
        {
            await Task.Delay(3000).ConfigureAwait(false);
            var returnValue = Enumerable.Range(_nextSequenceStartingValue, batchSize);
            _nextSequenceStartingValue += batchSize;
            return returnValue.ToArray();
        }
    }
}
