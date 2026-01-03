using UnityEngine;
using UnityEditor;

public class ParentPrefixer : EditorWindow
{
    [MenuItem("Tools/Rename/Prefix with Parent Name")]
    private static void PrefixSelectedObjects()
    {
        // Get all selected objects in the scene
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected to rename.");
            return;
        }

        // This allows you to press Ctrl+Z to undo the rename
        Undo.RecordObjects(selectedObjects, "Prefix Parent Name");

        int count = 0;
        foreach (GameObject obj in selectedObjects)
        {
            // Skip if no parent exists
            if (obj.transform.parent == null) continue;

            string parentName = obj.transform.parent.name;
            string currentName = obj.name;

            // Optional: Check if it already has the prefix to avoid "LeftPanel-LeftPanel-Title"
            if (currentName.StartsWith(parentName + "-") || currentName.StartsWith(parentName + "_"))
            {
                continue;
            }

            // Apply the new name
            obj.name = $"{parentName}-{currentName}";
            count++;
        }

        Debug.Log($"Renamed {count} objects.");
    }
    
    // Validation: Greys out the menu item if nothing is selected
    [MenuItem("Tools/Rename/Prefix with Parent Name", true)]
    private static bool ValidatePrefixSelectedObjects()
    {
        return Selection.gameObjects.Length > 0;
    }
}