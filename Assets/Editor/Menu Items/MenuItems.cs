using UnityEngine;
using UnityEditor;

namespace NR.Colors
{
    public static class MenuItems
    {
        const int priority = 100000;
        
        [MenuItem("Assets/Colors/Sky Blue", false, priority)]
        static void ColorBlue() => SetColor("Sky Blue");

        [MenuItem("Assets/Colors/Blue", false, priority)]
        static void ColorRed() => SetColor("Blue");

        [MenuItem("Assets/Colors/Green", false, priority)]
        static void ColorGreen() => SetColor("Green");

        [MenuItem("Assets/Colors/Yellow", false, priority)]
        static void ColorYellow() => SetColor("Yellow");

        [MenuItem("Assets/Colors/Purple", false, priority)]
        static void ColorPurple() => SetColor("Purple");

        [MenuItem("Assets/Colors/Red", false, priority)]
        static void ColorOrange() => SetColor("Red");

        [MenuItem("Assets/Colors/Pink", false, priority)]
        static void ColorPink() => SetColor("Pink");
        
        [MenuItem("Assets/Colors/Grey", false, priority)]
        static void ColorGrey() => SetColor("Grey");

        [MenuItem("Assets/Colors/Black", false, priority)]
        static void ColorBlack() => SetColor("Black");

        [MenuItem("Assets/Colors/White", false, priority + 11)]
        static void ColorWhite() => SetColor("White");

        [MenuItem("Assets/Colors/Reset", false, priority + 22)]
        static void ResetIcon() => ColorFolders.ResetColor();

        static void SetColor(string colorName)
        {
            ColorFolders.SetIconName(colorName);
            Debug.Log($"Color: {colorName}");
        }
        
        [MenuItem("Assets/Colors/Sky Blue", true)]
        [MenuItem("Assets/Colors/Blue", true)]
        [MenuItem("Assets/Colors/Green", true)]
        [MenuItem("Assets/Colors/Yellow", true)]
        [MenuItem("Assets/Colors/Purple", true)]
        [MenuItem("Assets/Colors/Red", true)]
        [MenuItem("Assets/Colors/Pink", true)]
        [MenuItem("Assets/Colors/Grey", true)]
        [MenuItem("Assets/Colors/Black", true)]
        [MenuItem("Assets/Colors/White", true)]
        [MenuItem("Assets/Colors/Reset", true)]
        static bool ColorFolder()
        {
            var selectedObject = Selection.activeObject;
            return selectedObject != null && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(selectedObject));
        }
    }
}
