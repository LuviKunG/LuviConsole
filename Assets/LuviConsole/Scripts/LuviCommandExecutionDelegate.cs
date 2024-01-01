using System.Collections.Generic;

namespace LuviKunG.Console
{
    /// <summary>
    /// Delegate function of execution of the command in console.
    /// Using in <see cref="LuviConsole"/> for calling callback function of the command execution.
    /// </summary>
    /// <param name="arguments">Available arguments. The first arguments is always command prefix.</param>
    public delegate void LuviCommandExecutionDelegate(IReadOnlyList<string> arguments);
}