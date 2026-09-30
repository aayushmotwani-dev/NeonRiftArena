using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace NeonRift
{
    /// <summary>
    /// Desktop simulation of the scan -> understand -> place pipeline used by
    /// room-adaptive MR systems. Layout data is deliberately separated from the
    /// rendering so a real scene-mesh provider can replace the simulated scan.
    /// </summary>
    public sealed class SpatialLayoutManager : MonoBehaviour
    {
        public static SpatialLayoutManager I { get; private set; }

        public string LayoutName { get; private set; }
        public int LayoutIndex { get; private set; }
        public int WallSegments { get; private set; }
        public int Anchors { get; private set; }
        public float SafeArea { get; private set; }
        public bool ScanComplete { get; private set; }

        readonly List<Rect> blockedZones = new List<Rect>();
        readonly List<Vector3> anchorPoints = new List<Vector3>();
        Transform generatedRoot;
        Coroutine scanRoutine;
        float halfWidth;
        float halfDepth;
        float scanProgress;

        static readonly Color Cyan = new Color(0.08f, 0.85f, 1f);
        static readonly Color Amber = new Color(1f, 0.52f, 0.08f);

        void Awake()
        {
            I = this;
            ApplyLayout(0);
        }

        void Update()
        {
            if (NeonRiftGame.I == null || NeonRiftGame.I.Phase != GamePhase.Intro) return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) ApplyLayout(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) ApplyLayout(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) ApplyLayout(2);
            if (Input.GetKeyDown(KeyCode.Tab)) ApplyLayout((LayoutIndex + 1) % 3);
        }

        public void ApplyLayout(int index)
        {
            if (scanRoutine != null)
            {
                StopCoroutine(scanRoutine);
                scanRoutine = null;
            }

            LayoutIndex = Mathf.Clamp(index, 0, 2);
            if (generatedRoot != null) Destroy(generatedRoot.gameObject);
            generatedRoot = new GameObject("Generated Spatial Layout").transform;
            generatedRoot.SetParent(transform, false);
            blockedZones.Clear();
            anchorPoints.Clear();
            WallSegments = 0;
            Anchors = 0;
            ScanComplete = false;
            scanProgress = 0f;

            switch (LayoutIndex)
            {
                case 0:
                    LayoutName = "COMPACT STUDIO";
                    halfWidth = 13.5f;
                    halfDepth = 10.5f;
                    AddObstacle(new Rect(-7.2f, -2.2f, 4.2f, 4.4f), "SpatialAssets/Props/Prop_Computer", new Vector3(2.8f, 1.8f, 1.5f));
                    AddObstacle(new Rect(3.5f, 3.2f, 4.6f, 3.2f), "SpatialAssets/Props/Prop_Crate3", new Vector3(2.4f, 1.6f, 2.1f));
                    AddObstacle(new Rect(2.2f, -6.8f, 2.8f, 2.8f), "SpatialAssets/Props/Prop_Barrel_Large", new Vector3(1.6f, 1.9f, 1.6f));
                    break;
                case 1:
                    LayoutName = "WIDE LAB";
                    halfWidth = 16.4f;
                    halfDepth = 9.2f;
                    AddObstacle(new Rect(-10.5f, 2.5f, 5.3f, 2.8f), "SpatialAssets/Props/Prop_ItemHolder", new Vector3(3.6f, 1.6f, 1.7f));
                    AddObstacle(new Rect(-1.8f, -5.2f, 3.6f, 3.6f), "SpatialAssets/Columns/Column_Pipes", new Vector3(2.1f, 2.8f, 2.1f));
                    AddObstacle(new Rect(7.3f, -1.8f, 5.1f, 3.6f), "SpatialAssets/Props/Prop_AccessPoint", new Vector3(3.2f, 1.7f, 2.2f));
                    break;
                default:
                    LayoutName = "L-SHAPE LOFT";
                    halfWidth = 15.3f;
                    halfDepth = 12.1f;
                    // The upper-right area represents physical space that the scan marks unavailable.
                    AddObstacle(new Rect(5.2f, 3.0f, 9.1f, 8.0f), "SpatialAssets/Walls/WallBand_Straight", new Vector3(8.2f, 2.4f, 4.8f), true);
                    AddObstacle(new Rect(-9.5f, -2.1f, 4.0f, 4.0f), "SpatialAssets/Props/Prop_Crate3", new Vector3(2.6f, 1.7f, 2.4f));
                    AddObstacle(new Rect(0.5f, -8.0f, 4.0f, 2.7f), "SpatialAssets/Props/Prop_Computer", new Vector3(3.0f, 1.8f, 1.4f));
                    break;
            }

            BuildRoomShell();
            BuildSurfaceStations();
            SafeArea = CalculateSafeArea();
            scanRoutine = StartCoroutine(ScanAnimation());
        }

        System.Collections.IEnumerator ScanAnimation()
        {
            var renderers = generatedRoot.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers) renderer.enabled = false;
            int revealCount = Mathf.Max(1, renderers.Length / 14);
            for (int i = 0; i < renderers.Length; i += revealCount)
            {
                for (int j = i; j < Mathf.Min(i + revealCount, renderers.Length); j++) renderers[j].enabled = true;
                scanProgress = (i + revealCount) / (float)renderers.Length;
                yield return new WaitForSecondsRealtime(0.035f);
            }
            foreach (var renderer in renderers) renderer.enabled = true;
            scanProgress = 1f;
            ScanComplete = true;
            scanRoutine = null;
            if (NeonRiftGame.I != null) NeonRiftGame.I.ShowMessage("SPATIAL MAP READY // " + Anchors + " ANCHORS", 1.5f);
        }

        void BuildRoomShell()
        {
            const float segment = 3.15f;
            for (float x = -halfWidth + segment * 0.5f; x < halfWidth; x += segment)
            {
                PlaceWall(new Vector3(x, 1.15f, halfDepth), Quaternion.identity, segment);
                PlaceWall(new Vector3(x, 1.15f, -halfDepth), Quaternion.identity, segment);
            }
            for (float z = -halfDepth + segment * 0.5f; z < halfDepth; z += segment)
            {
                PlaceWall(new Vector3(halfWidth, 1.15f, z), Quaternion.Euler(0f, 90f, 0f), segment);
                PlaceWall(new Vector3(-halfWidth, 1.15f, z), Quaternion.Euler(0f, 90f, 0f), segment);
            }

            var boundary = new GameObject("Guardian Boundary").AddComponent<LineRenderer>();
            boundary.transform.SetParent(generatedRoot, false);
            boundary.loop = true;
            boundary.positionCount = 4;
            boundary.widthMultiplier = 0.1f;
            boundary.material = NeonMaterials.Line(new Color(Cyan.r, Cyan.g, Cyan.b, 0.8f));
            boundary.SetPositions(new[]
            {
                new Vector3(-halfWidth, 0.08f, -halfDepth), new Vector3(-halfWidth, 0.08f, halfDepth),
                new Vector3(halfWidth, 0.08f, halfDepth), new Vector3(halfWidth, 0.08f, -halfDepth)
            });
        }

        void PlaceWall(Vector3 position, Quaternion rotation, float length)
        {
            string asset = WallSegments % 6 == 0 ? "SpatialAssets/Walls/WallAstra_Straight_Window" :
                           WallSegments % 6 == 3 ? "SpatialAssets/Walls/ShortWall_AccentStrip_Straight" :
                           "SpatialAssets/Walls/WallAstra_Straight";
            CreateVisual(asset, generatedRoot, position, rotation, new Vector3(length, 2.4f, 0.32f), PrimitiveType.Cube, new Color(0.07f, 0.14f, 0.24f));
            WallSegments++;
        }

        void BuildSurfaceStations()
        {
            var center = CreateVisual("SpatialAssets/Platforms/Platform_Round1", generatedRoot, Vector3.zero, Quaternion.identity,
                new Vector3(5.2f, 0.18f, 5.2f), PrimitiveType.Cylinder, new Color(0.04f, 0.18f, 0.26f));
            center.name = "Mapped Play Origin";

            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                Vector3 p = new Vector3(Mathf.Cos(a) * Mathf.Min(halfWidth - 3f, 9f), 0.05f, Mathf.Sin(a) * Mathf.Min(halfDepth - 2.5f, 7f));
                if (InsideBlocked(p, 1f)) continue;
                CreateVisual("SpatialAssets/Props/Prop_Light_Floor", generatedRoot, p, Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f),
                    new Vector3(1.1f, 0.45f, 1.1f), PrimitiveType.Cylinder, Cyan * 0.65f);
            }
        }

        void AddObstacle(Rect zone, string resource, Vector3 size, bool unavailableSpace = false)
        {
            blockedZones.Add(zone);
            Vector3 center = new Vector3(zone.center.x, size.y * 0.5f, zone.center.y);
            CreateVisual(resource, generatedRoot, center, Quaternion.Euler(0f, unavailableSpace ? 90f : Random.Range(-12f, 12f), 0f), size,
                unavailableSpace ? PrimitiveType.Cube : PrimitiveType.Capsule,
                unavailableSpace ? new Color(0.18f, 0.03f, 0.06f) : new Color(0.08f, 0.16f, 0.22f));
            AddAnchor(new Vector3(zone.center.x, 0.12f, zone.center.y), unavailableSpace ? Amber : Cyan, unavailableSpace ? "NO-GO VOLUME" : "PASSIVE HAPTIC ANCHOR");
        }

        void AddAnchor(Vector3 position, Color color, string label)
        {
            anchorPoints.Add(position);
            Anchors++;
            var marker = new GameObject(label).AddComponent<LineRenderer>();
            marker.transform.SetParent(generatedRoot, false);
            marker.loop = true;
            marker.positionCount = 32;
            marker.widthMultiplier = 0.055f;
            marker.material = NeonMaterials.Line(color);
            for (int i = 0; i < 32; i++)
            {
                float a = i / 32f * Mathf.PI * 2f;
                marker.SetPosition(i, position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.05f);
            }
        }

        public Vector3 GetSpawnPoint(int index, int count)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                float a = (index + attempt * 0.31f) / Mathf.Max(1f, count) * Mathf.PI * 2f;
                Vector3 point = new Vector3(Mathf.Cos(a) * (halfWidth - 1.1f), 0.55f, Mathf.Sin(a) * (halfDepth - 1.1f));
                if (!InsideBlocked(point, 1.4f)) return point;
            }
            return new Vector3(-halfWidth + 2f, 0.55f, -halfDepth + 2f);
        }

        public Vector3 Constrain(Vector3 position, float radius)
        {
            position.x = Mathf.Clamp(position.x, -halfWidth + radius, halfWidth - radius);
            position.z = Mathf.Clamp(position.z, -halfDepth + radius, halfDepth - radius);
            for (int i = 0; i < blockedZones.Count; i++)
            {
                Rect expanded = blockedZones[i];
                expanded.xMin -= radius; expanded.xMax += radius;
                expanded.yMin -= radius; expanded.yMax += radius;
                Vector2 p = new Vector2(position.x, position.z);
                if (!expanded.Contains(p)) continue;
                float left = Mathf.Abs(p.x - expanded.xMin);
                float right = Mathf.Abs(expanded.xMax - p.x);
                float bottom = Mathf.Abs(p.y - expanded.yMin);
                float top = Mathf.Abs(expanded.yMax - p.y);
                float nearest = Mathf.Min(left, right, bottom, top);
                if (nearest == left) position.x = expanded.xMin;
                else if (nearest == right) position.x = expanded.xMax;
                else if (nearest == bottom) position.z = expanded.yMin;
                else position.z = expanded.yMax;
            }
            return position;
        }

        bool InsideBlocked(Vector3 point, float margin)
        {
            foreach (Rect source in blockedZones)
            {
                Rect zone = source;
                zone.xMin -= margin; zone.xMax += margin;
                zone.yMin -= margin; zone.yMax += margin;
                if (zone.Contains(new Vector2(point.x, point.z))) return true;
            }
            return false;
        }

        float CalculateSafeArea()
        {
            float area = halfWidth * 2f * halfDepth * 2f;
            foreach (Rect zone in blockedZones) area -= zone.width * zone.height;
            return Mathf.Max(0f, area);
        }

        public float ScanProgress => scanProgress;

        public static GameObject CreateVisual(string resource, Transform parent, Vector3 position, Quaternion rotation,
            Vector3 desiredSize, PrimitiveType fallback, Color fallbackColor)
        {
            GameObject prefab = Resources.Load<GameObject>(resource);
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, position, rotation, parent);
                Normalize(go, desiredSize);
            }
            else
            {
                go = GameObject.CreatePrimitive(fallback);
                go.transform.SetParent(parent, true);
                go.transform.SetPositionAndRotation(position, rotation);
                go.transform.localScale = desiredSize;
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null) renderer.material = NeonMaterials.Make(fallbackColor, fallbackColor * 1.4f, 1.5f);
                var collider = go.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
            }
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            return go;
        }

        public static bool AttachEnemyModel(Transform parent, string resource, float desiredHeight, Vector3 offset)
        {
            GameObject prefab = Resources.Load<GameObject>(resource);
            if (prefab == null) return false;
            GameObject go = Instantiate(prefab, parent);
            go.name = "CC0 Enemy Visual";
            go.transform.localPosition = offset;
            go.transform.localRotation = Quaternion.identity;
            Bounds bounds = CombinedBounds(go);
            if (bounds.size.y > 0.001f)
            {
                float parentScale = Mathf.Max(0.001f, parent.lossyScale.y);
                go.transform.localScale *= desiredHeight / bounds.size.y / parentScale;
            }
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            return true;
        }

        static void Normalize(GameObject go, Vector3 desired)
        {
            Bounds bounds = CombinedBounds(go);
            Vector3 size = bounds.size;
            if (size.x < 0.001f || size.y < 0.001f || size.z < 0.001f) return;
            Vector3 scale = go.transform.localScale;
            scale.x *= desired.x / size.x;
            scale.y *= desired.y / size.y;
            scale.z *= desired.z / size.z;
            go.transform.localScale = scale;
        }

        static Bounds CombinedBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
    }

    /// <summary>
    /// Hardware-agnostic XR device bridge. The desktop portfolio build runs
    /// without an XR loader; when a loader is present this exposes HMD and
    /// controller tracking data to gameplay systems.
    /// </summary>
    public sealed class XRSpatialBridge : MonoBehaviour
    {
        public static XRSpatialBridge I { get; private set; }
        public bool HeadsetDetected { get; private set; }
        public string HeadsetName { get; private set; } = "DESKTOP SIMULATOR";
        public Vector3 HeadLocalPosition { get; private set; }
        public Quaternion HeadLocalRotation { get; private set; } = Quaternion.identity;

        readonly List<InputDevice> devices = new List<InputDevice>();
        InputDevice head;

        void Awake()
        {
            I = this;
            RefreshDevices();
            InputDevices.deviceConnected += OnDeviceChanged;
            InputDevices.deviceDisconnected += OnDeviceChanged;
        }

        void OnDestroy()
        {
            InputDevices.deviceConnected -= OnDeviceChanged;
            InputDevices.deviceDisconnected -= OnDeviceChanged;
        }

        void OnDeviceChanged(InputDevice _) => RefreshDevices();

        void RefreshDevices()
        {
            devices.Clear();
            InputDevices.GetDevices(devices);
            HeadsetDetected = false;
            foreach (InputDevice device in devices)
            {
                if ((device.characteristics & InputDeviceCharacteristics.HeadMounted) == 0) continue;
                head = device;
                HeadsetDetected = device.isValid;
                HeadsetName = string.IsNullOrWhiteSpace(device.name) ? "OPENXR HEADSET" : device.name.ToUpperInvariant();
                break;
            }
            if (!HeadsetDetected) HeadsetName = "DESKTOP SPATIAL SIMULATOR";
        }

        void Update()
        {
            if (!HeadsetDetected || !head.isValid) return;
            if (head.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position)) HeadLocalPosition = position;
            if (head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation)) HeadLocalRotation = rotation;
        }
    }
}
