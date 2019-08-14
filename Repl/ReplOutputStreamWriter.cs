using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Cash.Repl
{
    public class ReplOutputStreamWriter : StreamWriter
    {
        public string Prompt { get; set; } = "> ";
        private readonly StreamWriter _outputWriter;
        private readonly StringBuilder _inputBuffer = new StringBuilder();

        public ReplOutputStreamWriter(Stream outputStream, bool leaveOpen = false) : base(outputStream)
        {
            _outputWriter = new StreamWriter(outputStream, Encoding.UTF8, 2048, leaveOpen);
        }

        public override async Task WriteLineAsync(string value)
        {
            await _outputWriter.WriteAsync('\r');
            if (value.Length < (_inputBuffer.Length + Prompt.Length))
                await _outputWriter.WriteAsync(new string(' ', _inputBuffer.Length + Prompt.Length));
            await _outputWriter.FlushAsync();

            await base.WriteLineAsync(value);
            await base.FlushAsync();
            
            await WriteInputBufferAsync();
        }

        public async Task WriteInputBufferAsync()
        {
            await _outputWriter.WriteAsync("\r");
            await _outputWriter.WriteAsync(Prompt);
            await _outputWriter.WriteAsync(_inputBuffer.ToString());
            await _outputWriter.FlushAsync();
        }

        public async Task SetInputBufferAsync(string buffer)
        {
            var oldLength = _inputBuffer.Length;

            _inputBuffer.Clear();
            _inputBuffer.Append(buffer);

            await _outputWriter.WriteAsync("\r");
            await _outputWriter.WriteAsync(Prompt);

            if (oldLength > _inputBuffer.Length)
            {
                await _outputWriter.WriteAsync(new string(' ', oldLength));
                await _outputWriter.WriteAsync("\r");
                await _outputWriter.WriteAsync(Prompt);
            }

            await _outputWriter.WriteAsync(_inputBuffer.ToString());
            await _outputWriter.FlushAsync();
        }

        public async Task AppendInputBufferAsync(char input)
        {
            _inputBuffer.Append(input);
            await _outputWriter.WriteAsync(input);
            await _outputWriter.FlushAsync();
        }

        public async Task AppendInputBufferAsync(string input)
        {
            _inputBuffer.Append(input);
            await _outputWriter.WriteAsync(input);
            await _outputWriter.FlushAsync();
        }
    }
}