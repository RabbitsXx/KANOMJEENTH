#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SDG.Unturned.Tools;

namespace Kanomjeen.EditorTools
{
    /// <summary>
    /// Builds the Workshop Effect prefab (`.prefab`) that the plugins drive at runtime.
    ///
    /// Design tokens are measured from two commercial reference Effects (Supernovea Itemshop V2,
    /// Supernovea RankQuest V2) whose shipped bundles were read with
    /// <see cref="BundleInspector"/>: Kanit typography, #212121 cards on a translucent dark table,
    /// an opaque black header band, the built-in nine-slice UI sprite, and a white/grey
    /// Button ColorBlock where colour lives on the Image. See workshop-ui/DESIGN_SYSTEM.md.
    ///
    /// Element names beginning `KJ_` are the server API contract (UI_CONTRACT.md). This file may change
    /// geometry, colour and typography freely, but renaming a bound element is a contract change.
    /// </summary>
    public static class KanomjeenUiBuilder
    {
        private const string OutputFolder = "Assets/KanomjeenUI/Effects/KanomjeenUI";
        private const string OutputPrefab = OutputFolder + "/Effect.prefab";
        private const string FontFolder = "Assets/KanomjeenUI/Fonts/";
        private const string BodyFontPath = FontFolder + "Kanit-Regular.ttf";
        private const string StrongFontPath = FontFolder + "Kanit-SemiBold.ttf";
        private const string DisplayFontPath = FontFolder + "Kanit-Bold.ttf";
        private const string MasterBundleName = "kanomjeen_ui.masterbundle";
        private const string MasterBundleOutputFolder = "WorkshopExport";

        // ---- Measured reference palette -------------------------------------------------------
        private static readonly Color Backdrop = Rgba(0, 0, 0, 0xDD);            // full-screen dim
        private static readonly Color Table = Rgba(0x1A, 0x1A, 0x1A, 0xE6);      // ref table surface
        private static readonly Color HeaderBar = Rgba(0x00, 0x00, 0x00, 0xFF);  // ref opaque header
        private static readonly Color Card = Rgba(0x21, 0x21, 0x21, 0xFF);       // ref card surface
        private static readonly Color Inset = Rgba(0x16, 0x16, 0x16, 0xFF);      // recessed list area
        private static readonly Color Neutral = Rgba(0x3C, 0x3C, 0x3C, 0xFF);    // secondary surface
        private static readonly Color Accent = Rgba(0x4A, 0xDC, 0x43, 0xFF);     // ref affirmative
        private static readonly Color Danger = Rgba(0x9F, 0x1B, 0x1B, 0xFF);     // ref destructive
        private static readonly Color Warning = Rgba(0xFF, 0x95, 0x00, 0xFF);    // ref warning
        private static readonly Color Text = Rgba(0xFF, 0xFF, 0xFF, 0xFF);
        private static readonly Color TextDim = Rgba(0xDB, 0xDB, 0xDB, 0xFF);
        private static readonly Color Muted = Rgba(0x9E, 0x9E, 0x9E, 0xFF);
        private static readonly Color OnDark = Rgba(0x10, 0x10, 0x10, 0xFF);     // text on accent fill
        private static readonly Color Rule = Rgba(0xFF, 0xFF, 0xFF, 0x1E);       // hairline dividers

        // ---- Layout scale (8px grid) -----------------------------------------------------------
        private const float ShellWidth = 1180f;
        private const float ShellHeight = 700f;
        private const float ContentWidth = 1100f;
        private const float ContentHeight = 496f;
        private const float ContentCenterY = -52f;
        private const float HeaderHeight = 96f;
        private const float RowPitch = 54f;
        private const float Pad = 8f;

        private static Font _body;
        private static Font _strong;
        private static Font _display;
        private static Sprite _panelSprite;
        private static Sprite _insetSprite;

        [MenuItem("Kanomjeen/Build Workshop UI Prefab")]
        public static void Build()
        {
            Directory.CreateDirectory(OutputFolder);
            LoadFonts();
            LoadSprites();

            var root = new GameObject("Effect", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            // The reference Effects match width; height follows so the table keeps its proportions
            // on 16:10 and ultra-wide displays.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            var dim = Panel(root.transform, "KJ_Dim", Backdrop, Vector2.zero, new Vector2(1920, 1080));
            Stretch(dim.GetComponent<RectTransform>());

            var shell = Panel(root.transform, "KJ_Root", Table, Vector2.zero, new Vector2(ShellWidth, ShellHeight));
            AnchorCenter(shell.GetComponent<RectTransform>());
            AddAccent(shell.transform);
            BuildHeader(shell.transform);

            BuildMain(shell.transform);
            BuildWaypoints(shell.transform);
            BuildTpa(shell.transform);
            BuildHomes(shell.transform);
            BuildKits(shell.transform);
            BuildStats(shell.transform);
            BuildAirdrop(shell.transform);
            BuildAdmin(shell.transform);
            BuildToast(shell.transform);

            Label(shell.transform, "KJ_ContractVersion", "1.0", 14, Muted, new Vector2(ShellWidth / 2f - 40f, -ShellHeight / 2f + 26f), new Vector2(200, 24), TextAnchor.MiddleRight, FontStyle.Normal, _body);
            Label(shell.transform, "KJ_FooterNote", "Server-validated • /menu or /kjmenu", 13, Muted, new Vector2(-(ShellWidth / 2f - 40f), -ShellHeight / 2f + 26f), new Vector2(560, 24), TextAnchor.MiddleLeft, FontStyle.Normal, _body);

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

        // ---- Frame -----------------------------------------------------------------------------

        private static void BuildHeader(Transform shell)
        {
            var bar = Panel(shell, "KJ_HeaderBar", HeaderBar, new Vector2(0, (ShellHeight - HeaderHeight) / 2f), new Vector2(ShellWidth, HeaderHeight));
            bar.GetComponent<Image>().sprite = _panelSprite;
            bar.GetComponent<Image>().type = Image.Type.Sliced;

            Label(shell, "KJ_Brand", "KANOMJEEN", 13, Accent, new Vector2(-(ShellWidth / 2f - 40f), (ShellHeight - HeaderHeight) / 2f + 24f), new Vector2(320, 20), TextAnchor.MiddleLeft, FontStyle.Normal, _strong);
            Label(shell, "KJ_Title", "KANOMJEEN", 30, Text, new Vector2(-(ShellWidth / 2f - 40f), (ShellHeight - HeaderHeight) / 2f - 4f), new Vector2(700, 40), TextAnchor.MiddleLeft, FontStyle.Normal, _display);
            Label(shell, "KJ_Status", "Semi-Vanilla Survival • California 2", 15, TextDim, new Vector2(-(ShellWidth / 2f - 40f), (ShellHeight - HeaderHeight) / 2f - 32f), new Vector2(820, 24), TextAnchor.MiddleLeft, FontStyle.Normal, _body);

            Button(shell, "KJ_Close", "CLOSE", new Vector2(ShellWidth / 2f - 40f - 48f, (ShellHeight - HeaderHeight) / 2f), new Vector2(96, 40), Neutral, Text);

            var rule = Panel(shell, "KJ_HeaderRule", Rule, new Vector2(0, ShellHeight / 2f - HeaderHeight), new Vector2(ShellWidth, 1));
            rule.GetComponent<Image>().raycastTarget = false;
        }

        private static void AddAccent(Transform shell)
        {
            var accent = Panel(shell, "KJ_Accent", Accent, new Vector2(-(ShellWidth / 2f) + 3f, 0), new Vector2(6, ShellHeight));
            accent.GetComponent<Image>().raycastTarget = false;
        }

        // ---- Screens ---------------------------------------------------------------------------

        private static void BuildMain(Transform parent)
        {
            var screen = Screen(parent, "main");
            Label(screen, "KJ_Main_Intro", "SURVIVAL SERVICES", 13, Accent, new Vector2(-ContentWidth / 2f, 214), new Vector2(500, 22), TextAnchor.MiddleLeft, FontStyle.Normal, _strong);

            const float cardWidth = 542f;
            const float cardHeight = 120f;
            var columnX = cardWidth / 2f + Pad / 2f;
            var topRowY = 160f;
            var pitch = cardHeight + 16f;

            CardButton(screen, "KJ_Main_TPA", "TPA", "Player-to-player travel\nCombat and raid protected", -columnX, topRowY, cardWidth, cardHeight);
            CardButton(screen, "KJ_Main_Waypoints", "WAYPOINTS", "Save and track destinations\nNative map-marker fallback", columnX, topRowY, cardWidth, cardHeight);
            CardButton(screen, "KJ_Main_Homes", "HOMES", "Track or teleport separately\nRestricted-zone aware", -columnX, topRowY - pitch, cardWidth, cardHeight);
            CardButton(screen, "KJ_Main_Kits", "KITS", "Survival utility only\nNo pay-to-win loadouts", columnX, topRowY - pitch, cardWidth, cardHeight);
            CardButton(screen, "KJ_Main_Stats", "STATS", "Kills, deaths, KDR\nPlaytime and survival", -columnX, topRowY - (pitch * 2f), cardWidth, cardHeight);
            CardButton(screen, "KJ_Main_Airdrop", "AIRDROP", "Live PvP objective\nTravel restrictions inside", columnX, topRowY - (pitch * 2f), cardWidth, cardHeight);

            var staff = Button(screen, "KJ_Main_Admin", string.Empty, new Vector2(0, topRowY - (pitch * 3f) + 8f), new Vector2(ContentWidth, 64), Card, Text);
            Label(staff.transform, "KJ_Main_Admin_Title", "STAFF TOOLS", 20, Text, new Vector2(-(ContentWidth / 2f) + 20f, 12), new Vector2(420, 28), TextAnchor.MiddleLeft, FontStyle.Normal, _strong);
            Label(staff.transform, "KJ_Main_Admin_Subtitle", "Permission restricted — /inspect, /warn, /mute, /kick, /kjban", 14, Muted, new Vector2(-(ContentWidth / 2f) + 20f, -14), new Vector2(900, 24), TextAnchor.MiddleLeft, FontStyle.Normal, _body);
        }

        private static void BuildWaypoints(Transform parent)
        {
            var screen = Screen(parent, "waypoints");
            Label(screen, "KJ_Waypoint_Help", "Use /wp add <name> where you stand. TRACK sends that destination to Unturned's native map.", 15, Muted, new Vector2(-ContentWidth / 2f, 214), new Vector2(ContentWidth, 24), TextAnchor.MiddleLeft, FontStyle.Normal, _body);

            const float rowWidth = 1058f;
            const float rowHeight = 46f;
            var firstRowY = 165f;
            for (var i = 0; i < 8; i++)
            {
                var row = Panel(screen, "KJ_Waypoint_Row_" + i, Card, new Vector2(0, firstRowY - (i * RowPitch)), new Vector2(rowWidth, rowHeight));
                Label(row.transform, "KJ_Waypoint_Name_" + i, "WAYPOINT", 19, Text, new Vector2(-(rowWidth / 2f - 20f), 0), new Vector2(740, 32), TextAnchor.MiddleLeft, FontStyle.Normal, _strong);
                Button(row.transform, "KJ_Waypoint_Track_" + i, "TRACK", new Vector2(339, 0), new Vector2(132, 36), Accent, OnDark);
                Button(row.transform, "KJ_Waypoint_Delete_" + i, "DELETE", new Vector2(467, 0), new Vector2(108, 36), Danger, Text);
            }

            Button(screen, "KJ_Waypoint_Stop", "STOP TRACKING", new Vector2(0, -262), new Vector2(240, 44), Danger, Text);
        }

        private static void BuildTpa(Transform parent)
        {
            var screen = Screen(parent, "tpa");
            Label(screen, "KJ_TPA_Help", "Use /tpa <player> to request travel, or /tpahere <player> to invite them.\nA successful teleport starts the persistent cooldown.", 15, Muted, new Vector2(-ContentWidth / 2f, 208), new Vector2(ContentWidth, 48), TextAnchor.MiddleLeft, FontStyle.Normal, _body);

            var panel = Panel(screen, "KJ_TPA_RequestPanel", Card, new Vector2(0, -40), new Vector2(900, 260));
            var marker = Panel(panel.transform, "KJ_TPA_RequestPanel_Marker", Accent, new Vector2(-447, 0), new Vector2(6, 260));
            marker.GetComponent<Image>().raycastTarget = false;

            Label(panel.transform, "KJ_TPA_Label", "INCOMING REQUEST", 13, Accent, new Vector2(0, 96), new Vector2(500, 22), TextAnchor.MiddleCenter, FontStyle.Normal, _strong);
            Label(panel.transform, "KJ_TPA_Requester", "PLAYER", 30, Text, new Vector2(0, 48), new Vector2(820, 42), TextAnchor.MiddleCenter, FontStyle.Normal, _display);
            Label(panel.transform, "KJ_TPA_Timer", "30s", 16, TextDim, new Vector2(0, 10), new Vector2(400, 26), TextAnchor.MiddleCenter, FontStyle.Normal, _body);
            Button(panel.transform, "KJ_TPA_Accept", "ACCEPT", new Vector2(-232, -76), new Vector2(220, 48), Accent, OnDark);
            Button(panel.transform, "KJ_TPA_Deny", "DENY", new Vector2(0, -76), new Vector2(220, 48), Danger, Text);
            Button(panel.transform, "KJ_TPA_Cancel", "CANCEL MINE", new Vector2(232, -76), new Vector2(220, 48), Neutral, Text);

            Label(screen, "KJ_TPA_Note", "Requests expire automatically, and every teleport is re-validated on the server.", 14, Muted, new Vector2(0, -206), new Vector2(ContentWidth, 24), TextAnchor.MiddleCenter, FontStyle.Normal, _body);
        }

        private static void BuildHomes(Transform parent)
        {
            var screen = Screen(parent, "homes");
            Label(screen, "KJ_Home_Help", "Six slots are reserved for permission-based limits. Teleporting honours combat, raid and zone restrictions.", 15, Muted, new Vector2(-ContentWidth / 2f, 214), new Vector2(ContentWidth, 24), TextAnchor.MiddleLeft, FontStyle.Normal, _body);

            const float rowWidth = 1058f;
            const float rowHeight = 56f;
            var firstRowY = 160f;
            for (var i = 0; i < 6; i++)
            {
                var row = Panel(screen, "KJ_Home_Row_" + i, Card, new Vector2(0, firstRowY - (i * 64f)), new Vector2(rowWidth, rowHeight));
                Label(row.transform, "KJ_Home_Name_" + i, "HOME " + (i + 1), 20, Text, new Vector2(-(rowWidth / 2f - 20f), 0), new Vector2(420, 40), TextAnchor.MiddleLeft, FontStyle.Normal, _strong);
                Button(row.transform, "KJ_Home_Track_" + i, "TRACK", new Vector2(219, 0), new Vector2(104, 38), Neutral, Text);
                Button(row.transform, "KJ_Home_Teleport_" + i, "TELEPORT", new Vector2(344, 0), new Vector2(130, 38), Accent, OnDark);
                Button(row.transform, "KJ_Home_Delete_" + i, "DELETE", new Vector2(469, 0), new Vector2(104, 38), Danger, Text);
            }

            Button(screen, "KJ_Home_Add", "+ CREATE HOME", new Vector2(0, -216), new Vector2(300, 44), Neutral, Text);
        }

        private static void BuildKits(Transform parent)
        {
            var screen = Screen(parent, "kits");
            Label(screen, "KJ_Kit_Help", "Claim cooldowns are per kit and persist across sessions.", 15, Muted, new Vector2(-ContentWidth / 2f, 214), new Vector2(ContentWidth, 24), TextAnchor.MiddleLeft, FontStyle.Normal, _body);

            const float rowWidth = 1058f;
            const float rowHeight = 46f;
            var firstRowY = 165f;
            for (var i = 0; i < 8; i++)
            {
                var row = Panel(screen, "KJ_Kit_Row_" + i, Card, new Vector2(0, firstRowY - (i * RowPitch)), new Vector2(rowWidth, rowHeight));
                Label(row.transform, "KJ_Kit_Name_" + i, "KIT", 19, Text, new Vector2(-(rowWidth / 2f - 20f), 0), new Vector2(560, 32), TextAnchor.MiddleLeft, FontStyle.Normal, _strong);
                Label(row.transform, "KJ_Kit_Cooldown_" + i, "READY", 15, Muted, new Vector2(240, 0), new Vector2(200, 32), TextAnchor.MiddleRight, FontStyle.Normal, _body);
                Button(row.transform, "KJ_Kit_Claim_" + i, "CLAIM", new Vector2(461, 0), new Vector2(120, 36), Accent, OnDark);
            }
        }

        private static void BuildStats(Transform parent)
        {
            var screen = Screen(parent, "stats");
            StatCard(screen, "KILLS", "KJ_Stats_Kills", -360, 152);
            StatCard(screen, "DEATHS", "KJ_Stats_Deaths", 0, 152);
            StatCard(screen, "KDR", "KJ_Stats_KDR", 360, 152);
            StatCard(screen, "ZOMBIES", "KJ_Stats_Zombies", -360, 14);
            StatCard(screen, "HEADSHOTS", "KJ_Stats_Headshots", 0, 14);
            StatCard(screen, "AIRDROPS", "KJ_Stats_Airdrops", 360, 14);
            WideStat(screen, "PLAYTIME", "KJ_Stats_Playtime", -78);
            WideStat(screen, "LONGEST LIFE", "KJ_Stats_LongestLife", -134);
            Label(screen, "KJ_Stats_Note", "Statistics persist across sessions and are stored server-side.", 14, Muted, new Vector2(0, -212), new Vector2(ContentWidth, 24), TextAnchor.MiddleCenter, FontStyle.Normal, _body);
        }

        private static void BuildAirdrop(Transform parent)
        {
            var screen = Screen(parent, "airdrop");
            Label(screen, "KJ_Airdrop_State", "STANDBY", 14, Accent, new Vector2(0, 200), new Vector2(400, 24), TextAnchor.MiddleCenter, FontStyle.Normal, _strong);
            Label(screen, "KJ_Airdrop_Region", "NO ACTIVE DROP", 30, Text, new Vector2(0, 152), new Vector2(ContentWidth, 42), TextAnchor.MiddleCenter, FontStyle.Normal, _display);

            var card = Panel(screen, "KJ_Airdrop_Card", Card, new Vector2(0, -20), new Vector2(900, 220));
            Label(card.transform, "KJ_Airdrop_DistanceLabel", "DISTANCE", 14, Muted, new Vector2(-225, 46), new Vector2(360, 22), TextAnchor.MiddleCenter, FontStyle.Normal, _strong);
            Label(card.transform, "KJ_Airdrop_Distance", "-", 44, Text, new Vector2(-225, -14), new Vector2(380, 60), TextAnchor.MiddleCenter, FontStyle.Normal, _display);
            Label(card.transform, "KJ_Airdrop_TimerLabel", "OBJECTIVE TIMER", 14, Muted, new Vector2(225, 46), new Vector2(360, 22), TextAnchor.MiddleCenter, FontStyle.Normal, _strong);
            Label(card.transform, "KJ_Airdrop_Timer", "-", 44, Text, new Vector2(225, -14), new Vector2(380, 60), TextAnchor.MiddleCenter, FontStyle.Normal, _display);

            Label(screen, "KJ_Airdrop_Warning", "TPA, homes, kits and configured building actions are blocked inside the active objective radius.", 15, Warning, new Vector2(0, -204), new Vector2(ContentWidth, 28), TextAnchor.MiddleCenter, FontStyle.Normal, _body);
        }

        private static void BuildAdmin(Transform parent)
        {
            var screen = Screen(parent, "admin");
            Label(screen, "KJ_Admin_Name", "STAFF", 30, Text, new Vector2(0, 176), new Vector2(900, 42), TextAnchor.MiddleCenter, FontStyle.Normal, _display);
            Label(screen, "KJ_Admin_State", "God=False  Vanish=False", 16, TextDim, new Vector2(0, 128), new Vector2(900, 26), TextAnchor.MiddleCenter, FontStyle.Normal, _body);
            Button(screen, "KJ_Admin_God", "TOGGLE GOD", new Vector2(-158, 40), new Vector2(300, 56), Neutral, Text);
            Button(screen, "KJ_Admin_Vanish", "TOGGLE VANISH", new Vector2(158, 40), new Vector2(300, 56), Neutral, Text);
            Label(screen, "KJ_Admin_Help", "Targeted moderation stays command-based so every action lands in the audit log:\n/warn   /mute   /kick   /kjban   /kjunban   /inspect", 16, TextDim, new Vector2(0, -110), new Vector2(1000, 100), TextAnchor.MiddleCenter, FontStyle.Normal, _body);
        }

        private static void BuildToast(Transform parent)
        {
            var screen = Screen(parent, "toast");
            var toast = Panel(screen, "KJ_Toast", Card, new Vector2(0, -196), new Vector2(760, 112));
            var marker = Panel(toast.transform, "KJ_Toast_Marker", Accent, new Vector2(-377, 0), new Vector2(6, 112));
            marker.GetComponent<Image>().raycastTarget = false;
            Label(toast.transform, "KJ_Toast_Title", "KANOMJEEN", 14, Accent, new Vector2(0, 26), new Vector2(700, 24), TextAnchor.MiddleCenter, FontStyle.Normal, _strong);
            Label(toast.transform, "KJ_Toast_Body", "Notification", 17, Text, new Vector2(0, -14), new Vector2(700, 48), TextAnchor.MiddleCenter, FontStyle.Normal, _body);
        }

        // ---- Primitives ------------------------------------------------------------------------

        private static Transform Screen(Transform parent, string name)
        {
            var go = new GameObject("KJ_Screen_" + name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(ContentWidth, ContentHeight);
            rect.anchoredPosition = new Vector2(0, ContentCenterY);
            return go.transform;
        }

        private static void CardButton(Transform parent, string name, string title, string subtitle, float x, float y, float width, float height)
        {
            var card = Button(parent, name, string.Empty, new Vector2(x, y), new Vector2(width, height), Card, Text);
            var left = -(width / 2f) + 20f;
            Label(card.transform, name + "_Title", title, 24, Text, new Vector2(left, 26), new Vector2(width - 90f, 34), TextAnchor.MiddleLeft, FontStyle.Normal, _display);
            Label(card.transform, name + "_Subtitle", subtitle, 15, Muted, new Vector2(left, -26), new Vector2(width - 60f, 44), TextAnchor.MiddleLeft, FontStyle.Normal, _body);

            var marker = Panel(card.transform, name + "_Marker", Accent, new Vector2(-(width / 2f) + 3f, 0), new Vector2(6, height - 16f));
            marker.GetComponent<Image>().raycastTarget = false;
        }

        private static void StatCard(Transform parent, string label, string valueName, float x, float y)
        {
            var card = Panel(parent, valueName + "_Card", Card, new Vector2(x, y), new Vector2(340, 118));
            Label(card.transform, valueName + "_Label", label, 14, Muted, new Vector2(0, 34), new Vector2(300, 22), TextAnchor.MiddleCenter, FontStyle.Normal, _strong);
            Label(card.transform, valueName, "0", 44, Text, new Vector2(0, -16), new Vector2(320, 60), TextAnchor.MiddleCenter, FontStyle.Normal, _display);
        }

        private static void WideStat(Transform parent, string label, string valueName, float y)
        {
            var card = Panel(parent, valueName + "_Card", Card, new Vector2(0, y), new Vector2(1060, 48));
            Label(card.transform, valueName + "_Label", label, 14, Muted, new Vector2(-510, 0), new Vector2(300, 30), TextAnchor.MiddleLeft, FontStyle.Normal, _strong);
            Label(card.transform, valueName, "0m 0s", 20, Text, new Vector2(510, 0), new Vector2(400, 30), TextAnchor.MiddleRight, FontStyle.Normal, _strong);
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
            var image = go.GetComponent<Image>();
            image.color = color;
            image.sprite = _panelSprite;
            image.type = Image.Type.Sliced;
            return go;
        }

        private static GameObject Button(Transform parent, string name, string caption, Vector2 pos, Vector2 size, Color color, Color textColor)
        {
            var go = Panel(parent, name, color, pos, size);
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            var colors = button.colors;
            // Reference ColorBlock: the image carries the colour, the block only shades it.
            colors.normalColor = Rgba(0xFF, 0xFF, 0xFF, 0xFF);
            colors.highlightedColor = Rgba(0xF5, 0xF5, 0xF5, 0xFF);
            colors.pressedColor = Rgba(0xC8, 0xC8, 0xC8, 0xFF);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = Rgba(0xC8, 0xC8, 0xC8, 0x80);
            button.colors = colors;

            if (!string.IsNullOrEmpty(caption))
                Label(go.transform, name + "_Text", caption, 14, textColor, Vector2.zero, size - new Vector2(16, 10), TextAnchor.MiddleCenter, FontStyle.Normal, _display);
            return go;
        }

        private static Text Label(Transform parent, string name, string text, int size, Color color, Vector2 pos, Vector2 rectSize, TextAnchor anchor, FontStyle style, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);

            // `pos` is the aligned edge/point, so the pivot must follow the TextAnchor. A centre pivot
            // on a MiddleLeft label shifts the drawn text left by half the rect width (that bug shipped
            // once; ValidateTextLayout now enforces the pairing).
            rect.pivot = PivotFor(anchor);
            rect.anchoredPosition = pos;
            rect.sizeDelta = rectSize;

            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = font != null ? font : _body;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = anchor;
            label.color = color;
            label.supportRichText = true;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            return label;
        }

        // ---- Asset loading ---------------------------------------------------------------------

        private static void LoadFonts()
        {
            _body = LoadFont(BodyFontPath);
            _strong = LoadFont(StrongFontPath) ?? _body;
            _display = LoadFont(DisplayFontPath) ?? _strong;
            if (LoadFont(BodyFontPath) == null)
                Debug.LogWarning("[Kanomjeen] Kanit fonts not found under " + FontFolder + ". The prefab will build with the Unity fallback font and Thai/display typography must not ship until this is fixed.");
        }

        private static Font LoadFont(string path)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (font != null) return font;
            var builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return builtin != null ? builtin : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void LoadSprites()
        {
            // Unity's built-in nine-slice sprites, exactly what the reference Effects use for their
            // panels and buttons. They live in unity_builtin_extra (AssetDatabase) rather than the
            // default resources (Resources), and resolve on the client from the game's own build.
            _panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            _insetSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
            if (_panelSprite == null)
                Debug.LogWarning("[Kanomjeen] Built-in UISprite not found; panels will render as flat colour.");
            if (_insetSprite == null)
                Debug.LogWarning("[Kanomjeen] Built-in InputFieldBackground not found; recessed rows will render as flat colour.");
        }

        // ---- Validation ------------------------------------------------------------------------

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

            // Nine-slice panels must keep a sliced sprite, otherwise the shipped Effect loses its
            // rounded reference look without any visible error.
            var images = root.GetComponentsInChildren<Image>(true);
            for (var i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image.sprite == null || image.type == Image.Type.Sliced) continue;
                Debug.LogWarning("[Kanomjeen] Image " + GetHierarchyPath(image.transform) + " has a sprite but is not Sliced.");
            }
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

        private static Color Rgba(int r, int g, int b, int a)
        {
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }
    }
}
#endif
