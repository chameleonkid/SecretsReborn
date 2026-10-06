using UnityEngine;

namespace SecretsReborn
{
    [RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public sealed class TreeMeleeEnemy : MonoBehaviour
    {
        [SerializeField] private string enemyId;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private bool hasStateAuthority = true;
        private Rigidbody2D body;
        private SpriteRenderer image;
        private Vector2 home;
        private int facing;
        private int health = 6;
        private float flashUntil, knockbackUntil, walkElapsed;
        private Vector2 knockback;
        private int replicaFrame;
        private readonly ReplicaMotion replicaMotion = new ReplicaMotion();
        public void SetReplica(bool value)
        {
            if (value && hasStateAuthority) replicaMotion.Push(transform.position.x, transform.position.y, Time.unscaledTimeAsDouble, true);
            hasStateAuthority = !value; body.simulated = !value;
        }
        public CoopEnemyPose CaptureReplica() => new CoopEnemyPose { id = enemyId, x = transform.position.x, y = transform.position.y,
            active = gameObject.activeSelf, frame = frames != null ? System.Array.IndexOf(frames, image.sprite) : 0 };
        public void ApplyReplica(CoopEnemyPose pose)
        {
            SetReplica(true); replicaFrame = Mathf.Clamp(pose.frame, 0, 23);
            replicaMotion.Push(pose.x, pose.y, Time.unscaledTimeAsDouble,
                !pose.active || !gameObject.activeSelf || GameSession.Instance.RewardPresentationActive);
            gameObject.SetActive(pose.active);
        }
        private static readonly int[] WalkFrames = { 1, 2, 3, 2 };
        public bool CanContact => Alive && Time.time >= knockbackUntil;
        public string EnemyId => enemyId;
        public bool HasStateAuthority => hasStateAuthority;
        public bool Alive => health > 0 && isActiveAndEnabled;
        public void Configure(string id, Sprite[] sprites) { enemyId = id; frames = sprites; }
        public void SetIdentity(string id) => enemyId = id;
        private void Awake() { body = GetComponent<Rigidbody2D>(); image = GetComponent<SpriteRenderer>(); home = body.position; }
        private void Start()
        {
            if (string.IsNullOrWhiteSpace(enemyId)) { Debug.LogError("Baumgegner braucht eine eindeutige Enemy ID.", this); enabled = false; return; }
            RefreshSession();
        }
        public void RefreshSession()
        {
            health = 6; flashUntil = 0; knockbackUntil = 0; walkElapsed = 0;
            gameObject.SetActive(!GameSession.Instance.World.IsEnemyDefeated(enemyId));
        }
        private void FixedUpdate()
        {
            body.linearVelocity = Vector2.zero;
            if (!hasStateAuthority || GameSession.Instance.Busy || GameSession.Instance.IsGameOver || (!Application.isFocused && !NetworkCoop.Running) || !Alive) return;
            if (Time.time < knockbackUntil) { body.linearVelocity = knockback; return; }
            CharacterInventory target = null; float best = 5 * 5;
            foreach (var candidate in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None))
            {
                if (!GameSession.Instance.CanFight(candidate) || candidate.gameObject.scene != gameObject.scene) continue;
                float distance = ((Vector2)candidate.transform.position - body.position).sqrMagnitude;
                if (distance < best) { best = distance; target = candidate; }
            }
            if (target == null || Vector2.Distance(body.position, home) > 6)
            { Move(home - body.position); return; }
            Vector2 delta = (Vector2)target.transform.position - body.position;
            Move(delta);
        }
        private void Move(Vector2 delta)
        {
            if (delta.sqrMagnitude < .04f) return;
            Face(delta); body.linearVelocity = delta.normalized * 1.15f;
        }
        // Original LogWalk clips: down = row 0, up = row 1, right = row 2, left = row 3.
        private void Face(Vector2 delta) => facing = CombatRules.LogFacingRow(delta.x, delta.y);
        private void LateUpdate()
        {
            if (!hasStateAuthority)
            {
                replicaMotion.Sample(Time.unscaledTimeAsDouble, out float x, out float y);
                transform.position = new Vector3(x, y, transform.position.z);
                if (frames != null && replicaFrame < frames.Length) image.sprite = frames[replicaFrame]; return;
            }
            bool walking = body.linearVelocity.sqrMagnitude > .01f && Time.time >= knockbackUntil;
            walkElapsed = walking ? walkElapsed + Time.deltaTime : 0;
            if (frames != null && frames.Length >= 24) image.sprite = frames[facing * 6 + (walking ? WalkFrames[Mathf.FloorToInt(walkElapsed * 8) % 4] : 0)];
            image.color = Time.time < flashUntil ? new Color(1, .6f, .6f) : Color.white;
        }
        // Called by trusted host combat resolution, never by local input directly.
        public bool ReceiveHostHit(int amount, Vector2 push)
        {
            if (!hasStateAuthority || !Alive || amount <= 0) return false;
            health = Mathf.Max(0, health - amount); flashUntil = Time.time + .18f;
            knockback = push; knockbackUntil = Time.time + .18f;
            if (health == 0)
            {
                GameSession.Instance.World.DefeatEnemy(enemyId);
                foreach (var loot in FindObjectsByType<EnemyLoot>(FindObjectsSortMode.None)) loot.RefreshSession();
                gameObject.SetActive(false);
            }
            return true;
        }
        private void OnCollisionEnter2D(Collision2D collision) => Contact(collision);
        private void OnCollisionStay2D(Collision2D collision) => Contact(collision);
        private void Contact(Collision2D collision)
        {
            var actor = collision.collider.GetComponentInParent<CharacterInventory>();
            if (actor != null) GameSession.Instance.RequestEnemyContact(this, actor);
        }
        private void OnDisable() { if (body != null) body.linearVelocity = Vector2.zero; }
    }
}
