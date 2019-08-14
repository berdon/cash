using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cash.Abstractions
{
    public interface IInterpreter
    {
        Task<ExecutionContext> ExecuteAsync(Stream inputStream, StreamWriter outputStream, Stream errorStream, CancellationToken cancellationToken = default);
    }
}