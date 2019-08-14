using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cash.Abstractions;
using Cash.Interpreters;
using Cash.Repl;
using McMaster.Extensions.CommandLineUtils;

namespace Cash
{
    public class Program
    {
        [Argument(0)]
        public string FilePath { get; }

        [Option("-o|--output", "Optional output file", CommandOptionType.SingleValue)]
        public string OutputPath { get; }

        [Option("-d|--debug", "Debug", CommandOptionType.MultipleValue)]
        public DebugCategories[] Debug { get; }

        [HelpOption]
        public bool Help { get; }

        public static Task<int> Main(string[] args) => CommandLineApplication.ExecuteAsync<Program>(args);

        private async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            if (Help)
            {
                return 0;
            }

            using (var cts = new CancellationTokenSource())
            {
                AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => cts.Cancel();
                Console.CancelKeyPress += (sender, eventArgs) => cts.Cancel();
                
                Stream inputStream = null;
                StreamWriter outputStreamWriter = null;
                Stream errorStream  = null;
                try
                {
                    outputStreamWriter = OutputPath != null
                        ? new StreamWriter(File.Open(OutputPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                        : new ReplOutputStreamWriter(Console.OpenStandardOutput());

                    if (outputStreamWriter is ReplOutputStreamWriter replOutputWriter)
                        await replOutputWriter.SetInputBufferAsync("");

                    if (FilePath != null)
                    {
                        inputStream = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }
                    else
                    {
                        inputStream = new ReplInputStream(Debug, (ReplOutputStreamWriter) outputStreamWriter);
                        await ((ReplInputStream)inputStream).StartAsync();
                    }

                    errorStream = Console.OpenStandardError();

                    var interpreter = new CashInterpreter();
                    var context = await interpreter.ExecuteAsync(inputStream, outputStreamWriter, errorStream, cts.Token);

                    return 0;
                }
                catch (Exception e)
                {
                    throw e;
                }
                finally
                {
                    inputStream.Dispose();
                    outputStreamWriter.Dispose();
                    errorStream.Dispose();
                }
            }
        }
    }
}
