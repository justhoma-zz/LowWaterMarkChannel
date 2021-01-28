using System;
using System.Linq;
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
            await _semaphoreSlim.WaitAsync();

            try
            {
                if (_channel.Reader.Count == 0)
                {
                    var sequences = Array.Empty<int>();

                    if (_loadTask != null)
                    {
                        // The channel is empty but there is an in-flight load task so wait for it to complete
                        sequences = await _loadTask;
                        _loadTask = null;
                    }
                    else
                    {
                        // The channel is empty so get the sequences synchronously
                        sequences = await _loadChannelAction(_capacity);
                    }

                    // Now write all the sequences to the channel
                    foreach (var sequence in sequences)
                    {
                        await _channel.Writer.WriteAsync(sequence);
                    }
                }
                else if (LowWaterMark.HasValue && _channel.Reader.Count == LowWaterMark)
                {
                    // If the low water mark has been supplied and it's hit fire up the load task
                    _loadTask = Task.Run(async () =>
                    {
                        return await _loadChannelAction(_capacity);
                    });
                }

                // Get the sequence
                var currentSequence = await _channel.Reader.ReadAsync();
                Console.WriteLine($"{DateTime.Now:T} Got a sequence {currentSequence}\tReader count: {_channel.Reader.Count}");
                return currentSequence;
            }
            finally
            {
                _ = _semaphoreSlim.Release();
            }
        }
    }
}
