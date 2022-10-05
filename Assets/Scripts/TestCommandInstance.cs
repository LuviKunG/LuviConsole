using UnityEngine;
// Required namespace.
using LuviKunG.Console;
using System.Collections.Generic;

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
        
        console.AddCommand("/parse", (args) =>
        {
            string[] dataArgs = new string[args.Count - 1];
            for (int i = 1; i < args.Count; i++)
            {
                console.Log(args[i], Color.green);
                dataArgs[i - 1] = args[i];
            }
            object variant = CreateVariantFromStringArray(dataArgs);
            Debug.Log(variant);
        });

        // Add a new command preset of '/foo' that will be show as button below of command input in the group of 'Testing'.
        console.AddCommandPreset("/foo", "Foo", "Just test command of foo", "Testing", false);

        // Add a new command preset of '/test' that will be show as button below of command input in the group of 'Testing' and will execute immediately when pressed.
        console.AddCommandPreset("/test \"Hello World!\"", "Test", "Just test command of test", "Testing", true);

        console.AddCommandPreset("/parse data {\"name\":\"LuviKunG\",\"age\":20} data2 {\"hello\":\"world\"}", "Parse", "Just test command of parse", "Testing", true);
        //console.AddCommandPreset("{\"hello\":\"world\"} \"hello world\"", "Parse 2", "Just test command of parse 2", "Testing", true);
        //console.AddCommandPreset("\"hello world\" {\"hello\":\"world\"} \"yes\"", "Parse 3", "Just test command of parse 3", "Testing", true);

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

    private void Start()
    {
        Debug.Log("This is 'Debug.Log()'");
        Debug.LogWarning("This is 'Debug.LogWarning()'");
        Debug.LogError("This is 'Debug.LogError()'");
        Debug.LogException(new System.Exception("This is 'Debug.LogException()'"));

        LuviConsole console = LuviConsole.Instance;
        console.ExecuteCommand("/foo");
        console.ExecuteCommand("/test Arg1 Arg2 Arg3 Arg4 Arg5");
    }

    private object CreateVariantFromStringArray(params string[] args)
    {
        if (args.Length % 2 != 0)
            throw new System.ArgumentException("Cannot use argument that isn't even.");
        var data = new Dictionary<string, object>();
        for (int i = 0; i < args.Length; i += 2)
        {
            var key = args[i];
            var value = args[i + 1];
            if (int.TryParse(value, out int valueInt))
                data.Add(key, (long)valueInt);
            else if (float.TryParse(value, out float valueFloat))
                data.Add(key, (double)valueFloat);
            else if (bool.TryParse(value, out bool valueBool))
                data.Add(key, valueBool);
            else if (value.StartsWith("{") && value.EndsWith("}"))
            {
                Debug.Log(value);
                data.Add(key, JsonUtility.FromJson<Dictionary<string, object>>(value));
            }
            else
                data.Add(key, value);
        }
        return data;
    }
}
