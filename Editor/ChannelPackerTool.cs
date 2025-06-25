// ChannelPackerTool.cs
// Version 1.0 - Initial release, June 2025
// A Unity Editor tool for packing/unpacking texture channels.
// Developed by Richard/NotMyFirstNull with significant contributions from Grok (xAI) and ChatGPT.
// Tested in Unity 6000.1.5f1 (may work in earlier versions).

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class ChannelPackerTool : EditorWindow
{
    // Pack variables
    private Texture2D redSource, greenSource, blueSource, alphaSource;
    private bool invertR, invertG, invertB, invertA;
    private enum FallbackColour { Black, Gray, White }
    private FallbackColour fallbackR = FallbackColour.Black;
    private FallbackColour fallbackG = FallbackColour.Black;
    private FallbackColour fallbackB = FallbackColour.Black;
    private FallbackColour fallbackA = FallbackColour.Black;

    // Unpack variables
    private Texture2D unpackSource;
    private bool unpackR = true, unpackG = true, unpackB = true, unpackA = true;

    private bool isProcessing;
    private int tab; // 0 = Pack, 1 = Unpack
    private Vector2 scrollPos;
    private Texture2D previewTexture;
    private bool showPreview;
    private string lastSavePath = "Assets";

    private readonly string[] fallbackOptions = { "Black", "Gray", "White" };
    private readonly string[] tabs = { "Pack", "Unpack" };

    [MenuItem("Tools/Channel Packer Tool")]
    private static void OpenWindow()
    {
        GetWindow<ChannelPackerTool>("Channel Packer");
    }

    private void OnEnable()
    {
        lastSavePath = EditorPrefs.GetString("ChannelPacker_LastSavePath", "Assets");
    }

    private void OnDisable()
    {
        if (previewTexture != null) DestroyImmediate(previewTexture);
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        tab = GUILayout.Toolbar(tab, tabs, EditorStyles.toolbarButton);

        EditorGUILayout.Space(5);

        switch (tab)
        {
            case 0: DrawPackTab(); break;
            case 1: DrawUnpackingTab(); break;
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawPackTab()
    {
        EditorGUILayout.LabelField("Pack Channels Into One Texture", EditorStyles.boldLabel);
        GUIStyle style = new(EditorStyles.label) { wordWrap = true };
        EditorGUILayout.LabelField(
            "Assign grayscale textures to the R, G, B, or A slots. Fallback colours are used for empty channels. Assign at least one texture.",
            style
        );

        DrawDivider();

        EditorGUI.BeginDisabledGroup(isProcessing);

        EditorGUILayout.BeginVertical("box");
        DrawChannelField("Red", ref redSource, ref invertR, ref fallbackR);
        DrawChannelField("Green", ref greenSource, ref invertG, ref fallbackG);
        DrawChannelField("Blue", ref blueSource, ref invertB, ref fallbackB);
        DrawChannelField("Alpha", ref alphaSource, ref invertA, ref fallbackA);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        if (previewTexture != null)
        {
            showPreview = EditorGUILayout.Foldout(showPreview, "Preview Result", true);
            if (showPreview)
            {
                float maxPreviewSize = 256f;
                float aspectRatio = (float)previewTexture.width / previewTexture.height;
                float previewWidth = maxPreviewSize;
                float previewHeight = maxPreviewSize / aspectRatio;
                if (previewHeight > maxPreviewSize)
                {
                    previewHeight = maxPreviewSize;
                    previewWidth = maxPreviewSize * aspectRatio;
                }
                float availableWidth = EditorGUIUtility.currentViewWidth - 40;
                float xOffset = (availableWidth - previewWidth) / 2;
                Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(previewWidth), GUILayout.Height(previewHeight));
                rect.x += xOffset;
                EditorGUI.DrawPreviewTexture(rect, previewTexture);
                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear Preview", GUILayout.Width(100)))
                {
                    if (previewTexture != null) DestroyImmediate(previewTexture);
                    previewTexture = null;
                    showPreview = false;
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUI.EndDisabledGroup();

        if (isProcessing)
            EditorGUILayout.HelpBox("Processing... Please wait.", MessageType.Info);
        EditorGUILayout.Space(4);
        if (GUILayout.Button(new GUIContent("Pack", "Combine textures into a single RGBA image"), GUILayout.Height(30)) && !isProcessing)
        {
            ProcessPacking();
        }
    }

    private void DrawChannelField(string label, ref Texture2D tex, ref bool invert, ref FallbackColour fallback)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label + ":", GUILayout.Width(50));
        tex = (Texture2D)EditorGUILayout.ObjectField(tex, typeof(Texture2D), false, GUILayout.Width(150));
        if (tex == null)
        {
            GUILayout.Label(new GUIContent("Fallback", "Fill channel with this colour"), GUILayout.Width(60));
            fallback = (FallbackColour)EditorGUILayout.Popup((int)fallback, fallbackOptions, GUILayout.Width(70));
        }
        else
        {
            invert = EditorGUILayout.ToggleLeft(new GUIContent("Invert", "Invert this channel"), invert, GUILayout.Width(70));
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2);
        DrawDivider();
    }

    private void DrawDivider(float height = 1f, float padding = 4f)
    {
        Rect rect = EditorGUILayout.GetControlRect(false, height + padding);
        rect.height = height;
        rect.y += padding * 0.5f;
        EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f)); // dark grey line
    }

    private void ProcessPacking()
    {
        bool tryAgain;
        do
        {
            tryAgain = false;
            isProcessing = true;
            try
            {
                if (!ValidatePackingTextures(out string errorMsg, out tryAgain))
                {
                    if (errorMsg != "")
                        EditorUtility.DisplayDialog("Error", errorMsg, "OK");
                    continue;
                }
                PackTexture();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Packing failed: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Packing failed:\n{e.Message}", "OK");
            }
            finally
            {
                isProcessing = false;
                EditorUtility.ClearProgressBar();
            }
        } while (tryAgain);
    }

    private bool ValidatePackingTextures(out string errorMsg, out bool tryAgain)
    {
        errorMsg = "";
        tryAgain = false;
        List<Texture2D> textures = new List<Texture2D> { redSource, greenSource, blueSource, alphaSource };
        List<string> textureNames = new List<string> { "Red", "Green", "Blue", "Alpha" };
        int refWidth = -1, refHeight = -1;

        // Set reference dimensions from the first assigned texture
        for (int i = 0; i < textures.Count; i++)
        {
            if (textures[i] != null)
            {
                refWidth = textures[i].width;
                refHeight = textures[i].height;
                break;
            }
        }

        if (refWidth == -1)
        {
            errorMsg = "Please assign at least one texture.";
            return false;
        }

        // Check readability
        List<(Texture2D tex, string name, string path)> nonReadableTextures = new List<(Texture2D, string, string)>();
        for (int i = 0; i < textures.Count; i++)
        {
            if (textures[i] == null) continue;
            if (!CheckTextureReadable(textures[i], textureNames[i], out bool textureTryAgain))
            {
                nonReadableTextures.Add((textures[i], textureNames[i], AssetDatabase.GetAssetPath(textures[i])));
                tryAgain |= textureTryAgain;
            }
        }

        if (nonReadableTextures.Count > 0)
        {
            string prompt = "The following textures are not readable:\n" +
                            string.Join("\n", nonReadableTextures.ConvertAll(t => $"- {t.name}")) +
                            "\nEnable Read/Write in import settings for all?";
            if (EditorUtility.DisplayDialog("Textures Not Readable", prompt, "Fix All", "Cancel"))
            {
                foreach (var (tex, name, path) in nonReadableTextures)
                {
                    TextureImporter ti = TextureImporter.GetAtPath(path) as TextureImporter;
                    ti.isReadable = true;
                    ti.SaveAndReimport();
                }
                AssetDatabase.Refresh();
                tryAgain = true;
                return false;
            }
            else
            {
                errorMsg = "Operation aborted because some textures are not readable.";
                tryAgain = false;
                return false;
            }
        }

        // Check dimensions
        for (int i = 0; i < textures.Count; i++)
        {
            if (textures[i] == null) continue;
            if (textures[i].width != refWidth || textures[i].height != refHeight)
            {
                errorMsg = $"All textures must have the same dimensions. {textureNames[i]} texture is {textures[i].width}x{textures[i].height}, expected {refWidth}x{refHeight}.";
                return false;
            }
        }

        return true;
    }

    private void PackTexture()
    {
        Texture2D refTex = redSource ?? greenSource ?? blueSource ?? alphaSource;
        if (refTex == null)
        {
            Debug.LogError("No source texture assigned to any channel.");
            EditorUtility.DisplayDialog("Error", "No source texture assigned. Please assign at least one texture.", "OK");
            return;
        }

        int width = refTex.width;
        int height = refTex.height;

        Texture2D result = new(width, height, TextureFormat.RGBA32, false, false);
        Color[] redPixels = redSource != null ? redSource.GetPixels() : null;
        Color[] greenPixels = greenSource != null ? greenSource.GetPixels() : null;
        Color[] bluePixels = blueSource != null ? blueSource.GetPixels() : null;
        Color[] alphaPixels = alphaSource != null ? alphaSource.GetPixels() : null;

        Color[] pixels = new Color[width * height];
        int updateInterval = Mathf.Max(1, height / 10);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                float r = redPixels != null ? SampleChannel(redPixels, idx, invertR, fallbackR) : GetFallbackValue(fallbackR);
                float g = greenPixels != null ? SampleChannel(greenPixels, idx, invertG, fallbackG) : GetFallbackValue(fallbackG);
                float b = bluePixels != null ? SampleChannel(bluePixels, idx, invertB, fallbackB) : GetFallbackValue(fallbackB);
                float a = alphaPixels != null ? SampleChannel(alphaPixels, idx, invertA, fallbackA) : GetFallbackValue(fallbackA);
                pixels[idx] = new Color(r, g, b, a);
            }
            if (y % updateInterval == 0)
            {
                EditorUtility.DisplayProgressBar("Channel Packer", $"Packing row {y}/{height}", (float)y / height);
            }
        }

        result.SetPixels(pixels);
        result.Apply();

        if (previewTexture != null) DestroyImmediate(previewTexture);
        previewTexture = result;
        showPreview = true;

        string path = EditorUtility.SaveFilePanel("Save Packed Texture", lastSavePath, "PackedTexture.png", "png");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllBytes(path, result.EncodeToPNG());
            Debug.Log($"Saved packed texture to {path}");
            lastSavePath = Path.GetDirectoryName(path);
            EditorPrefs.SetString("ChannelPacker_LastSavePath", lastSavePath);
            AssetDatabase.Refresh();
        }
        else
        {
            DestroyImmediate(result);
        }
    }

    private float GetFallbackValue(FallbackColour fallback) => fallback switch
    {
        FallbackColour.Black => 0f,
        FallbackColour.Gray => 0.5f,
        FallbackColour.White => 1f,
        _ => 0f
    };

    private float SampleChannel(Color[] pixels, int idx, bool invert, FallbackColour fallback)
    {
        if (pixels == null)
        {
            return GetFallbackValue(fallback);
        }
        float val = pixels[idx].r;
        return invert ? 1f - val : val;
    }

    private void DrawUnpackingTab()
    {
        EditorGUILayout.LabelField("Unpack Channels From Texture", EditorStyles.boldLabel);
        GUIStyle style = new(EditorStyles.label) { wordWrap = true };
        EditorGUILayout.LabelField(
            "Select channels to extract from the source texture into separate grayscale images.",
            style
        );
        DrawDivider();
        EditorGUI.BeginDisabledGroup(isProcessing);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Source Texture", EditorStyles.boldLabel);
        unpackSource = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("", "Source texture to unpack"),
            unpackSource, typeof(Texture2D), false);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Channels to Unpack", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        unpackR = EditorGUILayout.ToggleLeft(new GUIContent("R", "Unpack Red channel"), unpackR, GUILayout.Width(40));
        unpackG = EditorGUILayout.ToggleLeft(new GUIContent("G", "Unpack Green channel"), unpackG, GUILayout.Width(40));
        unpackB = EditorGUILayout.ToggleLeft(new GUIContent("B", "Unpack Blue channel"), unpackB, GUILayout.Width(40));
        unpackA = EditorGUILayout.ToggleLeft(new GUIContent("A", "Unpack Alpha channel"), unpackA, GUILayout.Width(40));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUI.EndDisabledGroup();

        if (isProcessing)
            EditorGUILayout.HelpBox("Processing... Please wait.", MessageType.Info);

        if (GUILayout.Button(new GUIContent("Unpack", "Extract selected channels into grayscale images"), GUILayout.Height(30)) && !isProcessing)
        {
            ProcessUnpack();
        }
    }

    private void ProcessUnpack()
    {
        if (unpackSource == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a source texture.", "OK");
            return;
        }

        if (!unpackR && !unpackG && !unpackB && !unpackA)
        {
            EditorUtility.DisplayDialog("Error", "Please select at least one channel to unpack.", "OK");
            return;
        }

        bool tryAgain;
        do
        {
            tryAgain = false;
            isProcessing = true;
            try
            {
                if (!CheckTextureReadable(unpackSource, "Source", out tryAgain))
                {
                    if (tryAgain)
                    {
                        string path = AssetDatabase.GetAssetPath(unpackSource);
                        TextureImporter ti = TextureImporter.GetAtPath(path) as TextureImporter;
                        if (EditorUtility.DisplayDialog("Texture Not Readable",
                            $"Texture 'Source' is not readable. Enable Read/Write in import settings?", "Fix", "Cancel"))
                        {
                            ti.isReadable = true;
                            ti.SaveAndReimport();
                            AssetDatabase.Refresh();
                            tryAgain = true;
                            continue;
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error", "Operation aborted because source texture is not readable.", "OK");
                            tryAgain = false;
                            return;
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Operation aborted because source texture is not readable.", "OK");
                        return;
                    }
                }

                UnpackTexture();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Unpack failed: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Unpack failed:\n{e.Message}", "OK");
            }
            finally
            {
                isProcessing = false;
                EditorUtility.ClearProgressBar();
            }
        } while (tryAgain);
    }

    private void UnpackTexture()
    {
        int width = unpackSource.width;
        int height = unpackSource.height;
        Color[] pixels = unpackSource.GetPixels();

        string folder = EditorUtility.OpenFolderPanel("Select Output Folder", lastSavePath, "");
        if (string.IsNullOrEmpty(folder))
            return;

        lastSavePath = folder;
        EditorPrefs.SetString("ChannelPacker_LastSavePath", lastSavePath);

        int updateInterval = Mathf.Max(1, height / 10);
        if (unpackR) SaveChannelTexture(pixels, width, height, Channel.R, folder, updateInterval);
        if (unpackG) SaveChannelTexture(pixels, width, height, Channel.G, folder, updateInterval);
        if (unpackB) SaveChannelTexture(pixels, width, height, Channel.B, folder, updateInterval);
        if (unpackA) SaveChannelTexture(pixels, width, height, Channel.A, folder, updateInterval);
    }

    private enum Channel { R, G, B, A }

    private void SaveChannelTexture(Color[] pixels, int width, int height, Channel channel, string folder, int updateInterval)
    {
        Texture2D channelTex = new(width, height, TextureFormat.R8, false, false);
        Color[] outputPixels = new Color[pixels.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = y * width + x;
                float v = channel switch
                {
                    Channel.R => pixels[i].r,
                    Channel.G => pixels[i].g,
                    Channel.B => pixels[i].b,
                    Channel.A => pixels[i].a,
                    _ => 0f
                };
                outputPixels[i] = new Color(v, v, v, 1f);
            }
            if (y % updateInterval == 0)
            {
                EditorUtility.DisplayProgressBar("Channel Packer", $"Unpacking {channel} channel, row {y}/{height}", (float)y / height);
            }
        }

        channelTex.SetPixels(outputPixels);
        channelTex.Apply();

        string fileName = $"{unpackSource.name}_{channel}.png";
        string path = Path.Combine(folder, fileName);
        File.WriteAllBytes(path, channelTex.EncodeToPNG());
        Debug.Log($"Saved {channel} channel to {path}");
        DestroyImmediate(channelTex);
    }

    private bool CheckTextureReadable(Texture2D tex, string texName, out bool tryAgain)
    {
        tryAgain = false;
        if (tex == null)
            return true;

        string path = AssetDatabase.GetAssetPath(tex);
        if (string.IsNullOrEmpty(path))
        {
            EditorUtility.DisplayDialog("Error", $"Texture '{texName}' is not an imported asset.", "OK");
            return false;
        }

        TextureImporter ti = TextureImporter.GetAtPath(path) as TextureImporter;
        if (ti == null)
            return true;

        if (!ti.isReadable)
        {
            tryAgain = true;
            return false;
        }

        return true;
    }
}
