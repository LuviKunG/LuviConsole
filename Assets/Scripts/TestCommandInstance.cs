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

        console.AddCommand("/throw", (args) =>
        {
            throw new System.Exception("Test throwing exception during execute the command.");
        });

        // Add a new command preset of '/foo' that will be show as button below of command input in the group of 'Testing'.
        console.AddCommandPreset("/foo", "Foo", "Just test command of foo", "Testing", false);

        // Add a new command preset of '/test' that will be show as button below of command input in the group of 'Testing' and will execute immediately when pressed.
        console.AddCommandPreset("/test \"Hello World!\"", "Test", "Just test command of test", "Testing", true);

        console.AddCommandPreset("/parse data {\"name\":\"LuviKunG\",\"age\":20} data2 {\"hello\":\"world\"}", "Parse", "Just test command of parse", "Testing", true);
        //console.AddCommandPreset("{\"hello\":\"world\"} \"hello world\"", "Parse 2", "Just test command of parse 2", "Testing", true);
        //console.AddCommandPreset("\"hello world\" {\"hello\":\"world\"} \"yes\"", "Parse 3", "Just test command of parse 3", "Testing", true);

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

        // This is custom log for showing message in screen.
        const float SCREEN_MESSAGE_DURATION = 5.0f;
        console.LogScreen("This is screen message.", SCREEN_MESSAGE_DURATION);
        console.LogScreen("This is screen message with color.", SCREEN_MESSAGE_DURATION, Color.blue);
        console.LogScreen("This is screen message with bold.", SCREEN_MESSAGE_DURATION, true, false);
        console.LogScreen("This is screen message with italic.", SCREEN_MESSAGE_DURATION, false, true);
        console.LogScreen("This is screen message with both of bold and italic.", SCREEN_MESSAGE_DURATION, true, true);
        console.LogScreen("This is screen message with color and bold.", SCREEN_MESSAGE_DURATION, Color.blue, true, false);
        console.LogScreen("This is screen message with color and italic.", SCREEN_MESSAGE_DURATION, Color.blue, false, true);
        console.LogScreen("This is screen message with color and both of bold and italic.", SCREEN_MESSAGE_DURATION, Color.blue, true, true);
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
            LuviConsole.Instance.Log(RandomString(16));
        if (Input.GetKeyDown(KeyCode.V))
            Debug.Log(RandomString(16));
    }

    private string RandomString(int length)
    {
        if (length < 0)
            return string.Empty;
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = 0; i < length; i++)
            builder.Append(chars[Random.Range(0, chars.Length)]);
        return builder.ToString();
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
                data.Add(key, JsonUtility.FromJson<Dictionary<string, object>>(value));
            else if (value.StartsWith("[") && value.EndsWith("]"))
                data.Add(key, JsonUtility.FromJson<List<object>>(value));
            else
                data.Add(key, value);
        }
        return data;
    }
}
