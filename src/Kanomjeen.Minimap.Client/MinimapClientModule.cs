using System;
using System.Globalization;
using System.Reflection;
using SDG.Framework.Modules;
using UnityEngine;
using UnityEngine.UI;

namespace Kanomjeen.Minimap.Client
{
    public sealed class MinimapClientModule : IModuleNexus
    {
        public void initialize()
        {
            if (Application.isBatchMode) return;
            var host = new GameObject("Kanomjeen_Minimap_Client");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<MinimapTracker>();
        }

        public void shutdown() { }
    }

    internal sealed class MinimapTracker : MonoBehaviour
    {
        private const string SurfaceName = "KJ_Minimap_Surface";
        private const string MarkerName = "KJ_Client_PlayerMarker";
        private const float MapAspect = 5441f / 3905f;
        private static MethodInfo projectWorldPositionToMap;
        private RectTransform surface;
        private RectTransform marker;
        private Text markerText;
        private Text waypointTargetText;
        private RectTransform[] mapImages;
        private Vector3 waypointTarget;
        private bool hasWaypoint;
        private GUIStyle waypointStyle;
        private float nextSearch;
        private float nextLog;

        private void Update()
        {
            if (Time.unscaledTime >= nextSearch || surface == null)
            {
                nextSearch = Time.unscaledTime + 0.5f;
                FindSurface();
            }
            if (surface == null || marker == null)
            {
                LogHeartbeat("surface-or-marker-missing");
                return;
            }

            var playerType = Type.GetType("SDG.Unturned.Player, Assembly-CSharp");
            var playerProperty = playerType == null ? null : playerType.GetProperty("player");
            var player = playerProperty == null ? null : playerProperty.GetValue(null, null) as Component;
            if (player == null)
            {
                marker.gameObject.SetActive(false);
                LogHeartbeat("local-player-missing");
                return;
            }

            var position = player.transform.position;
            hasWaypoint = TryReadWaypointTarget(out waypointTarget);
            var width = surface.rect.width;
            var height = surface.rect.height;
            if (width <= 0f || height <= 0f) return;

            if (!TryProjectWorldPositionToMap(position, out var mapPosition))
            {
                LogHeartbeat("native-chart-projection-unavailable");
                return;
            }
            var nx = Mathf.Clamp01(mapPosition.x);
            // ProjectWorldPositionToMap returns chart coordinates with a top-left
            // origin, while the Unity RectTransform/image uses a bottom-left
            // origin for its normalized map position. Flip Y before scrolling the
            // map so the terrain under the fixed player marker matches the chart.
            var ny = Mathf.Clamp01(1f - mapPosition.y);
            // The player marker is locked to the center. Move the map image
            // underneath it, like a normal open-world minimap.
            marker.anchoredPosition = Vector2.zero;
            if (mapImages != null)
            {
                var selected = FindActiveMapImage();
                if (selected == null)
                {
                    selected = mapImages.Length > 1 ? mapImages[1] : mapImages[0];
                    if (selected != null) selected.gameObject.SetActive(true);
                }
                foreach (var mapImage in mapImages)
                {
                    if (mapImage == null) continue;
                    var mapWidth = Mathf.Max(width, mapImage.rect.width);
                    var mapHeight = mapWidth * MapAspect;
                    mapImage.sizeDelta = new Vector2(mapWidth, mapHeight);
                    mapImage.anchoredPosition = new Vector2(
                        -(nx - 0.5f) * mapWidth,
                        -(ny - 0.5f) * mapHeight);
                }
            }
            markerText.rectTransform.localEulerAngles = new Vector3(0f, 0f, -player.transform.eulerAngles.y);
            marker.gameObject.SetActive(true);
            HideLegacyMarkers();
            LogHeartbeat("active world=" + position.x.ToString("0.0") + "," + position.y.ToString("0.0") + "," + position.z.ToString("0.0") + " map=" + nx.ToString("0.000") + "," + ny.ToString("0.000"));
        }

        private void OnGUI()
        {
            if (!hasWaypoint) return;
            var camera = Camera.main;
            if (camera == null) return;
            var playerType = Type.GetType("SDG.Unturned.Player, Assembly-CSharp");
            var playerProperty = playerType == null ? null : playerType.GetProperty("player");
            var player = playerProperty == null ? null : playerProperty.GetValue(null, null) as Component;
            if (player == null) return;

            var projected = camera.WorldToScreenPoint(waypointTarget);
            var distance = Vector2.Distance(
                new Vector2(player.transform.position.x, player.transform.position.z),
                new Vector2(waypointTarget.x, waypointTarget.z));
            var onScreen = projected.z > 0f && projected.x >= 0f && projected.x <= Screen.width && projected.y >= 0f && projected.y <= Screen.height;
            if (projected.z <= 0f)
            {
                projected.x = Screen.width - projected.x;
                projected.y = Screen.height - projected.y;
            }
            projected.x = Mathf.Clamp(projected.x, 36f, Screen.width - 36f);
            projected.y = Mathf.Clamp(Screen.height - projected.y, 36f, Screen.height - 36f);

            if (waypointStyle == null)
            {
                waypointStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16,
                    fontStyle = FontStyle.Bold
                };
                waypointStyle.normal.textColor = Color.white;
            }
            var label = (onScreen ? "◆" : "▲") + "\n" + Mathf.RoundToInt(distance) + "m";
            GUI.Label(new Rect(projected.x - 48f, projected.y - 28f, 96f, 56f), label, waypointStyle);
        }

        private void LogHeartbeat(string state)
        {
            if (Time.unscaledTime < nextLog) return;
            nextLog = Time.unscaledTime + 5f;
            Debug.Log("[Kanomjeen.Minimap] " + state);
        }

        private static bool TryProjectWorldPositionToMap(Vector3 worldPosition, out Vector2 mapPosition)
        {
            mapPosition = default;
            try
            {
                if (projectWorldPositionToMap == null)
                {
                    var dashboardType = Type.GetType("SDG.Unturned.PlayerDashboardInformationUI, Assembly-CSharp");
                    projectWorldPositionToMap = dashboardType?.GetMethod(
                        "ProjectWorldPositionToMap",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                        null,
                        new[] { typeof(Vector3) },
                        null);
                }

                if (projectWorldPositionToMap == null) return false;
                var result = projectWorldPositionToMap.Invoke(null, new object[] { worldPosition });
                if (!(result is Vector2 projected)) return false;
                mapPosition = projected;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void FindSurface()
        {
            var all = Resources.FindObjectsOfTypeAll<RectTransform>();
            foreach (var candidate in all)
            {
                if (candidate == null || candidate.name != SurfaceName || !candidate.gameObject.scene.IsValid()) continue;
                surface = candidate;
                surface.gameObject.SetActive(true);
                if (surface.parent != null) surface.parent.gameObject.SetActive(true);
                marker = surface.Find(MarkerName) as RectTransform;
                if (marker == null) marker = CreateMarker(surface);
                markerText = marker.GetComponent<Text>();
                waypointTargetText = surface.Find("KJ_Map_Waypoint_Target")?.GetComponent<Text>();
                mapImages = new[]
                {
                    surface.Find("KJ_Minimap_Image_Zoom_40") as RectTransform,
                    surface.Find("KJ_Minimap_Image_Zoom_50") as RectTransform,
                    surface.Find("KJ_Minimap_Image_Zoom_60") as RectTransform,
                    surface.Find("KJ_Minimap_Image_Zoom_25") as RectTransform,
                    surface.Find("KJ_Minimap_Image_Zoom_30") as RectTransform,
                    surface.Find("KJ_Minimap_Image_Zoom_35") as RectTransform
                };
                return;
            }
            surface = null;
            marker = null;
            waypointTargetText = null;
            mapImages = null;
        }

        private bool TryReadWaypointTarget(out Vector3 target)
        {
            target = default;
            var raw = waypointTargetText == null ? null : waypointTargetText.text;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            var parts = raw.Split('|');
            if (parts.Length != 3) return false;
            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)) return false;
            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)) return false;
            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z)) return false;
            target = new Vector3(x, y, z);
            return true;
        }

        private RectTransform FindActiveMapImage()
        {
            if (mapImages == null) return null;
            for (var i = 0; i < mapImages.Length; i++)
                if (mapImages[i] != null && mapImages[i].gameObject.activeSelf) return mapImages[i];
            return null;
        }

        private void HideLegacyMarkers()
        {
            var children = surface.GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                if (child == null || child == marker.transform) continue;
                if (child.name.StartsWith("KJ_Map_PlayerMarker", StringComparison.Ordinal))
                    child.gameObject.SetActive(false);
            }
        }

        private static RectTransform CreateMarker(RectTransform parent)
        {
            var go = new GameObject(MarkerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(48f, 48f);
            var text = go.GetComponent<Text>();
            text.text = "▲";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return rect;
        }
    }
}
