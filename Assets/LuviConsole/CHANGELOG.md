# Change Log

## 3.2.0

- Refactor the code to be more readable and maintainable for Unity 6 and above.

## 3.1.0

- Add new feature of displaying log as 'Screen Message'.
  - By using `console.LogScreen(string message, float duration);` to display the message on the screen with duration.
  - Comes with various setting of the colors and style (bold and italic).
  - Able to set showing the screen messages on awake.

## 3.0.1

- Fix script syntax error on Android & iOS.
  - Parameter named `isShowing` aren't renamed to `isShowingConsole`.
- Change the swipe check algorithm.
  - Change from using mouse of `Input.GetKeyDown(0)` to use `Touch touch0 = Input.GetTouch(0)` and get `touch0.phase` instead.
  - Change from using `Input.mousePostion` to use `touch0.position` instead.
- Implementation the toggle on touch screen device by separate the swipe left or right side of the screen.
  - Swipe from the left side of the screen to toggle **Console Window**.
  - Swipe from the right side of the screen to toggle **Preview Window**.

## 3.0.0

- Add new preview window for showing types and amount of logs.
- Add options to toggle show/hide preview window.
- Add options to change preview window anchored position.
- Add options to adjust preview window size.
- Redesign the **LuviConsole** editor inspector to separate configure between console window and preview window.

## 2.7.6

- Fix unable to toggle **LuviConsole** by pressing desire keys while building other build target that didn't from Android or iOS.

## 2.7.5

- Add an exception catch block when executing the command to prevent crash in **LuviConsole**.
- Add various argument exception while adding the command to prevent unexpected behavior.

## 2.7.4

- Add new feature of **Extend UI** to adjust the **LuviConsole** extended UI.
- Fix wrong UI rect adjustment.

## 2.7.3

- Add the device's notch support to display the console in the safe area.

## 2.7.2

- Fix log panel issue that new console message will not show because  message count is over capacity.

## 2.7.1

- Fix the split command function that cannot split JSON string.
  - Using JSON string shouldn't have any space in it.

## 2.7.0

- Add new public function of `ExecuteCommand(string commandString)` for execute the command directly in run-time.
- Improve the editor inspector.
  - Add help URL to the git repository.
  - Able to validate keys to toggle the console.
  - Separate the value of swipe ratio depend of project's target build.
- Optimize the code.

## 2.6.0

- Change the static function of `Log()` to normal function.
  - Because calling the old static function is equal to calling the class instance function.
  - So calling function from instance is a better way. For example:
  ```csharp
  LuviConsole console = LuviConsole.Instance;
  console.Log("This is log");
  ```
- Add various overload function of `Log()` for adding message with color, bold and italic.
- Add various XML tags C# documentation comments for all public functions and fields.
- Remove `LuviConsoleException` class because it doesn't use anymore and  using .NET `System` exception instead.
- Remove many unnecessary functions, property and fields.
- Optimize the code.

## 2.5.2

- Add an example.

## 2.5.1

- Change the fixed key code for toggle **LuviConsole** to able to assign key codes as combo button by user.

## 2.5.0

- Change signature of `AddCommand()` that no longer using name, description, group name and boolean of will execution for create command preset while add new command.
- Separate the preset command information above to new function of `AddCommandPreset()` instead.
- Able to set `swipeRatio` when target build is Android or iOS (Cannot change `swipeRatio` in other target build).
- Add `TestCommandInstance.cs` in the Unity project for example how to create command in **LuviConsole**.
- Fix bugs that IMGUI label skin is dark when hover or active above text with cursor.
- Optimize code for newer Unity Editor version.

## 2.4.3

- namespace **LuviKunG.Console** is required. (`using LuviKunG.Console;`)
- Make `LuviConsole` as plugin manager and separate with **LuviTools** git.
- Remove requirement of `StringBuilderRichTextExtension` and using internal coloring rich text.

## 2.4.2

- Fix changes are not save via Unity Inspector.

## 2.4.1

- New **Command Group** that using for grouping your command.
- New **Execute Command Immediately** option that will execute the command instantly when press the command button.
- Remove internal Rich Text display for Log. But...
- Require extension of `StringBuilderRichTextExtension` to display rich text in Log.
- New **Command Log** to display your executed command in Log.

## 2.4.0

- namespace **LuviKunG** is required. (`using LuviKunG;`)
- WebGL support.
- New drag scroll view on log. (working on all platform)
- New LuviCommand syntax.
    - Now you can use string in your command by using quote "Your string here" to get full string without serparate by space.
    - Fix bugs that execute by double quote and got an error.
- Add realtime update window size and orientation.
- Add `LuviConsoleException` to throw error during execute the command.

## 2.3.6

- Upgrade compatible with Unity version 5, 2018 and 2019.
- Rearrange the inspectator.
- Add new unity instantiate menu on **GameObject > LuviKunG > LuviConsole.**

## Initial Version

- Combine `LuviDebug` and `LuviCommand` into one as `LuviConsole`.