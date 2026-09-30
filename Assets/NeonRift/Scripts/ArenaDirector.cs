using System.Collections;
using UnityEngine;

namespace NeonRift
{
    public sealed class ArenaDirector : MonoBehaviour
    {
        public const int FinalWave = 4;
        public int Wave { get; private set; }
        int alive;
        bool running;

        public void Begin()
        {
            if (running) return;
            running = true;
            StartCoroutine(RunWaves());
        }

        IEnumerator RunWaves()
        {
            yield return new WaitForSeconds(1f);
            for (Wave = 1; Wave <= FinalWave; Wave++)
            {
                NeonRiftGame.I.ShowMessage(Wave == FinalWave ? "GUARDIAN SIGNAL DETECTED" : "WAVE " + Wave, 2f);
                int count = Wave == FinalWave ? 7 : 4 + Wave * 2;
                alive = count;
                for (int i = 0; i < count; i++)
                {
                    EnemyKind kind;
                    if (Wave == FinalWave && i == count - 1) kind = EnemyKind.Guardian;
                    else if (Wave >= 2 && i % 4 == 0) kind = EnemyKind.Turret;
                    else if (Wave >= 2 && i % 3 == 0) kind = EnemyKind.Striker;
                    else kind = EnemyKind.Drone;
                    yield return Spawn(kind, i, count);
                    yield return new WaitForSeconds(Wave == FinalWave ? 0.16f : 0.28f);
                }

                while (alive > 0 && NeonRiftGame.I.Phase == GamePhase.Playing) yield return null;
                if (NeonRiftGame.I.Phase != GamePhase.Playing) yield break;
                if (Wave < FinalWave)
                {
                    NeonRiftGame.I.ShowMessage("SECTOR CLEAR  ·  RECONFIGURING", 2.4f);
                    yield return new WaitForSeconds(3.2f);
                }
            }

            yield return new WaitForSeconds(0.8f);
            NeonRiftGame.I.Win();
        }

        IEnumerator Spawn(EnemyKind kind, int index, int count)
        {
            float angle = index / (float)count * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
            Vector3 position = SpatialLayoutManager.I != null
                ? SpatialLayoutManager.I.GetSpawnPoint(index, count)
                : new Vector3(Mathf.Cos(angle) * (NeonRiftGame.ArenaRadius - 1.4f), 0.55f, Mathf.Sin(angle) * (NeonRiftGame.ArenaRadius - 1.4f));

            Color warning = kind == EnemyKind.Guardian ? new Color(1f, 0.18f, 0.12f) : new Color(1f, 0.48f, 0.12f);
            GameObject marker = new GameObject(kind + " Arrival Telegraph");
            marker.transform.position = new Vector3(position.x, 0.08f, position.z);
            LineRenderer ring = marker.AddComponent<LineRenderer>();
            ring.loop = true;
            ring.positionCount = 40;
            ring.widthMultiplier = kind == EnemyKind.Guardian ? 0.15f : 0.08f;
            ring.material = NeonMaterials.Line(warning);
            float radius = kind == EnemyKind.Guardian ? 2.1f : 1.05f;
            for (int p = 0; p < ring.positionCount; p++)
            {
                float a = p / (float)ring.positionCount * Mathf.PI * 2f;
                ring.SetPosition(p, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }

            Light pulse = marker.AddComponent<Light>();
            pulse.type = LightType.Point;
            pulse.color = warning;
            pulse.range = kind == EnemyKind.Guardian ? 8f : 4.5f;
            pulse.shadows = LightShadows.None;

            float telegraph = kind == EnemyKind.Guardian ? 0.78f : 0.44f;
            for (float elapsed = 0f; elapsed < telegraph; elapsed += Time.deltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / telegraph);
                marker.transform.localScale = Vector3.one * Mathf.SmoothStep(0.18f, 1f, progress);
                pulse.intensity = 0.6f + Mathf.PingPong(elapsed * 8f, 1f) * 1.8f;
                yield return null;
            }

            NeonFX.Burst(position, warning, kind == EnemyKind.Guardian ? 34 : 16, kind == EnemyKind.Guardian ? 6f : 3.5f);
            EnemyAgent.Create(kind, position);
            Destroy(marker);
        }

        public void NotifyEnemyDown(EnemyAgent enemy) => alive = Mathf.Max(0, alive - 1);
    }
}
