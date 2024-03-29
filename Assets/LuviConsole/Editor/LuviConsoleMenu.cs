﻿using UnityEditor;
using UnityEngine;

namespace LuviKunG.Console.Editor
{
    public class LuviConsoleMenu
    {
        [MenuItem("GameObject/LuviKunG/LuviConsole", false, 10)]
        public static LuviConsole CreateInstance()
        {
            GameObject obj = new GameObject("LuviConsole");
            return obj.AddComponent<LuviConsole>();
        }
    }
}