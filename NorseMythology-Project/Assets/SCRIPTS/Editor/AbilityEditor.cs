using UnityEngine;
using UnityEditor;

// The 'true' argument means this editor works for Ability AND all its children (FreezeTimeAbility, etc.)
[CustomEditor(typeof(Ability), true)] 
public class AbilityEditor : Editor
{
    // This basically lets me click a button and update the values in the editor based on what is in the code, so at least one of them is true.
    // This'll help update ANCIENT in-editor values from what I worked on more recently, i.e. the in-code values.

    public override void OnInspectorGUI()
    {
        // 1. Draw the default Inspector (fields, arrays, etc.)
        DrawDefaultInspector();

        // 2. Add some spacing
        GUILayout.Space(15);

        // 3. Get the target object
        Ability ability = (Ability)target;

        // 4. Draw the Button
        // We only enable the button if the "Use Code Defined Matrix" boolean is checked (optional safety)
        GUI.enabled = ability.useCodeDefinedMatrix; 
        
        if (GUILayout.Button("Update Stats from Code", GUILayout.Height(30)))
        {
            // Call the method
            ability.InitialiseFromCodeMatrix();
            
            // Mark the object as "Dirty" so Unity knows to save the changes to disk
            EditorUtility.SetDirty(ability);
            
            Debug.Log($"Updated {ability.name} stats from Code Matrix!");
        }
        
        GUI.enabled = true; // Re-enable GUI for anything else
    }
}