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
                    // If the low water mark is null you should see a 3 second pause every 20 items
                    // If the low water mark is 15 there should be no pause
                    lowWaterMark: 15,
                    loadChannelAction: async (batchSize) => await LoadAction(batchSize).ConfigureAwait(false)
                );

            // Add the ability to cancel
            _ = Task.Run(() =>
            {
                Console.WriteLine($"Press the ENTER key to cancel...");
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
            }).ConfigureAwait(false);
        }

        // The code to get a value from the NextSequenceProvider
        private static async Task GetSequenceAsync(NextSequenceProvider nextSequenceProvider)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            var sequence = await nextSequenceProvider.ReadAsync().ConfigureAwait(false);
            Console.WriteLine($"{DateTime.Now:ss:f} Got a sequence {sequence}");
        }

        // The code that returns the 'next' block of sequences
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
