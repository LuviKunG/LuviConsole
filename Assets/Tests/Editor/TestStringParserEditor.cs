using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TestStringParserEditor
{
    [Test]
    public void TestStringParseMultiple()
    {
        string[] str = new string[]
        {
            "hello world", // All separate
            "\"hello\" world", // Start with quote
            "hello \"world\"", // End with quote
            "\"hello\" \"world\"", // Start and end with quote
            "\"hello\"\"world\"", // Start and end with quote but has double quote in the middle
            "\"hello world\"", // All in quote
            "{\"name\":\"luvikung\",\"age\":20}", // JSON
            "{\"name\":\"thanut panichyotai\",\"age\":20}", // JSON with space in it
            "{\"name\":\"thanut panichyotai\",\"age\":20} {\"name\":\"luvikung\",\"age\":22}", // JSON with space in it and multiple JSON
            "/parse {\"name\":\"luvikung\",\"age\":20}", // command with JSON
            "/parse {\"name\":\"thanut panichyotai\",\"age\":20}", // command with JSON with space in it
        };
        string[][] expected = new string[][]
        {
            new string[] { "hello", "world" },
            new string[] { "hello", "world" },
            new string[] { "hello", "world" },
            new string[] { "hello", "world" },
            new string[] { "hello", "world" },
            new string[] { "hello world" },
            new string[] { "{\"name\":\"luvikung\",\"age\":20}" },
            new string[] { "{\"name\":\"thanut panichyotai\",\"age\":20}" },
            new string[] { "{\"name\":\"thanut panichyotai\",\"age\":20}", "{\"name\":\"luvikung\",\"age\":22}" },
            new string[] { "/parse", "{\"name\":\"luvikung\",\"age\":20}" },
            new string[] { "/parse", "{\"name\":\"thanut panichyotai\",\"age\":20}" },
        };
        for (int i = 0; i < str.Length; i++)
        {
            // get result string
            string[] result = new List<string>(SplitCommandArguments(str[i])).ToArray();
            // test equal with log.
            Assert.AreEqual(expected[i], result, "TestStringParse: " + str[i]);
        }
    }

    [Test]
    public void TestStringParseSingle()
    {
        string input = "{\"name\":\"thanut panichyotai\",\"age\":20} {\"name\":\"luvikung\",\"age\":22}";
        string[] output = new string[] { "{\"name\":\"thanut panichyotai\",\"age\":20}", "{\"name\":\"luvikung\",\"age\":22}" };
        string[] parsed = new List<string>(SplitCommandArguments(input)).ToArray();
        for (int i = 0; i < parsed.Length; i++)
            Debug.Log(parsed[i]);
        Assert.AreEqual(output, parsed, "Input: " + input);
    }

    // Split command arguments with space, JSON and quote support
    private IReadOnlyList<string> SplitCommandArguments(string command)
    {
        List<string> arguments = new List<string>();
        int start = 0;
        bool inQuote = false;
        bool inJSON = false;
        bool lastIsSpace = false;
        for (int i = 0; i < command.Length; i++)
        {
            if (command[i] == '{' && !inJSON && (i == 0 || lastIsSpace))
            {
                inJSON = true;
                start = i;
            }
            else if (command[i] == '}' && inJSON)
            {
                inJSON = false;
                if (start != i)
                {
                    arguments.Add(command.Substring(start, i + 1 - start));
                }
                start = i + 1;
            }
            else if (command[i] == '"' && !inJSON)
            {
                inQuote = !inQuote;
                if (inQuote)
                {
                    start = i + 1;
                }
                else
                {
                    if (start != i)
                    {
                        arguments.Add(command.Substring(start, i - start));
                    }
                    start = i + 1;
                }
            }
            else if (command[i] == ' ' && !inQuote && !inJSON)
            {
                if (start != i)
                {
                    arguments.Add(command.Substring(start, i - start));
                }
                start = i + 1;
                lastIsSpace = true;
            }
            else
            {
                lastIsSpace = false;
            }
        }
        var lastString = command.Substring(start);
        if (!string.IsNullOrEmpty(lastString))
            arguments.Add(lastString);
        return arguments;
    }
}
