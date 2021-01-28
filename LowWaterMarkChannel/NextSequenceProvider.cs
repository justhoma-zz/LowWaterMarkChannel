using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace LowWaterMarkChannel
{
    internal class NextSequenceProvider
    {
        private readonly int _fetchMax;
        private readonly int _fetchMin;
        private readonly Channel<int> _channel;
        private Task<int[]> _loadTask;
        private static readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);

        public NextSequenceProvider(int fetchMax, int fetchMin)
        {
            _fetchMax = fetchMax;
            _fetchMin = fetchMin; 
            
            _channel = Channel.CreateBounded<int>(new BoundedChannelOptions(_fetchMax)
            {
                SingleWriter = true,
                SingleReader = true
            });
        }

        internal async Task<int> ReadAsync()
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                if (_channel.Reader.Count == 0)
                {
                    var sequences = Array.Empty<int>();

                    if (_loadTask != null)
                    {
                        // The channel is empty but there is an inflight load task so wait for it to complete
                        sequences = await _loadTask;
                        _loadTask = null;
                    }
                    else
                    {
                        // The channel is empty so get the sequences synchronously
                        sequences = await GetSequencesAsync(_fetchMax);
                    }

                    foreach (var sequence in sequences)
                    {
                        await _channel.Writer.WriteAsync(sequence);
                    }
                }
                else if (_channel.Reader.Count == _fetchMin)
                {
                    // The channel has hit the low water mark so fire up a task to load it
                    _loadTask = Task.Run(async () =>
                    {
                        return await GetSequencesAsync(_fetchMax);
                    });
                }

                var currentSequence = await _channel.Reader.ReadAsync();
                Console.WriteLine($"{DateTime.Now:T} Got a sequence {currentSequence}");
                return currentSequence;
            }
            finally
            {
                _ = _semaphoreSlim.Release();
            }
        }

        // Dummy SP call code
        private static int _nextSequenceStartingValue = 1;
        private static async Task<int[]> GetSequencesAsync(int batchSize)
        {
            await Task.Delay(5000).ConfigureAwait(false);
            var returnValue = Enumerable.Range(_nextSequenceStartingValue, batchSize).ToArray();
            _nextSequenceStartingValue += batchSize;
            return returnValue;
        }
    }
}
