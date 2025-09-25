using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(PropRandomiser))]
public class PropRandomiserEditor : Editor
{
    private SerializedProperty propSpawnPoints;
    private SerializedProperty autoNormaliseProportions;
    private SerializedProperty propCategories;
    
    // Moved oldProportions to be a class-level variable to fix the scope issue.
    private float[] oldProportions;

    private void OnEnable()
    {
        propSpawnPoints = serializedObject.FindProperty("propSpawnPoints");
        autoNormaliseProportions = serializedObject.FindProperty("autoNormaliseProportions");
        propCategories = serializedObject.FindProperty("propCategories");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(propSpawnPoints, true);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Prop Categories", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(autoNormaliseProportions);

        HandleFolderDragAndDrop();

        // Ensure the oldProportions array is the correct size
        if (oldProportions == null || oldProportions.Length != propCategories.arraySize)
        {
            oldProportions = new float[propCategories.arraySize];
        }
        
        // Store old proportion values to detect changes
        for (int i = 0; i < propCategories.arraySize; i++)
        {
            oldProportions[i] = propCategories.GetArrayElementAtIndex(i).FindPropertyRelative("spawnProportion").floatValue;
        }

        // Loop through each category to draw the UI
        for (int i = 0; i < propCategories.arraySize; i++)
        {
            DrawCategoryUI(i);
        }

        // Normalise Proportions if a slider value has changed
        if (autoNormaliseProportions.boolValue)
        {
            for (int i = 0; i < propCategories.arraySize; i++)
            {
                float newProportion = propCategories.GetArrayElementAtIndex(i).FindPropertyRelative("spawnProportion").floatValue;
                if (Mathf.Abs(newProportion - oldProportions[i]) > 0.001f) // Check if a slider value has changed
                {
                    NormaliseProportions(i, newProportion);
                    break; // Normalise and then exit to avoid multiple normalisations in one frame
                }
            }
        }

        if (GUILayout.Button("Add New Category"))
        {
            propCategories.arraySize++;
            SerializedProperty newCategory = propCategories.GetArrayElementAtIndex(propCategories.arraySize - 1);
            newCategory.FindPropertyRelative("name").stringValue = "New Category";
            newCategory.FindPropertyRelative("enabled").boolValue = true;
            newCategory.FindPropertyRelative("spawnProportion").floatValue = 1f;
            newCategory.FindPropertyRelative("allPrefabsEnabled").boolValue = true;
            newCategory.FindPropertyRelative("prefabs").ClearArray();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCategoryUI(int index)
    {
        SerializedProperty category = propCategories.GetArrayElementAtIndex(index);
        SerializedProperty categoryEnabled = category.FindPropertyRelative("enabled");
        SerializedProperty categoryName = category.FindPropertyRelative("name");
        SerializedProperty spawnProportion = category.FindPropertyRelative("spawnProportion");
        SerializedProperty allPrefabsEnabled = category.FindPropertyRelative("allPrefabsEnabled");
        SerializedProperty prefabsList = category.FindPropertyRelative("prefabs");

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        categoryEnabled.boolValue = EditorGUILayout.Toggle(categoryEnabled.boolValue, GUILayout.Width(20));
        categoryName.stringValue = EditorGUILayout.TextField(categoryName.stringValue);
        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            propCategories.DeleteArrayElementAtIndex(index);
            return;
        }
        EditorGUILayout.EndHorizontal();

        if (categoryEnabled.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Slider(spawnProportion, 0f, 1f, new GUIContent("Spawn Proportion"));
            EditorGUILayout.PropertyField(prefabsList, new GUIContent("Prefabs"), false);

            if (prefabsList.isExpanded)
            {
                EditorGUI.indentLevel++;
                bool newAllEnabledState = EditorGUILayout.Toggle("Enable All Prefabs", allPrefabsEnabled.boolValue);
                if (newAllEnabledState != allPrefabsEnabled.boolValue)
                {
                    allPrefabsEnabled.boolValue = newAllEnabledState;
                    for (int j = 0; j < prefabsList.arraySize; j++)
                    {
                        prefabsList.GetArrayElementAtIndex(j).FindPropertyRelative("enabled").boolValue = newAllEnabledState;
                    }
                }

                prefabsList.arraySize = EditorGUILayout.IntField("Size", prefabsList.arraySize);

                for (int j = 0; j < prefabsList.arraySize; j++)
                {
                    SerializedProperty selectablePrefab = prefabsList.GetArrayElementAtIndex(j);
                    SerializedProperty prefabEnabled = selectablePrefab.FindPropertyRelative("enabled");
                    SerializedProperty prefabObject = selectablePrefab.FindPropertyRelative("prefab");

                    EditorGUILayout.BeginHorizontal();
                    prefabEnabled.boolValue = EditorGUILayout.Toggle(prefabEnabled.boolValue, GUILayout.Width(20));
                    EditorGUILayout.PropertyField(prefabObject, GUIContent.none);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void HandleFolderDragAndDrop()
    {
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag a Folder Here to Create a New Prop Category");

        Event currentEvent = Event.current;
        if (!dropArea.Contains(currentEvent.mousePosition))
        {
            return;
        }

        if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (Object draggedObject in DragAndDrop.objectReferences)
                {
                    if (draggedObject is DefaultAsset)
                    {
                        string path = AssetDatabase.GetAssetPath(draggedObject);
                        if (System.IO.Directory.Exists(path))
                        {
                            CreateCategoryFromFolder(path);
                        }
                    }
                }
            }
            currentEvent.Use();
        }
    }

    private void CreateCategoryFromFolder(string path)
    {
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { path });
        List<GameObject> prefabs = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(obj => obj != null).ToList();

        if (prefabs.Count == 0)
        {
            Debug.LogWarning($"No GameObject prefabs found in folder: {path}");
            return;
        }

        propCategories.arraySize++;
        SerializedProperty newCategory = propCategories.GetArrayElementAtIndex(propCategories.arraySize - 1);
        newCategory.FindPropertyRelative("name").stringValue = System.IO.Path.GetFileName(path);
        newCategory.FindPropertyRelative("enabled").boolValue = true;
        newCategory.FindPropertyRelative("spawnProportion").floatValue = 1f;
        newCategory.FindPropertyRelative("allPrefabsEnabled").boolValue = true;

        SerializedProperty prefabsList = newCategory.FindPropertyRelative("prefabs");
        prefabsList.ClearArray();
        prefabsList.arraySize = prefabs.Count;
        for (int i = 0; i < prefabs.Count; i++)
        {
            SerializedProperty item = prefabsList.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("prefab").objectReferenceValue = prefabs[i];
            item.FindPropertyRelative("enabled").boolValue = true;
        }
    }

    private void NormaliseProportions(int changedIndex, float newValue)
    {
        // Calculate the sum of proportions of OTHER sliders
        float sumOfOthers = 0f;
        for(int i = 0; i < propCategories.arraySize; i++)
        {
            if (i == changedIndex) continue;
            sumOfOthers += oldProportions[i];
        }

        float remainderToDistribute = 1.0f - newValue;

        // Distribute the remainder among the other sliders
        for (int i = 0; i < propCategories.arraySize; i++)
        {
            if (i == changedIndex) continue;

            SerializedProperty proportionProp = propCategories.GetArrayElementAtIndex(i).FindPropertyRelative("spawnProportion");
            if (sumOfOthers > 0)
            {
                // Scale the other sliders proportionally
                proportionProp.floatValue = (oldProportions[i] / sumOfOthers) * remainderToDistribute;
            }
            else if (propCategories.arraySize > 1)
            {
                // If other sliders are all zero, distribute the remainder equally
                proportionProp.floatValue = remainderToDistribute / (propCategories.arraySize - 1);
            }
        }
    }
}