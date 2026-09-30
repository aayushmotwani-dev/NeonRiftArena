using UnityEngine;

namespace NeonRift
{
    public enum NeonSound { Shot, Dash, Hurt, Explosion, Pickup, Boss }

    public static class NeonFX
    {
        public static void Burst(Vector3 position, Color color, int count, float speed)
        {
            var go = new GameObject("Neon Burst");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.45f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.58f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.17f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.18f;
            var trails = ps.trails;
            trails.enabled = true;
            trails.lifetime = 0.12f;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = NeonMaterials.Line(color);
            renderer.trailMaterial = NeonMaterials.Line(color);
            ps.Play();
        }
    }

    public sealed class CameraJuice : MonoBehaviour
    {
        static CameraJuice instance;
        Vector3 basePosition;
        float shakeTime;
        float magnitude;

        void Awake()
        {
            instance = this;
            basePosition = transform.localPosition;
        }

        public static void Shake(float seconds, float amount)
        {
            if (instance == null) return;
            instance.shakeTime = Mathf.Max(instance.shakeTime, seconds);
            instance.magnitude = Mathf.Max(instance.magnitude, amount);
        }

        void LateUpdate()
        {
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                transform.localPosition = basePosition + Random.insideUnitSphere * magnitude;
                magnitude = Mathf.Lerp(magnitude, 0f, 8f * Time.unscaledDeltaTime);
            }
            else transform.localPosition = basePosition;
        }
    }

    public static class NeonAudio
    {
        static AudioSource source;
        static AudioSource ambience;
        static readonly AudioClip[] clips = new AudioClip[6];

        public static void StartAmbience()
        {
            Ensure();
            if (ambience != null) return;
            GameObject go = new GameObject("Spatial Lab Ambience");
            ambience = go.AddComponent<AudioSource>();
            ambience.clip = MakeAmbience();
            ambience.loop = true;
            ambience.volume = 0.075f;
            ambience.spatialBlend = 0f;
            ambience.Play();
        }

        public static void Play(NeonSound sound, Vector3 position)
        {
            Ensure();
            source.transform.position = position;
            source.pitch = Random.Range(0.94f, 1.06f);
            source.PlayOneShot(clips[(int)sound], sound == NeonSound.Shot ? 0.22f : 0.45f);
        }

        static void Ensure()
        {
            if (source != null) return;
            var go = new GameObject("Procedural Audio");
            source = go.AddComponent<AudioSource>();
            source.spatialBlend = 0.15f;
            source.playOnAwake = false;
            clips[(int)NeonSound.Shot] = Make("Pulse", 0.09f, 520f, 180f, 0.35f);
            clips[(int)NeonSound.Dash] = Make("Dash", 0.22f, 180f, 720f, 0.28f);
            clips[(int)NeonSound.Hurt] = Make("Impact", 0.2f, 130f, 55f, 0.4f);
            clips[(int)NeonSound.Explosion] = Make("Burst", 0.28f, 90f, 35f, 0.5f);
            clips[(int)NeonSound.Pickup] = Make("Repair", 0.26f, 440f, 880f, 0.28f);
            clips[(int)NeonSound.Boss] = Make("Guardian", 0.42f, 70f, 45f, 0.55f);
        }

        static AudioClip Make(string name, float duration, float startFrequency, float endFrequency, float volume)
        {
            const int sampleRate = 44100;
            int count = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[count];
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
                phase += frequency / sampleRate * Mathf.PI * 2f;
                float envelope = Mathf.Pow(1f - t, 2.2f);
                float noise = (Random.value * 2f - 1f) * 0.16f;
                samples[i] = (Mathf.Sin(phase) + noise) * envelope * volume;
            }
            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static AudioClip MakeAmbience()
        {
            const int sampleRate = 22050;
            const float duration = 6f;
            int count = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float cycle = i / (float)count;
                float machinery = Mathf.Sin(t * Mathf.PI * 2f * 55f) * 0.18f + Mathf.Sin(t * Mathf.PI * 2f * 82.5f) * 0.1f;
                float ventilation = Mathf.Sin(cycle * Mathf.PI * 2f) * 0.12f + Mathf.Sin(cycle * Mathf.PI * 6f) * 0.06f;
                float pulse = Mathf.Sin(cycle * Mathf.PI * 4f) * 0.08f;
                samples[i] = machinery + ventilation + pulse;
            }
            AudioClip clip = AudioClip.Create("Mapped Lab Roomtone", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
