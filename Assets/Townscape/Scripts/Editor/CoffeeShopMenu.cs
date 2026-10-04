using System.IO;
using Townscape.Runtime.CoffeeShop;
using UnityEditor;
using UnityEngine;

namespace Townscape.Editor
{
    /// <summary>Finding and clearing the Fellside Coffee save while working on the game.</summary>
    internal static class CoffeeShopMenu
    {
        [MenuItem("Townscape/Fellside Coffee/Show Save File", priority = 200)]
        private static void ShowSaveFile()
        {
            var path = CoffeeShopSaveFile.FilePath;
            if (File.Exists(path))
            {
                EditorUtility.RevealInFinder(path);
            }
            else
            {
                EditorUtility.RevealInFinder(Application.persistentDataPath);
                Debug.Log($"[Fellside Coffee] No save yet. It will be written to {path}.");
            }
        }

        [MenuItem("Townscape/Fellside Coffee/Delete Save File", priority = 201)]
        private static void DeleteSaveFile()
        {
            if (!EditorUtility.DisplayDialog("Delete the Fellside Coffee save?", $"{CoffeeShopSaveFile.FilePath}\n\nThe next game starts from day 1.", "Delete", "Cancel"))
            {
                return;
            }

            CoffeeShopSaveFile.Delete();
            Debug.Log("[Fellside Coffee] Deleted the save.");
        }

        // While playing, the running game would only save itself again.
        [MenuItem("Townscape/Fellside Coffee/Delete Save File", true)]
        private static bool CanDeleteSaveFile() => !EditorApplication.isPlaying && CoffeeShopSaveFile.Exists;
    }
}
