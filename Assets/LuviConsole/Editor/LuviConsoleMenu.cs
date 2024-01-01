using UnityEditor;
using UnityEngine;

namespace LuviKunG.Console.Editor
{
    public class LuviConsoleMenu
    {
        [MenuItem("GameObject/LuviKunG/Luvi Console", false, 10)]
        public static LuviConsole CreateLuviConsoleInstance()
        {
            GameObject obj = new GameObject("LuviConsole");
            return obj.AddComponent<LuviConsole>();
        }

        [MenuItem("GameObject/LuviKunG/Luvi Log Preview", false, 11)]
        public static LuviLogPreview CreateLuviLogPreviewInstance()
        {
            GameObject obj = new GameObject("LuviLogPreview");
            return obj.AddComponent<LuviLogPreview>();
        }
    }
}