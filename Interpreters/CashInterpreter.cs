using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cash.Abstractions;
using Cash.Extensions;

namespace Cash.Interpreters
{
    public class CashInterpreter : IInterpreter
    {
        public async Task<ExecutionContext> ExecuteAsync(Stream inputStream, StreamWriter output, Stream errorStream, CancellationToken cancellationToken)
        {
            var context = new ExecutionContext();
            var shouldExit = false;

            using (var reader = new StreamReader(inputStream))
            {
                while (!shouldExit)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) continue;
                    await output.WriteLineAsync(line);
                    await output.FlushAsync();
                }
            }

            return context;
        }

        private async Task InterpretLine(ExecutionContext context, StringBuilder line, CancellationToken cancellationToken)
        {
            return;
        }

        private InputChar GetInputChar(StreamReader reader)
        {
            bool isControlPressed = false;
            var input = -1;
            while (true)
            {
                input = reader.Read();
                if (input == '^')
                {
                    isControlPressed = true;
                    continue;
                }

                break;
            }

            if (input == -1) return null;
            return new InputChar((char) input, isControlPressed, (input > 32 && input <= 95) || input == 126);
        }

        private class InputChar
        {
            public bool IsControlPressed { get; set; }
            public bool IsShiftPressed { get; set; }
            public char Character { get; set; }

            public InputChar(char character, bool isControlPressed, bool isShiftPressed)
            {
                IsControlPressed = isControlPressed;
                IsShiftPressed = isShiftPressed;
                Character = character;
            }
        }
    }
}