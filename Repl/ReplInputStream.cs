using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cash.Abstractions;
using Cash.Extensions;

namespace Cash.Repl
{
    public class ReplInputStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _buffer.Count;

        public override long Position { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        private readonly ReplOutputStreamWriter _outputWriter;
        private readonly DebugCategories[] _debug;
        private readonly SemaphoreSlim _bufferLock = new SemaphoreSlim(1, 1);
        private readonly Queue<char> _buffer = new Queue<char>();
        private readonly StringBuilder _lineBuffer = new StringBuilder();
        private bool _shouldStop = false;
        private bool _disposed = false;
        private List<string> _lineHistory = new List<string>();
        private int _historyIndex = 0;

        public ReplInputStream(DebugCategories[] debug, ReplOutputStreamWriter outputWriter)
        {
            _debug = debug;
            _outputWriter = outputWriter;
        }

        public async Task StartAsync(CancellationToken ct = default)
        {
            var _ = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested && !_shouldStop)
                {
                    var key = Console.ReadKey(true);

                    if (key.KeyChar == -1)
                    {
                        await Task.Delay(10);
                    }

                    if (_debug?.Contains(DebugCategories.Input) == true)
                    {
                        Console.WriteLine($"Char: '{(int)key.KeyChar}', ConsoleKey: '{key.Key}', Modifiers: '{key.Modifiers}'");
                    }

                    if (key.KeyChar != 0)
                    {
                        if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                        {
                            await AddPendingAsync('^', ct);
                        }

                        await AddPendingAsync(key.KeyChar, ct);
                    }
                    else
                    {
                        switch (key.Key)
                        {
                            case ConsoleKey.LeftArrow:
                                await AddPendingAsync("^[[D", ct);
                                break;
                        }
                    }
                }
            }, ct);
        }

        public override void Flush()
        {
            throw new System.NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _bufferLock.WaitAsync();

            try {
                if (_buffer.Count == 0) return 0;

                var charactersToWrite = Math.Min(Math.Min(buffer.Length - offset, count), _buffer.Count);
                for (var i = 0; i < charactersToWrite; i++)
                {
                    buffer[i + offset] = (byte)_buffer.Dequeue();
                }

                return charactersToWrite;
            }
            finally
            {
                _bufferLock.Release();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new System.NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        private async Task ProcessPendingCharacterAsync(char c, CancellationToken ct)
        {
            switch (c)
            {
                case (char)13:
                    var line = _lineBuffer.ToString();
                    _lineBuffer.Clear();

                    foreach (var c1 in line)
                    {
                        _buffer.Enqueue(c1);
                    }

                    _buffer.Enqueue('\r');
                    _buffer.Enqueue('\n');

                    _lineHistory.Add(line);
                    await _outputWriter.SetInputBufferAsync("");

                    break;
                case (char)8:
                case (char)127:
                    if (_lineBuffer.Length == 0) return;

                    _lineBuffer.Remove(_lineBuffer.Length - 1, 1);

                    await _outputWriter.SetInputBufferAsync(_lineBuffer.ToString());
                    break;
                default:
                    _lineBuffer.Append(c);
                    await _outputWriter.AppendInputBufferAsync(c);
                    break;
            }

            await _outputWriter.FlushAsync();
        }

        private async Task AddPendingAsync(char input, CancellationToken ct)
        {
            await _bufferLock.WaitAsync();
            await ProcessPendingCharacterAsync(input, ct);
            _bufferLock.Release();
        }

        private async Task AddPendingAsync(string input, CancellationToken ct)
        {
            await _bufferLock.WaitAsync();
            foreach (var c in input)
            {
                await ProcessPendingCharacterAsync(c, ct);
            }
            _bufferLock.Release();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _shouldStop = true;

                _bufferLock.Dispose();
            }

            _disposed = true;
        }
    }
}