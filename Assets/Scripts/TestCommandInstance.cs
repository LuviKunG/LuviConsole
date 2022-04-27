using UnityEngine;
// Required namespace.
using LuviKunG.Console;

public class TestCommandInstance : MonoBehaviour
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
            for (int i = 0; i < args.Count; ++i)
                Debug.Log($"args[{i}] = " + args[i]);
        });

        // Add a new command preset of '/foo' that will be show as button below of command input in the group of 'Testing'.
        console.AddCommandPreset("/foo", "Foo", "Just test command of foo", "Testing", false);
    }

    private void Start()
    {
        Debug.Log("This is 'Debug.Log()'");
        Debug.LogWarning("This is 'Debug.LogWarning()'");
        Debug.LogError("This is 'Debug.LogError()'");
        Debug.LogException(new System.Exception("This is 'Debug.LogException()'"));
    }
}
