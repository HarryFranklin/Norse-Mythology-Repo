using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

// The 'true' argument means this editor works for Ability AND all its children (FreezeTimeAbility, etc.)
[CustomEditor(typeof(Ability), true)] 
public class AbilityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. Draw the default Inspector (fields, arrays, etc.)
        DrawDefaultInspector();

        // 2. Add some spacing
        GUILayout.Space(15);

        // 3. Get the target object
        Ability ability = (Ability)target;

        // 4. Draw the Update Button
        GUI.enabled = ability.useCodeDefinedMatrix; 
        
        if (GUILayout.Button("Update Stats from Code", GUILayout.Height(30)))
        {
            ability.InitialiseFromCodeMatrix();
            EditorUtility.SetDirty(ability);
            Debug.Log($"Updated {ability.name} stats from Code Matrix!");
        }
        
        GUI.enabled = true;

        // 5. Draw Audio Previews
        DrawAudioPreview(ability);
    }

    private void DrawAudioPreview(UnityEngine.Object target)
    {
        bool hasAudio = false;
        
        // Use reflection to find all AudioClip fields in the ability
        var fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(AudioClip))
            {
                AudioClip clip = (AudioClip)field.GetValue(target);
                if (clip != null)
                {
                    if (!hasAudio)
                    {
                        GUILayout.Space(10);
                        GUILayout.Label("Audio Previews", EditorStyles.boldLabel);
                        hasAudio = true;
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(field.Name), GUILayout.Width(150));
                    EditorGUILayout.LabelField(clip.name, EditorStyles.miniLabel);
                    
                    if (GUILayout.Button("►", GUILayout.Width(30)))
                    {
                        PlayClip(clip);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }

    private void PlayClip(AudioClip clip)
    {
        // 1. Get the UnityEditor Assembly and the AudioUtil Class
        Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
        Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");

        if (audioUtilClass == null) return;

        // 2. Stop any current previews first (Try both 'StopAllPreviewClips' and 'StopAllClips')
        MethodInfo stopMethod = audioUtilClass.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public);
        if (stopMethod == null) stopMethod = audioUtilClass.GetMethod("StopAllClips", BindingFlags.Static | BindingFlags.Public);
        
        if (stopMethod != null) stopMethod.Invoke(null, null);

        // 3. Find the Play method (Try 'PlayPreviewClip' first, then 'PlayClip')
        // Newer Unity versions (2020.2+) use "PlayPreviewClip"
        MethodInfo playMethod = audioUtilClass.GetMethod(
            "PlayPreviewClip",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
            null
        );

        // Older Unity versions use "PlayClip"
        if (playMethod == null)
        {
            playMethod = audioUtilClass.GetMethod(
                "PlayClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null
            );
        }

        // Ancient Unity versions might use "PlayClip" with 1 argument
        if (playMethod == null)
        {
             playMethod = audioUtilClass.GetMethod(
                "PlayClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(AudioClip) },
                null
            );
        }

        // 4. Invoke the method
        if (playMethod != null)
        {
            int paramCount = playMethod.GetParameters().Length;
            if (paramCount == 3)
            {
                // AudioClip, StartSample (0), Loop (false)
                playMethod.Invoke(null, new object[] { clip, 0, false });
            }
            else if (paramCount == 1)
            {
                playMethod.Invoke(null, new object[] { clip });
            }
        }
        else
        {
            Debug.LogError("Could not find AudioUtil.PlayClip or PlayPreviewClip via reflection.");
        }
    }
}