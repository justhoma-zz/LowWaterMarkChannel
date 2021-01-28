using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LowWaterMarkChannel
{
    internal class Program
    {
        private static async Task Main()
        {
            var nextSequenceProvider = new NextSequenceProvider
                (
                    capacity: 10,
                    loadChannelAction: async (batchSize) => await LoadAction(batchSize).ConfigureAwait(false)
                );

            // If you don't set the low water mark you should see a 5 second pause every 10 items 
            // If you set the low water mark there should be no pause
            //nextSequenceProvider.LowWaterMark = 5;

            var cancellationTokenSource = new CancellationTokenSource();

            // Add the ability to cancel
            _ = Task.Run(() =>
            {
                Console.WriteLine("Press the ENTER key to cancel...");
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
        }

        // The code to pause for 5 seconds and then return a block of sequences
        private static int _nextSequenceStartingValue = 1;
        private static async Task<int[]> LoadAction(int batchSize)
        {
            await Task.Delay(5000).ConfigureAwait(false);
            var returnValue = Enumerable.Range(_nextSequenceStartingValue, batchSize).ToArray();
            _nextSequenceStartingValue += batchSize;
            return returnValue;
        }
    }
}
