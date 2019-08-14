using System.Collections.Generic;
using Microsoft.CodeAnalysis.Scripting;

public class ExecutionContext
{
    public IDictionary<string, object> EnvironmentVariables { get; set; }
    public IDictionary<string, Script<int>> Aliases { get; set; }
}