using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace NR.Colors
{
    [InitializeOnLoad]
    static class ColorFolders
    {
        static Dictionary<string, Texture2D> iconCache = new();
        static string folderColor;

        static ColorFolders()
        {
            EditorApplication.projectWindowItemOnGUI -= OnGui;
            EditorApplication.projectWindowItemOnGUI += OnGui;
        }

        static void OnGui(string guid, Rect selectionRect)
        {
            Color backgroundColor;
            var folderRect = GetFolderRect(selectionRect, out backgroundColor);

            if (!iconCache.TryGetValue(guid, out Texture2D folderTexture))
            {
                var iconGUI = EditorPrefs.GetString(guid, "");
                if (string.IsNullOrEmpty(iconGUI))
                    return;

                var folderPath = AssetDatabase.GUIDToAssetPath(iconGUI);
                folderTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(folderPath);

                if (folderTexture != null)
                {
                    iconCache[guid] = folderTexture;
                }
            }

            if (Selection.assetGUIDs.Contains(guid))
            {
                EditorGUI.DrawRect(folderRect, Color.clear);
            }
            else
            {
                EditorGUI.DrawRect(folderRect, backgroundColor);
            }

            
            if (folderTexture != null)
            {
                GUI.DrawTexture(folderRect, folderTexture, ScaleMode.StretchToFill, true);
            }
        }

        static Rect GetFolderRect(Rect selectionRect, out Color backgroundColor)
        {
            Rect folderRect;
            var scaleFactor = 0.8f; // scale factor

            backgroundColor = new Color(0.2f, 0.2f, 0.2f);

            if (selectionRect.x < 15)
            {
                var scaledSize = selectionRect.height * scaleFactor;
                var offset = (selectionRect.height - scaledSize) / 2;
                folderRect = new Rect(selectionRect.x + 3 + offset, selectionRect.y + offset, scaledSize, scaledSize);
            }
            else if (selectionRect.x >= 15 && selectionRect.height < 30)
            {
                var scaledSize = selectionRect.height * scaleFactor;
                var offset = (selectionRect.height - scaledSize) / 2;
                folderRect = new Rect(selectionRect.x + offset, selectionRect.y + offset, scaledSize, scaledSize);
                backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            }
            else
            {
                var scaledWidth = selectionRect.width * scaleFactor;
                var offset = (selectionRect.width - scaledWidth) / 2;
                folderRect = new Rect(selectionRect.x + offset, selectionRect.y + offset, scaledWidth, scaledWidth);
            }

            return folderRect;
        }

        public static void SetIconName(string _folderColor)
        {
            var folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            var folderGUID = AssetDatabase.GUIDFromAssetPath(folderPath).ToString();

            var iconPath = $"Assets/Editor/Menu Items/Folder Colors/{_folderColor}.png";
            var iconGUID = AssetDatabase.GUIDFromAssetPath(iconPath).ToString();

            EditorPrefs.SetString(folderGUID, iconGUID);

            // clean cache
            iconCache.Remove(folderGUID);
        }

        public static void ResetColor()
        {
            var folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            var folderGUID = AssetDatabase.GUIDFromAssetPath(folderPath).ToString();
            EditorPrefs.DeleteKey(folderGUID);

            // clean cache
            iconCache.Remove(folderGUID);
        }
    }
}
