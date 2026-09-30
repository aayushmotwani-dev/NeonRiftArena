using System.Collections.Generic;
using UnityEngine;

namespace NeonRift
{
    public enum EnemyKind { Drone, Striker, Turret, Guardian }

    public sealed class Projectile : MonoBehaviour
    {
        Vector3 direction;
        float speed;
        float damage;
        float age;
        bool friendly;

        public static Projectile Create(Vector3 position, Vector3 direction, bool friendly, float speed, float damage, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = friendly ? "Pulse Bolt" : "Hostile Bolt";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * (friendly ? 0.22f : 0.32f);
            Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().material = NeonMaterials.Make(color * 0.3f, color, 4f);
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.24f;
            trail.startWidth = friendly ? 0.18f : 0.26f;
            trail.endWidth = 0f;
            trail.material = NeonMaterials.Line(color);
            var projectile = go.AddComponent<Projectile>();
            projectile.direction = direction.normalized;
            projectile.speed = speed;
            projectile.damage = damage;
            projectile.friendly = friendly;
            return projectile;
        }

        void Update()
        {
            if (NeonRiftGame.I.Phase != GamePhase.Playing) return;
            transform.position += direction * speed * Time.deltaTime;
            age += Time.deltaTime;
            if (age > 4f || new Vector2(transform.position.x, transform.position.z).magnitude > NeonRiftGame.ArenaRadius + 3f)
            {
                Destroy(gameObject);
                return;
            }

            if (friendly)
            {
                var enemies = EnemyAgent.Active;
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    EnemyAgent enemy = enemies[i];
                    if (enemy == null) continue;
                    float radius = enemy.Kind == EnemyKind.Guardian ? 1.7f : 0.75f;
                    if ((enemy.transform.position - transform.position).sqrMagnitude < radius * radius)
                    {
                        enemy.Hit(damage, direction);
                        NeonFX.Burst(transform.position, new Color(0.1f, 0.9f, 1f), 7, 3.5f);
                        Destroy(gameObject);
                        return;
                    }
                }
            }
            else
            {
                PlayerPilot player = NeonRiftGame.I.Player;
                if (player != null && (player.transform.position - transform.position).sqrMagnitude < 0.66f)
                {
                    player.TakeDamage(damage);
                    Destroy(gameObject);
                }
            }
        }
    }

    public sealed class EnemyAgent : MonoBehaviour
    {
        public static readonly List<EnemyAgent> Active = new List<EnemyAgent>();
        public EnemyKind Kind { get; private set; }
        public float HealthNormalized => maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;

        float health;
        float maxHealth;
        float speed;
        float contactDamage;
        float attackTimer;
        float hitFlash;
        float orbitSign;
        float phase;
        Renderer bodyRenderer;
        Material bodyMaterial;
        Color baseColor;
        bool dying;
        Vector3 authoredScale;
        readonly List<Material> visualMaterials = new List<Material>();
        readonly List<Color> visualBaseColors = new List<Color>();
        readonly List<string> visualColorProperties = new List<string>();

        public static EnemyAgent Create(EnemyKind kind, Vector3 position)
        {
            PrimitiveType primitive = kind == EnemyKind.Guardian ? PrimitiveType.Cylinder :
                                      kind == EnemyKind.Turret ? PrimitiveType.Cube : PrimitiveType.Sphere;
            var go = GameObject.CreatePrimitive(primitive);
            go.name = "Enemy " + kind;
            go.transform.position = position;
            Destroy(go.GetComponent<Collider>());
            var enemy = go.AddComponent<EnemyAgent>();
            enemy.Kind = kind;
            enemy.orbitSign = Random.value < 0.5f ? -1f : 1f;
            enemy.phase = Random.Range(0f, 10f);
            enemy.Configure();
            return enemy;
        }

        void Configure()
        {
            switch (Kind)
            {
                case EnemyKind.Drone:
                    maxHealth = 38f; speed = 3.4f; contactDamage = 12f; baseColor = new Color(1f, 0.1f, 0.48f);
                    transform.localScale = Vector3.one * 1.05f;
                    break;
                case EnemyKind.Striker:
                    maxHealth = 58f; speed = 4.8f; contactDamage = 16f; baseColor = new Color(1f, 0.45f, 0.06f);
                    transform.localScale = new Vector3(0.8f, 0.8f, 1.25f);
                    break;
                case EnemyKind.Turret:
                    maxHealth = 72f; speed = 2.1f; contactDamage = 10f; baseColor = new Color(0.8f, 0.15f, 1f);
                    transform.localScale = Vector3.one * 1.05f;
                    attackTimer = Random.Range(0.5f, 1.1f);
                    break;
                default:
                    maxHealth = 620f; speed = 1.7f; contactDamage = 24f; baseColor = new Color(1f, 0.06f, 0.22f);
                    transform.localScale = new Vector3(3f, 1f, 3f);
                    attackTimer = 1.2f;
                    break;
            }
            health = maxHealth;
            bodyRenderer = GetComponent<Renderer>();
            bodyMaterial = NeonMaterials.Make(baseColor * 0.16f, baseColor, Kind == EnemyKind.Guardian ? 3.6f : 2.2f);
            bodyRenderer.material = bodyMaterial;

            string modelPath = Kind == EnemyKind.Drone ? "SpatialAssets/Aliens/Alien_Cyclop" :
                               Kind == EnemyKind.Striker ? "SpatialAssets/Aliens/Alien_Scolitex" :
                               "SpatialAssets/Aliens/Alien_Oculichrysalis";
            float modelHeight = Kind == EnemyKind.Guardian ? 3.1f : Kind == EnemyKind.Turret ? 1.65f : 1.35f;
            if (SpatialLayoutManager.AttachEnemyModel(transform, modelPath, modelHeight, new Vector3(0f, -0.45f, 0f)))
                bodyRenderer.enabled = false;

            authoredScale = transform.localScale;
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                foreach (Material material in renderer.materials)
                {
                    string colorProperty = material.HasProperty("_Color") ? "_Color" : material.HasProperty("_Tint") ? "_Tint" : null;
                    if (colorProperty == null) continue;
                    visualMaterials.Add(material);
                    visualBaseColors.Add(material.GetColor(colorProperty));
                    visualColorProperties.Add(colorProperty);
                }
            }

            var ring = new GameObject("Threat Ring").AddComponent<LineRenderer>();
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            ring.loop = true;
            ring.positionCount = 28;
            ring.widthMultiplier = Kind == EnemyKind.Guardian ? 0.075f : 0.035f;
            ring.material = NeonMaterials.Line(baseColor);
            float radius = Kind == EnemyKind.Guardian ? 0.62f : 0.7f;
            for (int i = 0; i < 28; i++)
            {
                float a = i / 28f * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        void Update()
        {
            if (dying || NeonRiftGame.I.Phase != GamePhase.Playing) return;
            PlayerPilot player = NeonRiftGame.I.Player;
            if (player == null) return;

            attackTimer -= Time.deltaTime;
            hitFlash -= Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, authoredScale, 1f - Mathf.Exp(-18f * Time.deltaTime));
            ApplyHitFlash(Mathf.Clamp01(hitFlash / 0.08f));
            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            Vector3 direction = distance > 0.01f ? toPlayer / distance : Vector3.forward;
            Vector3 move;

            if (Kind == EnemyKind.Turret)
            {
                move = (distance > 10f ? direction : distance < 7f ? -direction : Vector3.Cross(Vector3.up, direction) * orbitSign) * speed;
                if (attackTimer <= 0f)
                {
                    attackTimer = 1.35f;
                    Shoot(direction, 10.5f, 11f);
                }
            }
            else if (Kind == EnemyKind.Guardian)
            {
                move = direction * speed;
                if (attackTimer <= 0f)
                {
                    attackTimer = health < maxHealth * 0.5f ? 1.25f : 1.8f;
                    RadialBurst(health < maxHealth * 0.5f ? 14 : 10);
                    CameraJuice.Shake(0.18f, 0.18f);
                }
            }
            else if (Kind == EnemyKind.Striker)
            {
                Vector3 side = Vector3.Cross(Vector3.up, direction) * Mathf.Sin(Time.time * 4.5f + phase) * 0.75f;
                move = (direction + side).normalized * speed;
            }
            else move = direction * speed;

            transform.position += move * Time.deltaTime;
            if (SpatialLayoutManager.I != null)
                transform.position = SpatialLayoutManager.I.Constrain(transform.position, Kind == EnemyKind.Guardian ? 1.8f : 0.7f);
            if (direction.sqrMagnitude > 0.1f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 8f * Time.deltaTime);
            transform.Rotate(Vector3.up, Kind == EnemyKind.Guardian ? 18f * Time.deltaTime : 55f * Time.deltaTime, Space.Self);

            if (distance < (Kind == EnemyKind.Guardian ? 2.25f : 1.05f) && attackTimer <= 0f)
            {
                player.TakeDamage(contactDamage);
                attackTimer = 0.72f;
            }
        }

        void Shoot(Vector3 direction, float projectileSpeed, float damage)
        {
            Projectile.Create(transform.position + direction * 0.8f + Vector3.up * 0.15f, direction, false, projectileSpeed, damage, baseColor);
            NeonFX.Burst(transform.position, baseColor, 6, 2.5f);
        }

        void RadialBurst(int count)
        {
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f + Time.time * 0.3f;
                Vector3 direction = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Projectile.Create(transform.position + direction * 1.8f, direction, false, 8.5f, 12f, baseColor);
            }
            NeonFX.Burst(transform.position, baseColor, 24, 5f);
            NeonAudio.Play(NeonSound.Boss, transform.position);
        }

        public void Hit(float damage, Vector3 impactDirection)
        {
            if (dying) return;
            health -= damage;
            hitFlash = 0.08f;
            transform.localScale = authoredScale * (Kind == EnemyKind.Guardian ? 1.045f : 1.12f);
            transform.position += impactDirection * 0.08f;
            if (health <= 0f) Die();
        }

        void ApplyHitFlash(float amount)
        {
            for (int i = 0; i < visualMaterials.Count; i++)
                visualMaterials[i].SetColor(visualColorProperties[i], Color.Lerp(visualBaseColors[i], Color.white, amount * 0.72f));
        }

        void Die()
        {
            dying = true;
            int points = Kind == EnemyKind.Guardian ? 2500 : Kind == EnemyKind.Turret ? 300 : Kind == EnemyKind.Striker ? 220 : 150;
            NeonRiftGame.I.AddKill(transform.position, points);
            NeonFX.Burst(transform.position, baseColor, Kind == EnemyKind.Guardian ? 64 : 24, Kind == EnemyKind.Guardian ? 10f : 6f);
            CameraJuice.Shake(Kind == EnemyKind.Guardian ? 0.55f : 0.12f, Kind == EnemyKind.Guardian ? 0.5f : 0.12f);
            NeonAudio.Play(Kind == EnemyKind.Guardian ? NeonSound.Boss : NeonSound.Explosion, transform.position);
            if (Kind != EnemyKind.Guardian && Random.value < 0.22f)
                PickupOrb.Create(transform.position);
            NeonRiftGame.I.Director.NotifyEnemyDown(this);
            Destroy(gameObject);
        }
    }

    public sealed class PickupOrb : MonoBehaviour
    {
        Vector3 start;
        float age;

        public static void Create(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Repair Shard";
            go.transform.position = position + Vector3.up * 0.5f;
            go.transform.localScale = Vector3.one * 0.42f;
            Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().material = NeonMaterials.Make(new Color(0.02f, 0.3f, 0.12f), new Color(0.1f, 1f, 0.45f), 4f);
            go.AddComponent<PickupOrb>().start = go.transform.position;
        }

        void Update()
        {
            age += Time.deltaTime;
            transform.position = start + Vector3.up * (Mathf.Sin(age * 4f) * 0.18f);
            transform.Rotate(Vector3.up, 120f * Time.deltaTime);
            if ((NeonRiftGame.I.Player.transform.position - transform.position).sqrMagnitude < 1.4f)
            {
                NeonRiftGame.I.Player.Heal(18f);
                Destroy(gameObject);
            }
            else if (age > 10f) Destroy(gameObject);
        }
    }
}
