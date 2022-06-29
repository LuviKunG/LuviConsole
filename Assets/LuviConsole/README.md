# LuviConsole
LuviConsole is a console GUI of Unity that include custom command. Created by Thanut Panichyotai (@[LuviKunG]((https://github.com/LuviKunG)))

## How to use?

There are two different way to use.

1. Simply define the `LuviConsole.Instance` in the code where the game is awaking or starting.

```csharp
// Required namespace.
using LuviKunG.Console;

void Awake()
{
    _ = LuviConsole.Instance;
}
```

2. Put the prefab of **LuviConsole** into the most first scene. You can find it in package folder from this path: **Packages/LuviConsole/Resources/LuviKunG/LuviConsole.prefab**

## How to create command?

Create a new component class and attach this class into the **LuviConsole** GameObject.
Inside the component class, you can add your new command by using these functions.

```csharp
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
}
```