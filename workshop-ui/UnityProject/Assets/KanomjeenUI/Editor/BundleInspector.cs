#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Kanomjeen.EditorTools
{
    /// <summary>
    /// Dumps everything inside an Unturned Effect master bundle as readable text: the asset list, every
    /// GameObject's RectTransform geometry, and each Image / Text / Button / CanvasScaler setting.
    ///
    /// Purpose: verify what the *exported bundle* actually contains (the Editor prefab is not proof —
    /// see AGENTS.md §13), and study a third-party bundle's layout numbers without opening its project.
    ///
    /// Batch usage:
    /// <code>
    /// KJ_BUNDLE=&lt;bundle path&gt; KJ_DUMP=&lt;output .txt&gt; Unity.exe -batchmode -nographics -quit \
    ///   -projectPath &lt;project&gt; -executeMethod Kanomjeen.EditorTools.BundleInspector.Dump
    /// </code>
    /// Menu usage: select any file with the picker under Kanomjeen ▸ Inspect Bundle…
    /// </summary>
    public static class BundleInspector
    {
        [MenuItem("Kanomjeen/Inspect Bundle...")]
        public static void DumpFromPicker()
        {
            var bundlePath = EditorUtility.OpenFilePanel("Inspect Unturned bundle", "", "masterbundle");
            if (string.IsNullOrEmpty(bundlePath)) return;
            DumpBundle(bundlePath, Path.ChangeExtension(bundlePath, null) + ".dump.txt");
        }

        /// <summary>Entry point used by Unity's <c>-executeMethod</c>; reads KJ_BUNDLE and KJ_DUMP.</summary>
        public static void Dump()
        {
            var bundlePath = Environment.GetEnvironmentVariable("KJ_BUNDLE");
            var outputPath = Environment.GetEnvironmentVariable("KJ_DUMP");
            if (string.IsNullOrEmpty(bundlePath))
            {
                Debug.LogError("[Kanomjeen] KJ_BUNDLE is not set.");
                EditorApplication.Exit(1);
                return;
            }
            if (string.IsNullOrEmpty(outputPath))
                outputPath = Path.ChangeExtension(bundlePath, null) + ".dump.txt";

            try
            {
                DumpBundle(bundlePath, outputPath);
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Kanomjeen] Bundle dump failed: " + ex);
                EditorApplication.Exit(1);
            }
        }

        private static void DumpBundle(string bundlePath, string outputPath)
        {
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
                throw new InvalidOperationException("Could not load bundle at " + bundlePath);

            var text = new StringBuilder();
            text.AppendLine("# Bundle: " + bundlePath);
            text.AppendLine("# Bytes: " + new FileInfo(bundlePath).Length);
            text.AppendLine("# Unity load result: " + bundle.name + " (" + bundle.GetAllAssetNames().Length + " named assets)");
            text.AppendLine();

            var all = bundle.LoadAllAssets();
            var byType = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var asset in all)
            {
                if (asset == null) continue;
                var type = asset.GetType().Name;
                byType.TryGetValue(type, out var count);
                byType[type] = count + 1;
            }
            text.AppendLine("## Asset type counts");
            foreach (var pair in byType) text.AppendLine("  " + pair.Key + " x" + pair.Value);
            text.AppendLine();

            // AssetBundle.LoadAllAssets() does not return Font assets for prefab dependencies, so the
            // fonts that actually ship are collected from the components that reference them. This is
            // the release gate for Thai text: a missing font renders as boxes on the client.
            text.AppendLine("## Fonts (reachable from Text components)");
            var fonts = new Dictionary<string, Font>(StringComparer.Ordinal);
            foreach (var asset in all)
            {
                var gameObject = asset as GameObject;
                if (gameObject == null) continue;
                foreach (var label in gameObject.GetComponentsInChildren<Text>(true))
                {
                    if (label.font == null) continue;
                    var key = label.font.name + "|" + label.font.GetInstanceID();
                    fonts[key] = label.font;
                }
            }
            foreach (var font in fonts.Values)
            {
                var names = font.fontNames != null && font.fontNames.Length > 0 ? string.Join(" | ", font.fontNames) : "(none)";
                text.AppendLine("  Font asset=\"" + font.name + "\" fontNames=[" + names + "] dynamic=" + font.dynamic + " size=" + font.fontSize);
            }
            text.AppendLine();

            text.AppendLine("## Textures");
            foreach (var asset in all)
            {
                var texture = asset as Texture2D;
                if (texture == null) continue;
                text.AppendLine("  Texture \"" + texture.name + "\" " + texture.width + "x" + texture.height + " " + texture.format);
            }
            text.AppendLine();

            text.AppendLine("## Sprites");
            foreach (var asset in all)
            {
                var sprite = asset as Sprite;
                if (sprite == null) continue;
                var rect = sprite.rect;
                text.AppendLine("  Sprite \"" + sprite.name + "\" " + rect.width + "x" + rect.height + " texture=" + (sprite.texture != null ? sprite.texture.name : "(none)"));
            }
            text.AppendLine();

            text.AppendLine("## GameObject hierarchy");
            foreach (var asset in all)
            {
                var root = asset as GameObject;
                if (root == null || root.transform.parent != null) continue;
                WriteGameObject(text, root.transform, 1);
            }

            File.WriteAllText(outputPath, text.ToString());
            Debug.Log("[Kanomjeen] Wrote bundle dump: " + outputPath);
            bundle.Unload(true);
        }

        private static void WriteGameObject(StringBuilder text, Transform transform, int depth)
        {
            var indent = new string(' ', depth * 2);
            text.Append(indent).Append("GO \"").Append(transform.name).Append('"').Append(transform.gameObject.activeSelf ? "" : " (inactive)");

            var rect = transform as RectTransform;
            if (rect != null)
            {
                text.Append(" rect pos=(").Append(N(rect.anchoredPosition)).Append(") size=(").Append(N(rect.sizeDelta)).Append(")")
                    .Append(" anchor=(").Append(N(rect.anchorMin)).Append('-').Append(N(rect.anchorMax)).Append(")")
                    .Append(" pivot=(").Append(N(rect.pivot)).Append(')');
            }
            text.AppendLine();

            foreach (var component in transform.GetComponents<Component>())
            {
                if (component == null) continue;
                var line = Describe(component, depth + 1);
                if (line != null) text.AppendLine(line);
            }

            for (var i = 0; i < transform.childCount; i++)
                WriteGameObject(text, transform.GetChild(i), depth + 1);
        }

        private static string Describe(Component component, int depth)
        {
            var indent = new string(' ', depth * 2);
            var image = component as Image;
            if (image != null)
                return indent + "Image color=" + Hex(image.color) + " sprite=" + (image.sprite != null ? "\"" + image.sprite.name + "\"" : "(none)") + " type=" + image.type + " raycast=" + image.raycastTarget;

            var text = component as Text;
            if (text != null)
                return indent + "Text \"" + text.text + "\" size=" + text.fontSize + " style=" + text.fontStyle + " color=" + Hex(text.color)
                    + " align=" + text.alignment + " font=" + (text.font != null ? "\"" + text.font.name + "\" [" + (text.font.fontNames != null ? string.Join(",", text.font.fontNames) : "?") + "]" : "(none)")
                    + " wrap=" + text.horizontalOverflow + "/" + text.verticalOverflow;

            var button = component as Button;
            if (button != null)
            {
                var colors = button.colors;
                return indent + "Button normal=" + Hex(colors.normalColor) + " highlighted=" + Hex(colors.highlightedColor)
                    + " pressed=" + Hex(colors.pressedColor) + " disabled=" + Hex(colors.disabledColor) + " multiplier=" + colors.colorMultiplier;
            }

            var scaler = component as CanvasScaler;
            if (scaler != null)
                return indent + "CanvasScaler mode=" + scaler.uiScaleMode + " reference=" + N(scaler.referenceResolution) + " match=" + scaler.screenMatchMode + "/" + scaler.matchWidthOrHeight;

            var canvas = component as Canvas;
            if (canvas != null)
                return indent + "Canvas renderMode=" + canvas.renderMode + " sortingOrder=" + canvas.sortingOrder;

            var outline = component as Outline;
            if (outline != null) return indent + "Outline color=" + Hex(outline.effectColor) + " distance=(" + N(outline.effectDistance) + ")";

            var shadow = component as Shadow;
            if (shadow != null) return indent + "Shadow color=" + Hex(shadow.effectColor) + " distance=(" + N(shadow.effectDistance) + ")";

            var layout = component as LayoutElement;
            if (layout != null) return indent + "LayoutElement min=(" + N(new Vector2(layout.minWidth, layout.minHeight)) + ") preferred=(" + N(new Vector2(layout.preferredWidth, layout.preferredHeight)) + ")";

            var grid = component as GridLayoutGroup;
            if (grid != null) return indent + "GridLayoutGroup cell=(" + N(grid.cellSize) + ") spacing=(" + N(grid.spacing) + ")";

            var group = component as HorizontalOrVerticalLayoutGroup;
            if (group != null) return indent + "LayoutGroup spacing=" + group.spacing + " padding=" + group.padding;

            if (component is RectTransform || component is CanvasRenderer) return null;
            return indent + component.GetType().Name;
        }

        private static string Hex(Color color)
        {
            return "#" + Mathf.RoundToInt(color.r * 255f).ToString("X2") + Mathf.RoundToInt(color.g * 255f).ToString("X2")
                 + Mathf.RoundToInt(color.b * 255f).ToString("X2") + Mathf.RoundToInt(color.a * 255f).ToString("X2");
        }

        private static string N(Vector2 value)
        {
            return value.x.ToString("0.##") + ", " + value.y.ToString("0.##");
        }
    }
}
#endif
