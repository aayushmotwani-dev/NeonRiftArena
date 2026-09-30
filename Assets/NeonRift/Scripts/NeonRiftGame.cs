using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonRift
{
    public enum GamePhase { Intro, Playing, Paused, Victory, Defeat }

    [DisallowMultipleComponent]
    public sealed class NeonRiftGame : MonoBehaviour
    {
        public static NeonRiftGame I { get; private set; }
        public const float ArenaRadius = 18f;

        public GamePhase Phase { get; private set; } = GamePhase.Intro;
        public PlayerPilot Player { get; private set; }
        public ArenaDirector Director { get; private set; }
        public Camera GameCamera { get; private set; }
        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public float RunTime { get; private set; }

        readonly List<FloatingLabel> labels = new List<FloatingLabel>();
        Texture2D pixel;
        GUIStyle titleStyle;
        GUIStyle bodyStyle;
        GUIStyle smallStyle;
        GUIStyle scoreStyle;
        GUIStyle eyebrowStyle;
        Font interfaceFont;
        float styledScale = -1f;
        float messageUntil;
        string message = string.Empty;
        bool qaVisual;
        string qaDirectory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureGame()
        {
            if (FindAnyObjectByType<NeonRiftGame>() == null)
                new GameObject("Neon Rift Game").AddComponent<NeonRiftGame>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            Application.targetFrameRate = 120;
            QualitySettings.antiAliasing = 4;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.shadowDistance = 42f;
            Time.timeScale = 1f;
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            BuildWorld();
            ConfigureQualityAssurance();
        }

        void BuildWorld()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.17f, 0.23f, 0.3f);
            RenderSettings.ambientEquatorColor = new Color(0.085f, 0.12f, 0.15f);
            RenderSettings.ambientGroundColor = new Color(0.026f, 0.034f, 0.044f);
            RenderSettings.reflectionIntensity = 0.82f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0065f;
            RenderSettings.fogColor = new Color(0.012f, 0.018f, 0.028f);
            ConfigureSkybox();

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Mapped Industrial Deck";
            floor.transform.position = new Vector3(0f, -0.28f, 0f);
            floor.transform.localScale = new Vector3(36f, 0.45f, 28f);
            floor.GetComponent<Renderer>().material = NeonMaterials.Surface(
                "SpatialAssets/Textures/T_Trim_02_BaseColor_Blue",
                new Color(0.58f, 0.64f, 0.68f), new Vector2(6f, 5f), 0.58f, 0.42f);
            Destroy(floor.GetComponent<Collider>());

            CreateDeckDetails();
            CreateStructuralFrame();
            CreateGrid();

            var cameraObject = new GameObject("Arena Camera");
            GameCamera = cameraObject.AddComponent<Camera>();
            GameCamera.transform.position = new Vector3(0f, 25f, -18f);
            GameCamera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            GameCamera.fieldOfView = 46f;
            GameCamera.clearFlags = CameraClearFlags.Skybox;
            GameCamera.backgroundColor = new Color(0.008f, 0.012f, 0.02f);
            GameCamera.allowHDR = true;
            cameraObject.AddComponent<CameraJuice>();
            cameraObject.AddComponent<PremiumPresentation>();
            cameraObject.AddComponent<AudioListener>();
            NeonAudio.StartAmbience();

            var key = new GameObject("Key Light").AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(0.72f, 0.82f, 1f);
            key.intensity = 1.16f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.42f;
            key.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            var fill = new GameObject("Cool Fill Light").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.22f, 0.46f, 0.58f);
            fill.intensity = 0.34f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(38f, 142f, 0f);

            CreatePracticalLights();

            gameObject.AddComponent<SpatialLayoutManager>();
            gameObject.AddComponent<XRSpatialBridge>();
            Player = PlayerPilot.Create();
            Director = gameObject.AddComponent<ArenaDirector>();
        }

        static void ConfigureSkybox()
        {
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null) return;
            Material sky = new Material(shader) { name = "Spatial Lab Atmosphere" };
            sky.SetColor("_SkyTint", new Color(0.055f, 0.11f, 0.17f));
            sky.SetColor("_GroundColor", new Color(0.008f, 0.014f, 0.02f));
            sky.SetFloat("_AtmosphereThickness", 0.42f);
            sky.SetFloat("_Exposure", 0.48f);
            sky.SetFloat("_SunSize", 0.015f);
            RenderSettings.skybox = sky;
        }

        void CreateStructuralFrame()
        {
            Transform root = new GameObject("Lab Structural Frame").transform;
            Material frame = NeonMaterials.Surface("SpatialAssets/Textures/T_Trim_01_BaseColor",
                new Color(0.12f, 0.16f, 0.19f), new Vector2(2f, 1f), 0.8f, 0.44f);
            Material light = NeonMaterials.Make(new Color(0.015f, 0.11f, 0.12f), new Color(0.05f, 0.72f, 0.7f), 2.3f);
            Vector3[] corners =
            {
                new Vector3(-16.6f, 2.1f, -12.6f), new Vector3(16.6f, 2.1f, -12.6f),
                new Vector3(-16.6f, 2.1f, 12.6f), new Vector3(16.6f, 2.1f, 12.6f)
            };
            foreach (Vector3 corner in corners)
            {
                CreateDeckStrip(root, corner, new Vector3(0.42f, 4.2f, 0.42f), frame);
                CreateDeckStrip(root, corner + Vector3.up * 1.55f, new Vector3(0.18f, 0.85f, 0.18f), light);
            }
            CreateDeckStrip(root, new Vector3(0f, 3.95f, -12.6f), new Vector3(33.2f, 0.28f, 0.34f), frame);
            CreateDeckStrip(root, new Vector3(0f, 3.95f, 12.6f), new Vector3(33.2f, 0.28f, 0.34f), frame);
        }

        void CreateGrid()
        {
            var gridRoot = new GameObject("Neon Grid").transform;
            var material = NeonMaterials.Line(new Color(0.06f, 0.31f, 0.35f, 0.34f));
            for (int i = -8; i <= 8; i++)
            {
                float p = i * 2f;
                MakeLine(gridRoot, material, new Vector3(-17.5f, -0.04f, p), new Vector3(17.5f, -0.04f, p), i == 0 ? 0.055f : 0.018f);
                MakeLine(gridRoot, material, new Vector3(p, -0.04f, -13.5f), new Vector3(p, -0.04f, 13.5f), i == 0 ? 0.055f : 0.018f);
            }
        }

        void CreateDeckDetails()
        {
            var root = new GameObject("Authored Deck Details").transform;
            Material darkMetal = NeonMaterials.Surface("SpatialAssets/Textures/T_Trim_01_BaseColor",
                new Color(0.2f, 0.25f, 0.28f), new Vector2(2.5f, 1f), 0.72f, 0.5f);
            Material safety = NeonMaterials.Surface("SpatialAssets/Textures/T_Trim_01_BaseColor_Red",
                new Color(0.54f, 0.45f, 0.27f), new Vector2(4f, 1f), 0.5f, 0.36f);

            CreateDeckStrip(root, new Vector3(0f, -0.035f, -11.8f), new Vector3(32f, 0.035f, 0.7f), darkMetal);
            CreateDeckStrip(root, new Vector3(0f, -0.035f, 11.8f), new Vector3(32f, 0.035f, 0.7f), darkMetal);
            CreateDeckStrip(root, new Vector3(-15.8f, -0.025f, 0f), new Vector3(0.65f, 0.045f, 23f), safety);
            CreateDeckStrip(root, new Vector3(15.8f, -0.025f, 0f), new Vector3(0.65f, 0.045f, 23f), safety);

            for (int i = -3; i <= 3; i++)
            {
                if (i == 0) continue;
                float x = i * 4.1f;
                CreateDeckStrip(root, new Vector3(x, -0.02f, 0f), new Vector3(0.12f, 0.04f, 18f),
                    NeonMaterials.Make(new Color(0.02f, 0.08f, 0.09f), new Color(0.03f, 0.38f, 0.4f), 1.1f));
            }
        }

        static void CreateDeckStrip(Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "Deck Inlay";
            strip.transform.SetParent(parent, false);
            strip.transform.position = position;
            strip.transform.localScale = scale;
            strip.GetComponent<Renderer>().material = material;
            Destroy(strip.GetComponent<Collider>());
        }

        void CreatePracticalLights()
        {
            Vector3[] positions =
            {
                new Vector3(-12f, 3.2f, -8f), new Vector3(12f, 3.2f, -8f),
                new Vector3(-12f, 3.2f, 8f), new Vector3(12f, 3.2f, 8f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                Light light = new GameObject("Warm Service Light " + (i + 1)).AddComponent<Light>();
                light.transform.position = positions[i];
                light.type = LightType.Point;
                light.color = new Color(1f, 0.54f, 0.24f);
                light.range = 9f;
                light.intensity = 1.2f;
                light.shadows = LightShadows.None;
            }
        }

        void CreateBoundary()
        {
            var go = new GameObject("Rift Boundary");
            var lr = go.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.positionCount = 96;
            lr.widthMultiplier = 0.14f;
            lr.material = NeonMaterials.Line(new Color(0.1f, 0.75f, 1f));
            lr.numCornerVertices = 3;
            for (int i = 0; i < 96; i++)
            {
                float a = i / 96f * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * ArenaRadius, 0.11f, Mathf.Sin(a) * ArenaRadius));
            }
        }

        void CreatePylons()
        {
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                var pylon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pylon.name = "Boundary Pylon";
                pylon.transform.position = new Vector3(Mathf.Cos(a) * 18.3f, 0.8f, Mathf.Sin(a) * 18.3f);
                pylon.transform.localScale = new Vector3(0.22f, 1.6f, 0.22f);
                pylon.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 16f);
                pylon.GetComponent<Renderer>().material = NeonMaterials.Make(new Color(0.025f, 0.05f, 0.1f), new Color(0.05f, 0.55f, 1f), 2.4f);
                Destroy(pylon.GetComponent<Collider>());
            }
        }

        static void MakeLine(Transform parent, Material material, Vector3 a, Vector3 b, float width)
        {
            var line = new GameObject("Grid Line").AddComponent<LineRenderer>();
            line.transform.SetParent(parent);
            line.positionCount = 2;
            line.SetPositions(new[] { a, b });
            line.widthMultiplier = width;
            line.material = material;
        }

        void ConfigureQualityAssurance()
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (argument.Equals("-neonVisual", StringComparison.OrdinalIgnoreCase)) qaVisual = true;
                else if (argument.StartsWith("-neonQaDir=", StringComparison.OrdinalIgnoreCase))
                    qaDirectory = argument.Substring("-neonQaDir=".Length).Trim('"');
            }
            if (!qaVisual) return;
            if (string.IsNullOrWhiteSpace(qaDirectory)) qaDirectory = Path.Combine(Application.persistentDataPath, "NeonRiftQA");
            Directory.CreateDirectory(qaDirectory);
            StartCoroutine(QualityAssuranceRoutine());
        }

        IEnumerator QualityAssuranceRoutine()
        {
            while (SpatialLayoutManager.I == null || !SpatialLayoutManager.I.ScanComplete) yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(qaDirectory, "01-intro.png"));

            SpatialLayoutManager.I.ApplyLayout(2);
            while (!SpatialLayoutManager.I.ScanComplete) yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(qaDirectory, "02-layout.png"));

            StartRun();
            yield return new WaitForSecondsRealtime(3.2f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(qaDirectory, "03-gameplay.png"));

            int enemyCount = FindObjectsByType<EnemyAgent>(FindObjectsInactive.Exclude).Length;
            bool finite = IsFinite(Player.transform.position) && IsFinite(Player.Health) && IsFinite(RunTime);
            bool spatialValid = SpatialLayoutManager.I.WallSegments >= 20 && SpatialLayoutManager.I.Anchors >= 3 && SpatialLayoutManager.I.SafeArea > 400f;
            bool gameplayValid = Phase == GamePhase.Playing && enemyCount > 0 && Player.Health > 0f;
            bool passed = finite && spatialValid && gameplayValid;
            string report =
                "SPATIAL RIFT LAB AUTOMATED QA\n" +
                "result=" + (passed ? "PASS" : "FAIL") + "\n" +
                "layout=" + SpatialLayoutManager.I.LayoutName + "\n" +
                "wall_segments=" + SpatialLayoutManager.I.WallSegments + "\n" +
                "anchors=" + SpatialLayoutManager.I.Anchors + "\n" +
                "safe_area_m2=" + SpatialLayoutManager.I.SafeArea.ToString("F1") + "\n" +
                "enemy_count=" + enemyCount + "\n" +
                "player_health=" + Player.Health.ToString("F1") + "\n" +
                "finite_state=" + finite + "\n" +
                "spatial_valid=" + spatialValid + "\n" +
                "gameplay_valid=" + gameplayValid + "\n";
            File.WriteAllText(Path.Combine(qaDirectory, "qa-report.txt"), report);
            Debug.Log((passed ? "SPATIAL_RIFT_QA_OK" : "SPATIAL_RIFT_QA_FAILED") + "\n" + report);

            if (Phase == GamePhase.Playing) TogglePause();
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(qaDirectory, "04-pause.png"));
            yield return new WaitForSecondsRealtime(0.8f);
            Time.timeScale = 1f;
            Application.Quit(passed ? 0 : 2);
        }

        static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        void Update()
        {
            if (Phase == GamePhase.Intro && SpatialLayoutManager.I != null && SpatialLayoutManager.I.ScanComplete &&
                (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
                StartRun();

            if ((Phase == GamePhase.Victory || Phase == GamePhase.Defeat) && Input.GetKeyDown(KeyCode.R))
                Restart();

            if ((Phase == GamePhase.Playing || Phase == GamePhase.Paused) && Input.GetKeyDown(KeyCode.Escape))
                TogglePause();

            if (Phase == GamePhase.Playing) RunTime += Time.deltaTime;

            for (int i = labels.Count - 1; i >= 0; i--)
            {
                var item = labels[i];
                item.age += Time.unscaledDeltaTime;
                item.world += Vector3.up * Time.unscaledDeltaTime * 0.85f;
                if (item.age > 1f) labels.RemoveAt(i);
            }
        }

        public void StartRun()
        {
            Score = Combo = BestCombo = 0;
            RunTime = 0f;
            Phase = GamePhase.Playing;
            Player.ResetPilot();
            Director.Begin();
            ShowMessage("DEPLOYMENT COMPLETE  ·  WAVE 1", 2.2f);
        }

        public void AddKill(Vector3 position, int points)
        {
            Combo++;
            BestCombo = Mathf.Max(BestCombo, Combo);
            int earned = points * (1 + Mathf.Min(Combo / 4, 5));
            Score += earned;
            labels.Add(new FloatingLabel { world = position, text = "+" + earned, color = new Color(0.2f, 1f, 0.9f), age = 0f });
        }

        public void BreakCombo() => Combo = 0;

        public void ShowMessage(string text, float seconds)
        {
            message = text;
            messageUntil = Time.unscaledTime + seconds;
        }

        public void Win()
        {
            Phase = GamePhase.Victory;
            Player.SetControl(false);
            ShowMessage("RIFT SEALED", 99f);
            NeonFX.Burst(Player.transform.position, new Color(0.15f, 1f, 0.8f), 48, 8f);
        }

        public void Lose()
        {
            Phase = GamePhase.Defeat;
            ShowMessage("SIGNAL LOST", 99f);
        }

        void TogglePause()
        {
            bool pause = Phase == GamePhase.Playing;
            Phase = pause ? GamePhase.Paused : GamePhase.Playing;
            Time.timeScale = pause ? 0f : 1f;
        }

        void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnGUI()
        {
            float ui = Mathf.Clamp(Screen.height / 720f, 0.72f, 1.35f);
            EnsureStyles(ui);
            float w = Screen.width;
            float h = Screen.height;

            if (Phase == GamePhase.Intro)
            {
                DrawIntro(w, h, ui);
            }
            else
            {
                DrawHud(w, h, ui);
                if (Time.unscaledTime < messageUntil)
                {
                    Rect banner = new Rect(w * 0.5f - 245f * ui, 78f * ui, 490f * ui, 46f * ui);
                    Panel(banner, new Color(0.025f, 0.045f, 0.065f, 0.94f), new Color(0.16f, 0.82f, 0.78f));
                    Label(banner, message, scoreStyle);
                }

                foreach (var item in labels)
                {
                    Vector3 screen = GameCamera.WorldToScreenPoint(item.world);
                    if (screen.z < 0f) continue;
                    Color old = GUI.color;
                    GUI.color = new Color(item.color.r, item.color.g, item.color.b, 1f - item.age);
                    Label(new Rect(screen.x - 65f * ui, h - screen.y, 130f * ui, 28f * ui), item.text, scoreStyle);
                    GUI.color = old;
                }
            }

            if (Phase == GamePhase.Paused)
            {
                Fade(new Rect(0, 0, w, h), new Color(0.005f, 0.008f, 0.012f, 0.84f));
                Rect pausePanel = new Rect(w * 0.5f - 250f * ui, h * 0.5f - 105f * ui, 500f * ui, 210f * ui);
                Panel(pausePanel, new Color(0.025f, 0.04f, 0.055f, 0.97f), new Color(1f, 0.55f, 0.22f));
                Label(new Rect(pausePanel.x, pausePanel.y + 34f * ui, pausePanel.width, 70f * ui), "SYSTEM PAUSED", titleStyle);
                Label(new Rect(pausePanel.x, pausePanel.y + 119f * ui, pausePanel.width, 32f * ui), "PRESS ESC TO RETURN", bodyStyle);
            }
            else if (Phase == GamePhase.Victory || Phase == GamePhase.Defeat)
            {
                Fade(new Rect(0, 0, w, h), new Color(0.005f, 0.008f, 0.012f, 0.78f));
                Rect result = new Rect(w * 0.5f - 330f * ui, h * 0.5f - 155f * ui, 660f * ui, 310f * ui);
                Color resultAccent = Phase == GamePhase.Victory ? new Color(0.16f, 0.9f, 0.72f) : new Color(1f, 0.28f, 0.22f);
                Panel(result, new Color(0.025f, 0.04f, 0.055f, 0.97f), resultAccent);
                Label(new Rect(result.x, result.y + 35f * ui, result.width, 74f * ui), Phase == GamePhase.Victory ? "SECTOR SECURED" : "SIGNAL LOST", titleStyle);
                Label(new Rect(result.x, result.y + 130f * ui, result.width, 38f * ui), "FINAL SCORE  " + Score.ToString("N0"), scoreStyle);
                Label(new Rect(result.x, result.y + 172f * ui, result.width, 30f * ui), "BEST CHAIN  x" + BestCombo + "    •    RUN  " + FormatTime(RunTime), bodyStyle);
                Label(new Rect(result.x, result.y + 230f * ui, result.width, 32f * ui), "PRESS R TO RUN THE SIMULATION AGAIN", bodyStyle);
            }
        }

        void DrawIntro(float w, float h, float ui)
        {
            Fade(new Rect(0, 0, w, h), new Color(0.005f, 0.009f, 0.014f, 0.76f));
            float margin = Mathf.Max(14f, 24f * ui);
            float panelWidth = Mathf.Min(w - margin * 2f, 900f * ui);
            float panelHeight = Mathf.Min(h - margin * 2f, 560f * ui);
            Rect panel = new Rect((w - panelWidth) * 0.5f, (h - panelHeight) * 0.5f, panelWidth, panelHeight);
            Panel(panel, new Color(0.018f, 0.03f, 0.042f, 0.96f), new Color(0.16f, 0.82f, 0.78f));

            Label(new Rect(panel.x + 22f * ui, panel.y + 15f * ui, panel.width - 44f * ui, 24f * ui), "SPATIAL SYSTEMS PROTOTYPE  ·  BUILD 02", eyebrowStyle);
            Label(new Rect(panel.x + 18f * ui, panel.y + 45f * ui, panel.width - 36f * ui, 66f * ui), "SPATIAL RIFT", titleStyle);
            Label(new Rect(panel.x + 18f * ui, panel.y + 106f * ui, panel.width - 36f * ui, 28f * ui), "ROOM-SCALE COMBAT PROTOTYPE", scoreStyle);

            Rect process = new Rect(panel.x + 54f * ui, panel.y + 154f * ui, panel.width - 108f * ui, 34f * ui);
            Fade(process, new Color(0.04f, 0.075f, 0.085f, 0.9f));
            float processColumn = process.width / 3f;
            Label(new Rect(process.x, process.y, processColumn, process.height), "01  MAP ROOM", eyebrowStyle);
            Label(new Rect(process.x + processColumn, process.y, processColumn, process.height), "02  SOLVE CONSTRAINTS", eyebrowStyle);
            Label(new Rect(process.x + processColumn * 2f, process.y, processColumn, process.height), "03  PLACE CONTENT", eyebrowStyle);

            if (SpatialLayoutManager.I != null)
            {
                Label(new Rect(panel.x + 20f * ui, panel.y + 205f * ui, panel.width - 40f * ui, 36f * ui), SpatialLayoutManager.I.LayoutName, scoreStyle);
                Rect metricRow = new Rect(panel.x + 54f * ui, panel.y + 242f * ui, panel.width - 108f * ui, 27f * ui);
                float metricColumn = metricRow.width / 3f;
                Label(new Rect(metricRow.x, metricRow.y, metricColumn, metricRow.height), SpatialLayoutManager.I.WallSegments + "  SURFACES", bodyStyle);
                Label(new Rect(metricRow.x + metricColumn, metricRow.y, metricColumn, metricRow.height), SpatialLayoutManager.I.Anchors + "  ANCHORS", bodyStyle);
                Label(new Rect(metricRow.x + metricColumn * 2f, metricRow.y, metricColumn, metricRow.height), Mathf.RoundToInt(SpatialLayoutManager.I.SafeArea) + "  M2 PLAYABLE", bodyStyle);
                Bar(new Rect(panel.x + 120f * ui, panel.y + 279f * ui, panel.width - 240f * ui, 9f * ui),
                    SpatialLayoutManager.I.ScanProgress, new Color(0.16f, 0.82f, 0.78f));
            }

            Label(new Rect(panel.x + 20f * ui, panel.y + 317f * ui, panel.width - 40f * ui, 28f * ui), "CHOOSE A TEST ROOM", eyebrowStyle);
            Label(new Rect(panel.x + 20f * ui, panel.y + 346f * ui, panel.width - 40f * ui, 29f * ui), "[1]  COMPACT STUDIO       [2]  WIDE LAB       [3]  L-SHAPE LOFT", bodyStyle);
            Label(new Rect(panel.x + 20f * ui, panel.y + 397f * ui, panel.width - 40f * ui, 28f * ui), "WASD  MOVE     •     MOUSE  AIM     •     LEFT CLICK  FIRE     •     SPACE  DASH", bodyStyle);

            bool ready = SpatialLayoutManager.I != null && SpatialLayoutManager.I.ScanComplete;
            Rect action = new Rect(panel.x + 110f * ui, panel.y + 456f * ui, panel.width - 220f * ui, 54f * ui);
            Panel(action, ready ? new Color(0.05f, 0.16f, 0.16f, 0.96f) : new Color(0.08f, 0.09f, 0.1f, 0.96f),
                ready ? new Color(0.16f, 0.9f, 0.72f) : new Color(0.6f, 0.62f, 0.64f));
            Label(action, ready ? "PRESS ENTER TO DEPLOY" : "CALIBRATING ROOM…", scoreStyle);
        }

        void DrawHud(float w, float h, float ui)
        {
            float margin = Mathf.Max(12f, 18f * ui);
            Rect healthPanel = new Rect(margin, margin, 300f * ui, 92f * ui);
            Panel(healthPanel, new Color(0.018f, 0.03f, 0.045f, 0.94f), new Color(0.16f, 0.82f, 0.78f));
            Label(new Rect(healthPanel.x + 16f * ui, healthPanel.y + 9f * ui, healthPanel.width - 32f * ui, 24f * ui), "CORE INTEGRITY", smallStyle);
            Bar(new Rect(healthPanel.x + 16f * ui, healthPanel.y + 40f * ui, healthPanel.width - 32f * ui, 14f * ui),
                Player.Health / Player.MaxHealth, new Color(0.16f, 0.9f, 0.72f));
            Label(new Rect(healthPanel.x + 16f * ui, healthPanel.y + 61f * ui, healthPanel.width - 32f * ui, 22f * ui),
                Mathf.CeilToInt(Player.Health) + "  /  " + Mathf.CeilToInt(Player.MaxHealth), smallStyle);

            Rect scorePanel = new Rect(w - margin - 220f * ui, margin, 220f * ui, 92f * ui);
            Panel(scorePanel, new Color(0.018f, 0.03f, 0.045f, 0.94f), new Color(1f, 0.55f, 0.22f));
            Label(new Rect(scorePanel.x + 12f * ui, scorePanel.y + 7f * ui, scorePanel.width - 24f * ui, 39f * ui), Score.ToString("N0"), scoreStyle);
            Label(new Rect(scorePanel.x + 12f * ui, scorePanel.y + 51f * ui, scorePanel.width - 24f * ui, 25f * ui), "SCORE    •    CHAIN x" + Combo, smallStyle);

            Rect wavePanel = new Rect(w * 0.5f - 115f * ui, margin, 230f * ui, 56f * ui);
            Panel(wavePanel, new Color(0.018f, 0.03f, 0.045f, 0.94f), new Color(0.16f, 0.82f, 0.78f));
            Label(wavePanel, "WAVE  " + Mathf.Max(1, Director.Wave) + "  OF  " + ArenaDirector.FinalWave, scoreStyle);

            if (SpatialLayoutManager.I != null)
            {
                Rect mapPanel = new Rect(w - margin - 310f * ui, h - margin - 84f * ui, 310f * ui, 84f * ui);
                Panel(mapPanel, new Color(0.018f, 0.03f, 0.045f, 0.94f), new Color(1f, 0.55f, 0.22f));
                Label(new Rect(mapPanel.x + 15f * ui, mapPanel.y + 9f * ui, mapPanel.width - 30f * ui, 24f * ui), "ACTIVE ROOM  ·  " + SpatialLayoutManager.I.LayoutName, smallStyle);
                Label(new Rect(mapPanel.x + 15f * ui, mapPanel.y + 39f * ui, mapPanel.width - 30f * ui, 24f * ui),
                    SpatialLayoutManager.I.Anchors + " ANCHORS     •     " + Mathf.RoundToInt(SpatialLayoutManager.I.SafeArea) + " m² PLAYABLE", smallStyle);
            }

            float dash = 1f - Mathf.Clamp01(Player.DashCooldownRemaining / Player.DashCooldown);
            Rect dashPanel = new Rect(margin, h - margin - 60f * ui, 250f * ui, 60f * ui);
            Panel(dashPanel, new Color(0.018f, 0.03f, 0.045f, 0.94f), new Color(0.16f, 0.82f, 0.78f));
            Label(new Rect(dashPanel.x + 14f * ui, dashPanel.y + 15f * ui, 70f * ui, 26f * ui), "DASH", smallStyle);
            Bar(new Rect(dashPanel.x + 79f * ui, dashPanel.y + 21f * ui, dashPanel.width - 96f * ui, 11f * ui), dash, new Color(0.24f, 0.68f, 0.95f));

            EnemyAgent guardian = EnemyAgent.Active.Find(enemy => enemy != null && enemy.Kind == EnemyKind.Guardian);
            if (guardian != null)
            {
                Rect boss = new Rect(w * 0.5f - 230f * ui, margin + 68f * ui, 460f * ui, 48f * ui);
                Panel(boss, new Color(0.05f, 0.018f, 0.025f, 0.95f), new Color(1f, 0.25f, 0.19f));
                Label(new Rect(boss.x + 12f * ui, boss.y + 5f * ui, boss.width - 24f * ui, 18f * ui), "GUARDIAN CORE", eyebrowStyle);
                Bar(new Rect(boss.x + 18f * ui, boss.y + 28f * ui, boss.width - 36f * ui, 9f * ui), guardian.HealthNormalized, new Color(1f, 0.25f, 0.19f));
            }
        }

        void Bar(Rect rect, float value, Color color)
        {
            Fade(rect, new Color(0.045f, 0.06f, 0.07f, 1f));
            Rect fill = rect;
            fill.width *= Mathf.Clamp01(value);
            Fade(fill, color);
            Border(rect, new Color(0.45f, 0.55f, 0.58f, 0.5f), 1f);
        }

        void Panel(Rect rect, Color fill, Color accent)
        {
            Fade(rect, fill);
            Border(rect, new Color(0.34f, 0.44f, 0.48f, 0.72f), 1f);
            Fade(new Rect(rect.x, rect.y, Mathf.Max(3f, rect.width * 0.006f), rect.height), accent);
            Fade(new Rect(rect.x, rect.y, rect.width, 2f), accent * new Color(1f, 1f, 1f, 0.7f));
        }

        void Border(Rect rect, Color color, float thickness)
        {
            Fade(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Fade(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fade(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Fade(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        void Label(Rect rect, string text, GUIStyle style)
        {
            GUI.Label(rect, text, style);
        }

        void Fade(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = old;
        }

        static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        void EnsureStyles(float scale)
        {
            if (titleStyle != null && Mathf.Abs(styledScale - scale) < 0.01f) return;
            styledScale = scale;
            if (interfaceFont == null) interfaceFont = Resources.Load<Font>("Fonts/Oxanium");
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(37, Mathf.RoundToInt(54 * scale)), fontStyle = FontStyle.Bold, clipping = TextClipping.Clip };
            titleStyle.normal.textColor = new Color(0.15f, 0.95f, 1f);
            scoreStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(17, Mathf.RoundToInt(22 * scale)), fontStyle = FontStyle.Bold, clipping = TextClipping.Clip };
            scoreStyle.normal.textColor = new Color(0.86f, 0.94f, 0.95f);
            bodyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(13, Mathf.RoundToInt(16 * scale)), clipping = TextClipping.Clip };
            bodyStyle.normal.textColor = new Color(0.82f, 0.88f, 0.88f);
            smallStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = Mathf.Max(12, Mathf.RoundToInt(13 * scale)), fontStyle = FontStyle.Bold, clipping = TextClipping.Clip };
            smallStyle.normal.textColor = new Color(0.8f, 0.87f, 0.87f);
            eyebrowStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(12, Mathf.RoundToInt(12 * scale)), fontStyle = FontStyle.Bold, clipping = TextClipping.Clip };
            eyebrowStyle.normal.textColor = new Color(1f, 0.59f, 0.27f);

            if (interfaceFont != null)
            {
                titleStyle.font = interfaceFont;
                scoreStyle.font = interfaceFont;
                eyebrowStyle.font = interfaceFont;
            }
        }

        sealed class FloatingLabel
        {
            public Vector3 world;
            public string text;
            public Color color;
            public float age;
        }
    }

    public static class NeonMaterials
    {
        static readonly Dictionary<string, Material> surfaceCache = new Dictionary<string, Material>();

        public static Material Make(Color baseColor, Color emission, float intensity)
        {
            Shader shader = Resources.Load<Shader>("SpatialSurface");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader);
            if (material.HasProperty("_Tint")) material.SetColor("_Tint", baseColor);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", baseColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission * intensity);
            }
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.58f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.34f);
            return material;
        }

        public static Material Surface(string texturePath, Color tint, Vector2 tiling, float metallic, float smoothness)
        {
            string key = texturePath + tint + tiling + metallic + smoothness;
            if (surfaceCache.TryGetValue(key, out Material cached) && cached != null) return cached;

            Shader shader = Resources.Load<Shader>("SpatialSurface");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader);
            Texture2D texture = Resources.Load<Texture2D>(texturePath);
            if (texture != null)
            {
                material.mainTexture = texture;
                material.mainTextureScale = tiling;
                texture.wrapMode = TextureWrapMode.Repeat;
                texture.anisoLevel = 8;
            }
            if (material.HasProperty("_Tint")) material.SetColor("_Tint", tint);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            surfaceCache[key] = material;
            return material;
        }

        public static Material Line(Color color)
        {
            Shader shader = Resources.Load<Shader>("NeonRiftGlow");
            if (shader == null) shader = Shader.Find("Hidden/InternalErrorShader");
            var material = new Material(shader) { color = color };
            return material;
        }
    }
}
