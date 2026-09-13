#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SDG.Unturned.Tools;

namespace Kanomjeen.EditorTools
{
    public static class KanomjeenUiBuilder
    {
        private const string OutputFolder = "Assets/KanomjeenUI/Effects/KanomjeenUI";
        private const string OutputPrefab = OutputFolder + "/Effect.prefab";
        private const string PreferredFontPath = "Assets/KanomjeenUI/Fonts/KanomjeenThai.ttf";
        private const string MasterBundleName = "kanomjeen_ui.masterbundle";
        private const string MasterBundleOutputFolder = "WorkshopExport";

        private static readonly Color Backdrop = new Color32(8, 12, 15, 220);
        private static readonly Color Surface = new Color32(22, 29, 35, 250);
        private static readonly Color Surface2 = new Color32(31, 40, 47, 255);
        private static readonly Color Accent = new Color32(229, 172, 71, 255);
        private static readonly Color Text = new Color32(241, 245, 247, 255);
        private static readonly Color Muted = new Color32(157, 169, 176, 255);
        private static readonly Color Danger = new Color32(196, 78, 78, 255);
        private static readonly Color Success = new Color32(78, 161, 111, 255);
        private static Font _font;

        [MenuItem("Kanomjeen/Build Workshop UI Prefab")]
        public static void Build()
        {
            Directory.CreateDirectory(OutputFolder);
            _font = AssetDatabase.LoadAssetAtPath<Font>(PreferredFontPath);
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                Debug.LogWarning("[Kanomjeen] Thai font asset not found at " + PreferredFontPath + ". UI will build with fallback font, but Thai glyph coverage must be validated before Workshop release.");
            }

            var root = new GameObject("Effect", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var dim = Panel(root.transform, "KJ_Dim", Backdrop, Vector2.zero, new Vector2(1920, 1080));
            Stretch(dim.GetComponent<RectTransform>());

            var shell = Panel(root.transform, "KJ_Root", Surface, new Vector2(0, 0), new Vector2(980, 700));
            AnchorCenter(shell.GetComponent<RectTransform>());
            AddAccent(shell.transform);

            Label(shell.transform, "KJ_Brand", "KANOMJEEN", 18, Text, new Vector2(-390, 314), new Vector2(160, 28), TextAnchor.MiddleLeft, FontStyle.Bold);
            Label(shell.transform, "KJ_Title", "KANOMJEEN", 30, Text, new Vector2(-390, 266), new Vector2(650, 48), TextAnchor.MiddleLeft, FontStyle.Bold);
            Label(shell.transform, "KJ_Status", "Semi-Vanilla Survival • California 2", 15, Muted, new Vector2(-390, 226), new Vector2(730, 34), TextAnchor.MiddleLeft, FontStyle.Normal);
            Button(shell.transform, "KJ_Close", "CLOSE", new Vector2(394, 304), new Vector2(120, 42), Surface2, Text);
            Label(shell.transform, "KJ_ContractVersion", "1.0", 11, Muted, new Vector2(424, -325), new Vector2(70, 22), TextAnchor.MiddleRight, FontStyle.Normal);

            BuildMain(shell.transform);
            BuildWaypoints(shell.transform);
            BuildTpa(shell.transform);
            BuildHomes(shell.transform);
            BuildKits(shell.transform);
            BuildStats(shell.transform);
            BuildAirdrop(shell.transform);
            BuildAdmin(shell.transform);
            BuildToast(shell.transform);

            ValidateTextLayout(root);
            PrefabUtility.SaveAsPrefabAsset(root, OutputPrefab);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Kanomjeen] Workshop UI prefab created: " + OutputPrefab);
        }

        [MenuItem("Kanomjeen/Export Workshop UI Master Bundle")]
        public static void ExportMasterBundle()
        {
            Build();

            var importer = AssetImporter.GetAtPath(OutputPrefab);
            if (importer == null)
            {
                Debug.LogError("[Kanomjeen] Prefab not found after rebuilding the Workshop UI prefab.");
                return;
            }

            importer.assetBundleName = MasterBundleName;
            importer.SaveAndReimport();
            AssetDatabase.RemoveUnusedAssetBundleNames();

            var outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", MasterBundleOutputFolder));
            Directory.CreateDirectory(outputPath);
            EditorAssetBundleHelper.Build(MasterBundleName, outputPath, true);
            Debug.Log("[Kanomjeen] Workshop UI master bundle export completed: " + outputPath);
        }

        private static void BuildMain(Transform parent)
        {
            var screen = Screen(parent, "main");
            Label(screen, "KJ_Main_Intro", "SURVIVAL SERVICES", 13, Accent, new Vector2(-392, 180), new Vector2(350, 28), TextAnchor.MiddleLeft, FontStyle.Bold);
            CardButton(screen, "KJ_Main_TPA", "TPA", "Player-to-player travel\nCombat / raid protected", -205, 108);
            CardButton(screen, "KJ_Main_Waypoints", "WAYPOINTS", "Save and track destinations\nNative map-marker fallback", 205, 108);
            CardButton(screen, "KJ_Main_Homes", "HOMES", "Track or teleport separately\nRestricted-zone aware", -205, -16);
            CardButton(screen, "KJ_Main_Kits", "KITS", "Survival utility only\nNo pay-to-win loadouts", 205, -16);
            CardButton(screen, "KJ_Main_Stats", "STATS", "Kills, deaths, KDR\nPlaytime and survival", -205, -140);
            CardButton(screen, "KJ_Main_Airdrop", "AIRDROP", "Live PvP objective\nTemporary red marker", 205, -140);
            CardButton(screen, "KJ_Main_Admin", "STAFF", "Permission restricted", 0, -272);
        }

        private static void BuildWaypoints(Transform parent)
        {
            var screen = Screen(parent, "waypoints");
            Label(screen, "KJ_Waypoint_Help", "Use /wp add <name> at your position. Select TRACK to send the destination to Unturned's native map.", 14, Muted, new Vector2(0, 190), new Vector2(790, 34), TextAnchor.MiddleCenter, FontStyle.Normal);
            for (var i = 0; i < 8; i++)
            {
                var y = 146 - (i * 54);
                var row = Panel(screen, "KJ_Waypoint_Row_" + i, Surface2, new Vector2(0, y), new Vector2(790, 48));
                Label(row.transform, "KJ_Waypoint_Name_" + i, "WAYPOINT", 15, Text, new Vector2(-365, 0), new Vector2(430, 36), TextAnchor.MiddleLeft, FontStyle.Bold);
                Button(row.transform, "KJ_Waypoint_Track_" + i, "TRACK", new Vector2(225, 0), new Vector2(120, 40), Success, Text);
                Button(row.transform, "KJ_Waypoint_Delete_" + i, "DELETE", new Vector2(330, 0), new Vector2(84, 40), Danger, Text);
            }
            Button(screen, "KJ_Waypoint_Stop", "STOP TRACKING", new Vector2(0, -300), new Vector2(210, 44), Danger, Text);
        }

        private static void BuildTpa(Transform parent)
        {
            var screen = Screen(parent, "tpa");
            Label(screen, "KJ_TPA_Help", "Use /tpa <player> to request travel or /tpahere <player> to invite them.\nA successful teleport starts the persistent cooldown.", 17, Text, new Vector2(0, 145), new Vector2(760, 70), TextAnchor.MiddleCenter, FontStyle.Normal);
            var panel = Panel(screen, "KJ_TPA_RequestPanel", Surface2, new Vector2(0, -30), new Vector2(760, 250));
            Label(panel.transform, "KJ_TPA_Label", "INCOMING REQUEST", 13, Accent, new Vector2(0, 86), new Vector2(400, 26), TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(panel.transform, "KJ_TPA_Requester", "PLAYER", 28, Text, new Vector2(0, 44), new Vector2(520, 46), TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(panel.transform, "KJ_TPA_Timer", "30s", 14, Muted, new Vector2(0, 8), new Vector2(200, 24), TextAnchor.MiddleCenter, FontStyle.Normal);
            Button(panel.transform, "KJ_TPA_Accept", "ACCEPT", new Vector2(-200, -70), new Vector2(180, 48), Success, Text);
            Button(panel.transform, "KJ_TPA_Deny", "DENY", new Vector2(0, -70), new Vector2(180, 48), Danger, Text);
            Button(panel.transform, "KJ_TPA_Cancel", "CANCEL MINE", new Vector2(200, -70), new Vector2(180, 48), Surface, Text);
        }

        private static void BuildHomes(Transform parent)
        {
            var screen = Screen(parent, "homes");
            for (var i = 0; i < 6; i++)
            {
                var y = 170 - (i * 74);
                var row = Panel(screen, "KJ_Home_Row_" + i, Surface2, new Vector2(0, y), new Vector2(790, 60));
                Label(row.transform, "KJ_Home_Name_" + i, "HOME " + (i + 1), 17, Text, new Vector2(-280, 0), new Vector2(220, 40), TextAnchor.MiddleLeft, FontStyle.Bold);
                Button(row.transform, "KJ_Home_Track_" + i, "TRACK", new Vector2(100, 0), new Vector2(100, 40), Success, Text);
                Button(row.transform, "KJ_Home_Teleport_" + i, "TELEPORT", new Vector2(220, 0), new Vector2(130, 40), Accent, new Color32(20, 20, 20, 255));
                Button(row.transform, "KJ_Home_Delete_" + i, "DELETE", new Vector2(335, 0), new Vector2(90, 40), Danger, Text);
            }
            Button(screen, "KJ_Home_Add", "+ CREATE HOME", new Vector2(0, -286), new Vector2(220, 44), Surface2, Text);
        }

        private static void BuildKits(Transform parent)
        {
            var screen = Screen(parent, "kits");
            for (var i = 0; i < 8; i++)
            {
                var y = 184 - (i * 58);
                var row = Panel(screen, "KJ_Kit_Row_" + i, Surface2, new Vector2(0, y), new Vector2(790, 48));
                Label(row.transform, "KJ_Kit_Name_" + i, "KIT", 16, Text, new Vector2(-285, 0), new Vector2(250, 36), TextAnchor.MiddleLeft, FontStyle.Bold);
                Label(row.transform, "KJ_Kit_Cooldown_" + i, "READY", 13, Muted, new Vector2(90, 0), new Vector2(160, 36), TextAnchor.MiddleRight, FontStyle.Normal);
                Button(row.transform, "KJ_Kit_Claim_" + i, "CLAIM", new Vector2(300, 0), new Vector2(130, 36), Accent, new Color32(20, 20, 20, 255));
            }
        }

        private static void BuildStats(Transform parent)
        {
            var screen = Screen(parent, "stats");
            StatCard(screen, "KILLS", "KJ_Stats_Kills", -260, 130);
            StatCard(screen, "DEATHS", "KJ_Stats_Deaths", 0, 130);
            StatCard(screen, "KDR", "KJ_Stats_KDR", 260, 130);
            StatCard(screen, "ZOMBIES", "KJ_Stats_Zombies", -260, -25);
            StatCard(screen, "HEADSHOTS", "KJ_Stats_Headshots", 0, -25);
            StatCard(screen, "AIRDROPS", "KJ_Stats_Airdrops", 260, -25);
            WideStat(screen, "PLAYTIME", "KJ_Stats_Playtime", -190);
            WideStat(screen, "LONGEST LIFE", "KJ_Stats_LongestLife", -245);
        }

        private static void BuildAirdrop(Transform parent)
        {
            var screen = Screen(parent, "airdrop");
            Label(screen, "KJ_Airdrop_State", "STANDBY", 15, Accent, new Vector2(0, 150), new Vector2(300, 30), TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(screen, "KJ_Airdrop_Region", "NO ACTIVE DROP", 32, Text, new Vector2(0, 95), new Vector2(720, 52), TextAnchor.MiddleCenter, FontStyle.Bold);
            var card = Panel(screen, "KJ_Airdrop_Card", Surface2, new Vector2(0, -45), new Vector2(650, 180));
            Label(card.transform, "KJ_Airdrop_DistanceLabel", "DISTANCE", 12, Muted, new Vector2(-165, 45), new Vector2(220, 24), TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(card.transform, "KJ_Airdrop_Distance", "-", 28, Text, new Vector2(-165, 0), new Vector2(220, 50), TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(card.transform, "KJ_Airdrop_TimerLabel", "OBJECTIVE TIMER", 12, Muted, new Vector2(165, 45), new Vector2(220, 24), TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(card.transform, "KJ_Airdrop_Timer", "-", 28, Text, new Vector2(165, 0), new Vector2(220, 50), TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(screen, "KJ_Airdrop_Warning", "TPA / HOME / KIT and configured building actions are blocked inside the active objective radius.", 15, Muted, new Vector2(0, -190), new Vector2(760, 50), TextAnchor.MiddleCenter, FontStyle.Normal);
        }

        private static void BuildAdmin(Transform parent)
        {
            var screen = Screen(parent, "admin");
            Label(screen, "KJ_Admin_Name", "STAFF", 28, Text, new Vector2(0, 135), new Vector2(500, 44), TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(screen, "KJ_Admin_State", "God=False  Vanish=False", 15, Muted, new Vector2(0, 90), new Vector2(560, 30), TextAnchor.MiddleCenter, FontStyle.Normal);
            Button(screen, "KJ_Admin_God", "TOGGLE GOD", new Vector2(-150, 10), new Vector2(240, 54), Surface2, Text);
            Button(screen, "KJ_Admin_Vanish", "TOGGLE VANISH", new Vector2(150, 10), new Vector2(240, 54), Surface2, Text);
            Label(screen, "KJ_Admin_Help", "Targeted moderation remains command-based for clear audit logs:\n/warn  /mute  /kick  /kjban  /kjunban  /inspect", 16, Text, new Vector2(0, -120), new Vector2(720, 90), TextAnchor.MiddleCenter, FontStyle.Normal);
        }

        private static void BuildToast(Transform parent)
        {
            var screen = Screen(parent, "toast");
            var toast = Panel(screen, "KJ_Toast", Surface2, new Vector2(0, 0), new Vector2(650, 120));
            Label(toast.transform, "KJ_Toast_Title", "KANOMJEEN", 16, Accent, new Vector2(0, 25), new Vector2(560, 28), TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(toast.transform, "KJ_Toast_Body", "Notification", 17, Text, new Vector2(0, -15), new Vector2(560, 48), TextAnchor.MiddleCenter, FontStyle.Normal);
        }

        private static Transform Screen(Transform parent, string name)
        {
            var go = new GameObject("KJ_Screen_" + name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(900, 500);
            rect.anchoredPosition = new Vector2(0, -35);
            return go.transform;
        }

        private static void CardButton(Transform parent, string name, string title, string subtitle, float x, float y)
        {
            var card = Button(parent, name, string.Empty, new Vector2(x, y), new Vector2(370, 112), Surface2, Text);
            Label(card.transform, name + "_Title", title, 22, Text, new Vector2(-145, 25), new Vector2(280, 36), TextAnchor.MiddleLeft, FontStyle.Bold);
            Label(card.transform, name + "_Subtitle", subtitle, 14, Muted, new Vector2(-145, -25), new Vector2(290, 52), TextAnchor.MiddleLeft, FontStyle.Normal);
            var marker = Panel(card.transform, name + "_Marker", Accent, new Vector2(-176, 0), new Vector2(4, 95));
            marker.GetComponent<Image>().raycastTarget = false;
        }

        private static void StatCard(Transform parent, string label, string valueName, float x, float y)
        {
            var card = Panel(parent, valueName + "_Card", Surface2, new Vector2(x, y), new Vector2(230, 120));
            Label(card.transform, valueName + "_Label", label, 12, Muted, new Vector2(0, 31), new Vector2(180, 24), TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(card.transform, valueName, "0", 30, Text, new Vector2(0, -12), new Vector2(190, 48), TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private static void WideStat(Transform parent, string label, string valueName, float y)
        {
            var card = Panel(parent, valueName + "_Card", Surface2, new Vector2(0, y), new Vector2(750, 44));
            Label(card.transform, valueName + "_Label", label, 12, Muted, new Vector2(-270, 0), new Vector2(180, 30), TextAnchor.MiddleLeft, FontStyle.Bold);
            Label(card.transform, valueName, "0m 0s", 17, Text, new Vector2(230, 0), new Vector2(250, 30), TextAnchor.MiddleRight, FontStyle.Bold);
        }

        private static GameObject Panel(Transform parent, string name, Color color, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
            var image = go.GetComponent<Image>(); image.color = color;
            return go;
        }

        private static GameObject Button(Transform parent, string name, string caption, Vector2 pos, Vector2 size, Color color, Color textColor)
        {
            var go = Panel(parent, name, color, pos, size);
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            var colors = button.colors;
            colors.highlightedColor = Brighten(color, 1.12f);
            colors.pressedColor = Brighten(color, 0.82f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            if (!string.IsNullOrEmpty(caption)) Label(go.transform, name + "_Text", caption, 14, textColor, Vector2.zero, size - new Vector2(12, 8), TextAnchor.MiddleCenter, FontStyle.Bold);
            return go;
        }

        private static Text Label(Transform parent, string name, string text, int size, Color color, Vector2 pos, Vector2 rectSize, TextAnchor anchor, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);

            // IMPORTANT: `pos` is treated as the aligned edge/point, not always the
            // centre of the text rect. The previous implementation forced a 0.5/0.5
            // pivot for every label. For MiddleLeft labels this shifted the rendered
            // text left by half of rectSize.x, which is why card titles/subtitles were
            // visibly outside their panels in-game. Match the RectTransform pivot to
            // the TextAnchor so the authored coordinates remain intuitive/stable.
            rect.pivot = PivotFor(anchor);
            rect.anchoredPosition = pos;
            rect.sizeDelta = rectSize;

            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = _font;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = anchor;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            return label;
        }

        private static void ValidateTextLayout(GameObject root)
        {
            var labels = root.GetComponentsInChildren<Text>(true);
            var failures = 0;
            for (var i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                var rect = label.rectTransform;
                var expected = PivotFor(label.alignment);
                if (Vector2.SqrMagnitude(rect.pivot - expected) <= 0.0001f) continue;

                failures++;
                Debug.LogError("[Kanomjeen] Text pivot mismatch: " + GetHierarchyPath(label.transform)
                    + " alignment=" + label.alignment
                    + " pivot=" + rect.pivot
                    + " expected=" + expected);
            }

            if (failures > 0)
                throw new System.InvalidOperationException("Kanomjeen UI layout validation failed for " + failures + " text element(s). Fix pivots before exporting the Workshop bundle.");
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }

        private static Vector2 PivotFor(TextAnchor anchor)
        {
            float x;
            switch (anchor)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.LowerLeft:
                    x = 0f;
                    break;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    x = 1f;
                    break;
                default:
                    x = 0.5f;
                    break;
            }

            float y;
            switch (anchor)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.UpperCenter:
                case TextAnchor.UpperRight:
                    y = 1f;
                    break;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    y = 0f;
                    break;
                default:
                    y = 0.5f;
                    break;
            }

            return new Vector2(x, y);
        }

        private static void AddAccent(Transform shell)
        {
            var accent = Panel(shell, "KJ_Accent", Accent, new Vector2(-487, 0), new Vector2(6, 700));
            accent.GetComponent<Image>().raycastTarget = false;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void AnchorCenter(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        private static Color Brighten(Color color, float multiplier)
        {
            return new Color(Mathf.Clamp01(color.r * multiplier), Mathf.Clamp01(color.g * multiplier), Mathf.Clamp01(color.b * multiplier), color.a);
        }
    }
}
#endif
