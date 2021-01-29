using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace LowWaterMarkChannel
{
    internal class NextSequenceProvider
    {
        private readonly int _capacity;
        private readonly Func<int, Task<int[]>> _loadChannelAction;
        private readonly Channel<int> _channel;
        private static readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);
        private Task<int[]> _loadTask;

        public int? LowWaterMark { get; set; }

        public NextSequenceProvider(int capacity, Func<int, Task<int[]>> loadChannelAction)
        {
            _capacity = capacity;
            _loadChannelAction = loadChannelAction;

            _channel = Channel.CreateBounded<int>(new BoundedChannelOptions(_capacity)
            {
                SingleWriter = true,
                SingleReader = true
            });
        }

        internal async Task<int> ReadAsync()
        {
            // Prevent concurrent access from multiple threads
            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                // Channel is empty
                if (_channel.Reader.Count == 0)
                {
                    var sequences = Array.Empty<int>();

                    if (_loadTask != null)
                    {
                        // There is an in-flight load task so wait for it to complete
                        sequences = await _loadTask.ConfigureAwait(false);
                        _loadTask = null;
                    }
                    else
                    {
                        // Get the sequences
                        sequences = await _loadChannelAction(_capacity).ConfigureAwait(false);
                    }

                    // Now write all the sequences to the channel
                    foreach (var sequence in sequences)
                    {
                        await _channel.Writer.WriteAsync(sequence).ConfigureAwait(false);
                    }
                }
                // Low water mark has been set and it's hit
                else if (LowWaterMark.HasValue && _channel.Reader.Count == LowWaterMark)
                {
                    _loadTask = Task.Run(async () =>
                    {
                        return await _loadChannelAction(_capacity);
                    });
                }

                // Get and return the sequence
                return await _channel.Reader.ReadAsync().ConfigureAwait(false);
            }
            finally
            {
                _ = _semaphoreSlim.Release();
            }
        }
    }
}
