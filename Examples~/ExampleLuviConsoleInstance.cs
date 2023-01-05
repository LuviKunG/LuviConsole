using UnityEngine;
using LuviKunG.Console;

namespace LuviKunG.Examples
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LuviConsole))]
    public class ExampleLuviConsoleInstance : MonoBehaviour
    {
        private void Awake()
        {
            // Create or get instance of LuviConsole.
            LuviConsole console = LuviConsole.Instance;

            // Add a new command of '/foo' that will result debug log of 'bar!'
            console.AddCommand("/foo", (args) =>
            {
                Debug.Log("bar!");
            });

            // Add a new command of '/test' that require arguments and will log all available arguments.
            console.AddCommand("/test", (args) =>
            {
                Debug.Log("test");
                for (int i = 0; i < args.Count; ++i)
                    Debug.Log($"args[{i}] = " + args[i]);
            });

            // Add a new command of '/throw' that will cause an exception for testing.
            console.AddCommand("/throw", (args) =>
            {
                // You can throw an exception like this and it will not crash the console.
                throw new System.Exception("Test throwing exception during execute the command.");
            });

            // Add a new command preset of '/foo' that will be show as button below of command input in the group of 'Testing'.
            console.AddCommandPreset("/foo", "Foo", "Just test command of foo", "Testing", false);

            // Add a new command preset of '/test' that will be show as button below of command input in the group of 'Testing' and will execute immediately when pressed.
            console.AddCommandPreset("/test \"Hello World!\"", "Test", "Just test command of test", "Testing", true);

            // Add a new command preset of '/throw' that will be show as button below of command input in the group of 'Testing' and will execute immediately when pressed.
            console.AddCommandPreset("/throw", "Throw", "Just test command of throw", "Testing", true);

            // This is custom log for putting message in console directly without receive debug log.
            console.Log("This is custom log");
            console.Log("This is custom log with color", Color.blue);
            console.Log("This is custom log with bold", true, false);
            console.Log("This is custom log with italic", false, true);
            console.Log("This is custom log with both of bold and italic", true, true);
            console.Log("This is custom log with color and bold", Color.blue, true, false);
            console.Log("This is custom log with color and italic", Color.blue, false, true);
            console.Log("This is custom log with color and both of bold and italic", Color.blue, true, true);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            gameObject.name = nameof(LuviConsole);

            LuviConsole console = LuviConsole.Instance;
            console.ExecuteCommand("/foo");
            console.ExecuteCommand("/test Arg1 Arg2 Arg3 Arg4 Arg5");
        }
#endif
    }
}
