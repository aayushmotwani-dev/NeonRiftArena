using UnityEngine;

namespace NeonRift
{
    [DisallowMultipleComponent]
    public sealed class PlayerPilot : MonoBehaviour
    {
        public float MaxHealth { get; private set; } = 100f;
        public float Health { get; private set; }
        public float DashCooldown => 1.15f;
        public float DashCooldownRemaining => dashCooldown;

        CharacterController controller;
        Transform visual;
        Transform weapon;
        Vector3 velocity;
        Vector3 dashDirection;
        float dashTime;
        float dashCooldown;
        float shotCooldown;
        float invulnerable;
        bool control = true;
        Material playerMaterial;
        TrailRenderer movementTrail;
        Light engineLight;

        public static PlayerPilot Create()
        {
            var root = new GameObject("Player");
            root.transform.position = new Vector3(0f, 0.55f, 0f);
            var pilot = root.AddComponent<PlayerPilot>();
            pilot.controller = root.AddComponent<CharacterController>();
            pilot.controller.height = 1.25f;
            pilot.controller.radius = 0.48f;
            pilot.controller.center = new Vector3(0f, 0.15f, 0f);

            var visualRoot = new GameObject("Interceptor Visual").transform;
            visualRoot.SetParent(root.transform, false);
            pilot.visual = visualRoot;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Pilot Core";
            body.transform.SetParent(visualRoot, false);
            body.transform.localScale = new Vector3(0.76f, 0.24f, 0.96f);
            body.transform.localPosition = Vector3.zero;
            Destroy(body.GetComponent<Collider>());
            pilot.playerMaterial = NeonMaterials.Make(new Color(0.055f, 0.16f, 0.18f), new Color(0.04f, 0.66f, 0.64f), 1.6f);
            body.GetComponent<Renderer>().material = pilot.playerMaterial;

            var cockpit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cockpit.name = "Amber Cockpit";
            cockpit.transform.SetParent(visualRoot, false);
            cockpit.transform.localPosition = new Vector3(0f, 0.36f, 0.04f);
            cockpit.transform.localScale = new Vector3(0.48f, 0.28f, 0.63f);
            Destroy(cockpit.GetComponent<Collider>());
            cockpit.GetComponent<Renderer>().material = NeonMaterials.Make(new Color(0.18f, 0.08f, 0.025f), new Color(1f, 0.42f, 0.08f), 1.45f);

            CreatePart(visualRoot, "Port Stabiliser", new Vector3(-0.76f, 0.02f, -0.08f), new Vector3(0.72f, 0.11f, 0.5f),
                Quaternion.Euler(0f, -12f, -8f), pilot.playerMaterial);
            CreatePart(visualRoot, "Starboard Stabiliser", new Vector3(0.76f, 0.02f, -0.08f), new Vector3(0.72f, 0.11f, 0.5f),
                Quaternion.Euler(0f, 12f, 8f), pilot.playerMaterial);

            Material engineMaterial = NeonMaterials.Make(new Color(0.015f, 0.12f, 0.13f), new Color(0.08f, 0.95f, 0.82f), 2.6f);
            CreatePart(visualRoot, "Port Engine", new Vector3(-0.48f, 0.02f, -0.74f), new Vector3(0.24f, 0.24f, 0.34f), Quaternion.identity, engineMaterial);
            CreatePart(visualRoot, "Starboard Engine", new Vector3(0.48f, 0.02f, -0.74f), new Vector3(0.24f, 0.24f, 0.34f), Quaternion.identity, engineMaterial);

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Emitter";
            nose.transform.SetParent(visualRoot, false);
            nose.transform.localPosition = new Vector3(0f, 0.08f, 0.76f);
            nose.transform.localScale = new Vector3(0.18f, 0.18f, 0.62f);
            Destroy(nose.GetComponent<Collider>());
            nose.GetComponent<Renderer>().material = NeonMaterials.Make(new Color(0.12f, 0.055f, 0.015f), new Color(1f, 0.45f, 0.08f), 1.8f);
            pilot.weapon = nose.transform;

            var light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 5f;
            light.intensity = 1.8f;
            light.color = new Color(0.08f, 0.82f, 0.72f);
            light.shadows = LightShadows.None;
            pilot.engineLight = light;

            pilot.movementTrail = root.AddComponent<TrailRenderer>();
            pilot.movementTrail.time = 0.3f;
            pilot.movementTrail.startWidth = 0.48f;
            pilot.movementTrail.endWidth = 0f;
            pilot.movementTrail.minVertexDistance = 0.08f;
            pilot.movementTrail.material = NeonMaterials.Line(new Color(0.08f, 0.8f, 0.7f, 0.62f));
            pilot.movementTrail.emitting = false;
            return pilot;
        }

        static void CreatePart(Transform parent, string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localRotation = rotation;
            Destroy(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().material = material;
        }

        void Update()
        {
            if (NeonRiftGame.I.Phase != GamePhase.Playing || !control) return;
            dashCooldown -= Time.deltaTime;
            shotCooldown -= Time.deltaTime;
            invulnerable -= Time.deltaTime;

            Vector3 input = new Vector3(
                (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f),
                0f,
                (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f));
            input = Vector3.ClampMagnitude(input, 1f);

            AimAtMouse();

            if (Input.GetKeyDown(KeyCode.Space) && dashCooldown <= 0f)
            {
                dashDirection = input.sqrMagnitude > 0.1f ? input : transform.forward;
                dashTime = 0.18f;
                dashCooldown = DashCooldown;
                invulnerable = Mathf.Max(invulnerable, 0.28f);
                NeonFX.Burst(transform.position, new Color(0.1f, 0.75f, 1f), 18, 5f);
                CameraJuice.Shake(0.16f, 0.16f);
                NeonAudio.Play(NeonSound.Dash, transform.position);
            }

            Vector3 desired = input * 8.2f;
            velocity = Vector3.Lerp(velocity, desired, 1f - Mathf.Exp(-14f * Time.deltaTime));
            Vector3 move = dashTime > 0f ? dashDirection * 27f : velocity;
            dashTime -= Time.deltaTime;
            controller.Move(move * Time.deltaTime);

            if (SpatialLayoutManager.I != null)
            {
                transform.position = SpatialLayoutManager.I.Constrain(transform.position, 0.72f);
            }
            else
            {
                Vector3 p = transform.position;
                Vector2 flat = new Vector2(p.x, p.z);
                if (flat.magnitude > NeonRiftGame.ArenaRadius - 0.8f)
                {
                    flat = flat.normalized * (NeonRiftGame.ArenaRadius - 0.8f);
                    transform.position = new Vector3(flat.x, p.y, flat.y);
                }
            }

            float bank = -Vector3.Dot(transform.right, velocity) * 1.6f;
            float pitch = Vector3.Dot(transform.forward, velocity) * 0.45f;
            visual.localRotation = Quaternion.Lerp(visual.localRotation, Quaternion.Euler(pitch, 0f, bank), 10f * Time.deltaTime);
            visual.localPosition = Vector3.up * (0.025f + Mathf.Sin(Time.time * 5.5f) * 0.025f);
            movementTrail.emitting = velocity.sqrMagnitude > 6f || dashTime > 0f;
            engineLight.intensity = Mathf.Lerp(engineLight.intensity, dashTime > 0f ? 4.4f : 1.7f + velocity.magnitude * 0.12f, 12f * Time.deltaTime);

            if (Input.GetMouseButton(0) && shotCooldown <= 0f)
            {
                shotCooldown = 0.105f;
                Fire();
            }
        }

        void AimAtMouse()
        {
            Ray ray = NeonRiftGame.I.GameCamera.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float enter)) return;
            Vector3 target = ray.GetPoint(enter);
            Vector3 direction = target - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.04f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 1f - Mathf.Exp(-25f * Time.deltaTime));
        }

        void Fire()
        {
            Vector3 origin = weapon.position + transform.forward * 0.25f;
            Projectile.Create(origin, transform.forward, true, 22f, 22f, new Color(0.15f, 0.9f, 1f));
            weapon.localScale = new Vector3(0.28f, 0.28f, 0.44f);
            CancelInvoke(nameof(ResetWeapon));
            Invoke(nameof(ResetWeapon), 0.05f);
            NeonFX.Burst(origin, new Color(0.15f, 0.9f, 1f), 4, 2.5f);
            NeonAudio.Play(NeonSound.Shot, origin);
        }

        void ResetWeapon() => weapon.localScale = new Vector3(0.18f, 0.18f, 0.62f);

        public void TakeDamage(float amount)
        {
            if (invulnerable > 0f || NeonRiftGame.I.Phase != GamePhase.Playing) return;
            Health = Mathf.Max(0f, Health - amount);
            invulnerable = 0.52f;
            NeonRiftGame.I.BreakCombo();
            NeonFX.Burst(transform.position, new Color(1f, 0.15f, 0.45f), 22, 6f);
            CameraJuice.Shake(0.25f, 0.34f);
            NeonAudio.Play(NeonSound.Hurt, transform.position);
            if (Health <= 0f)
            {
                control = false;
                movementTrail.emitting = false;
                visual.gameObject.SetActive(false);
                NeonRiftGame.I.Lose();
            }
        }

        public void Heal(float amount)
        {
            Health = Mathf.Min(MaxHealth, Health + amount);
            NeonFX.Burst(transform.position, new Color(0.2f, 1f, 0.55f), 12, 3f);
            NeonAudio.Play(NeonSound.Pickup, transform.position);
        }

        public void ResetPilot()
        {
            transform.position = new Vector3(0f, 0.55f, 0f);
            Health = MaxHealth;
            control = true;
            visual.gameObject.SetActive(true);
            movementTrail.Clear();
        }

        public void SetControl(bool enabled)
        {
            control = enabled;
            if (!enabled && movementTrail != null) movementTrail.emitting = false;
        }
    }
}
