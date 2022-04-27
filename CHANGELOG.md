# Changelog

You can get the older version by package manager in here.

## Released

### v2.5.0 April 27th 2022 - Latest

`https://github.com/LuviKunG/LuviConsole.git#2.5.0`

- Change signature of `AddCommand()` that no longer using name, description, group name and boolean of will execution for create command preset while add new command.
- Separate the preset command information above to new function of `AddCommandPreset()` instead.
- Able to set `swipeRatio` when target build is Android or iOS (Cannot change `swipeRatio` in other target build).
- Add `TestCommandInstance.cs` in the Unity project for example how to create command in **LuviConsole**.
- Fix bugs that IMGUI label skin is dark when hover or active above text with cursor.
- Optimize code for newer Unity Editor version.

### v2.4.3 August 23th 2019

`https://github.com/LuviKunG/LuviConsole.git#2.4.3`

- namespace **LuviKunG.Console** is required. (```using LuviKunG.Console;```)
- Make ```LuviConsole``` as plugin manager and separate with **LuviTools** git.
- Remove requirement of ```StringBuilderRichTextExtension``` and using internal coloring rich text.

## Depreceted

### v2.4.2

- Fix changes are not save via Unity Inspector.

### v2.4.1

- New **Command Group** that using for grouping your command.
- New **Execute Command Immediately** option that will execute the command instantly when press the command button.
- Remove internal Rich Text display for Log. But...
- Require extension of ```StringBuilderRichTextExtension``` to display rich text in Log.
- New **Command Log** to display your executed command in Log.

### v2.4.0

- namespace **LuviKunG** is required. (```using LuviKunG;```)
- WebGL support.
- New drag scroll view on log. (working on all platform)
- New LuviCommand syntax.
    - Now you can use string in your command by using quote "Your string here" to get full string without serparate by space.
    - Fix bugs that execute by double quote and got an error.
- Add realtime update window size and orientation.
- Add ```LuviConsoleException``` to throw error during execute the command.

### v2.3.6

- Upgrade compatible with Unity version 5, 2018 and 2019.
- Rearrange the inspectator.
- Add new unity instantiate menu on **GameObject > LuviKunG > LuviConsole.**

## Initial Version

- Combine ```LuviDebug``` and ```LuviCommand``` into one as ```LuviConsole```.