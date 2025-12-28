using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class UniversalSpriteSlicer : EditorWindow
{
    // --- SETTINGS ---
    public enum InputMethod { EnterTotalCount, EnterPixelWidth } 
    public InputMethod currentMethod = InputMethod.EnterTotalCount;

    public int framesPerStrip = 4;   // Used if EnterTotalCount
    public int cellWidthPixels = 32; // Used if EnterPixelWidth

    public enum PivotMethod { BottomCenter, Center, BottomLeft }
    public PivotMethod pivotLocation = PivotMethod.BottomCenter;


    [MenuItem("Tools/Universal Sprite Slicer")]
    public static void ShowWindow()
    {
        GetWindow<UniversalSpriteSlicer>("Sprite Slicer");
    }

    void OnGUI()
    {
        GUILayout.Label("Slice Settings", EditorStyles.boldLabel);

        // 1. Toggle with Clear Names
        currentMethod = (InputMethod)EditorGUILayout.EnumPopup("Input Method", currentMethod);

        // 2. Fields
        if (currentMethod == InputMethod.EnterTotalCount)
        {
            framesPerStrip = EditorGUILayout.IntField("Total Items in Row", framesPerStrip);
            if (framesPerStrip < 1) framesPerStrip = 1;
        }
        else
        {
            cellWidthPixels = EditorGUILayout.IntField("Width of One Item (px)", cellWidthPixels);
            if (cellWidthPixels < 1) cellWidthPixels = 1;
        }

        // 3. Pivot
        GUILayout.Space(10);
        pivotLocation = (PivotMethod)EditorGUILayout.EnumPopup("Pivot", pivotLocation);

        // 4. Button
        GUILayout.Space(20);
        if (GUILayout.Button("Slice Selected Sprites", GUILayout.Height(40)))
        {
            Slice();
        }
    }

    void Slice()
    {
        Object[] selectedObjects = Selection.objects;
        int processedCount = 0;

        // Determine pivot
        Vector2 pivot = new Vector2(0.5f, 0.0f);
        if (pivotLocation == PivotMethod.Center) pivot = new Vector2(0.5f, 0.5f);
        if (pivotLocation == PivotMethod.BottomLeft) pivot = new Vector2(0.0f, 0.0f);

        foreach (Object obj in selectedObjects)
        {
            if (obj is Texture2D)
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                if (importer != null)
                {
                    // --- STEP 1: READ RAW FILE DIMENSIONS ---
                    int realWidth = 0;
                    int realHeight = 0;

                    try 
                    {
                        string fullSystemPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
                        byte[] fileData = File.ReadAllBytes(fullSystemPath);
                        
                        Texture2D rawTex = new Texture2D(2, 2);
                        rawTex.LoadImage(fileData); 
                        
                        realWidth = rawTex.width;
                        realHeight = rawTex.height;
                        
                        DestroyImmediate(rawTex);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Could not read file dimensions for {obj.name}: {e.Message}");
                        continue;
                    }

                    // --- STEP 2: SETUP IMPORTER ---
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Multiple;
                    importer.filterMode = FilterMode.Point;
                    importer.npotScale = TextureImporterNPOTScale.None; 
                    importer.isReadable = true;
                    importer.maxTextureSize = 8192; // Ensure high-res trees fit

                    // --- STEP 3: CALCULATE SLICES ---
                    int finalCellWidth = 0;
                    int finalFrameCount = 0;

                    if (currentMethod == InputMethod.EnterTotalCount)
                    {
                        // Logic: Total Width / Total Count = Width per Item
                        finalFrameCount = framesPerStrip;
                        finalCellWidth = realWidth / finalFrameCount;
                    }
                    else // EnterPixelWidth
                    {
                        // Logic: Total Width / Item Width = Total Count
                        finalCellWidth = cellWidthPixels;
                        finalFrameCount = realWidth / finalCellWidth;
                    }

                    // Safety Check
                    if (finalCellWidth <= 0 || finalFrameCount <= 0)
                    {
                        Debug.LogError($"Error on '{obj.name}': Dimensions mismatch. Image Width: {realWidth}px. Check your settings.");
                        continue;
                    }

                    List<SpriteMetaData> spriteSheet = new List<SpriteMetaData>();

                    for (int x = 0; x < finalFrameCount; x++)
                    {
                        SpriteMetaData smd = new SpriteMetaData();
                        
                        // Always use FULL height of the raw image
                        smd.rect = new Rect(x * finalCellWidth, 0, finalCellWidth, realHeight);
                        
                        smd.pivot = pivot;
                        if (pivotLocation == PivotMethod.BottomCenter) smd.alignment = (int)SpriteAlignment.BottomCenter;
                        else if (pivotLocation == PivotMethod.Center) smd.alignment = (int)SpriteAlignment.Center;
                        else smd.alignment = (int)SpriteAlignment.Custom;

                        smd.name = $"{obj.name}_{x}";
                        spriteSheet.Add(smd);
                    }

                    // --- STEP 4: APPLY ---
                    importer.spritesheet = spriteSheet.ToArray();
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                    
                    processedCount++;
                }
            }
        }
        Debug.Log($"Sliced {processedCount} files successfully.");
    }
}