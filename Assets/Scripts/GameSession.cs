using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsReborn
{
    public sealed class GameSession : MonoBehaviour
    {
        private static GameSession instance;
        public WorldSessionState World { get; private set; }
        public string Status { get; private set; }
        public bool Busy { get; private set; }
        private float portalCooldown;
        public bool CanFight(CharacterInventory actor) => CanChangeVitals(actor)
            && !World.CharacterVitals(actor.CharacterId).IsDown && !SaveBook.IsOpen
            && actor.GetComponent<InventoryInteraction>()?.IsOpen != true;
        public bool ClearCombatPath(Vector2 from, Vector2 to)
        {
            foreach (var hit in Physics2D.LinecastAll(from, to))
                if (hit.collider != null && !hit.collider.isTrigger && hit.collider.GetComponentInParent<CharacterInventory>() == null
                    && hit.collider.GetComponentInParent<TreeMeleeEnemy>() == null) return false;
            return true;
        }
        public bool RequestMeleeAttack(CharacterInventory actor)
        {
            if (!CanFight(actor)) return false;
            var melee = actor.GetComponent<PlayerMelee>();
            if (melee == null || !melee.isActiveAndEnabled) return false;
            var weapon = actor.Find(actor.State.GetEquipment(EquipmentSlot.MainHand));
            if (weapon != null && (weapon.Rules.kind != ItemKind.Weapon || weapon.Weapon == null)) return false;
            var profile = weapon != null ? weapon.Weapon : null;
            if (!melee.AttackCooldown.TryUse(Time.time, profile != null ? profile.Cooldown : .45)) return false;
            melee.PresentSwing(profile, weapon != null);
            StartCoroutine(ResolveMelee(actor, melee, profile != null ? profile.Damage : 1,
                profile != null ? profile.Range : 1.6f, profile != null ? profile.HitDelay : .14f,
                profile != null ? profile.Knockback : 0));
            return true;
        }
        private IEnumerator ResolveMelee(CharacterInventory actor, PlayerMelee melee, int damage, float range, float delay, float knockback)
        {
            var scene = actor.gameObject.scene;
            var combatWorld = World;
            yield return new WaitForSeconds(delay);
            if (!CanFight(actor) || melee == null || actor.gameObject.scene != scene || World != combatWorld) yield break;
            foreach (var enemy in FindObjectsByType<TreeMeleeEnemy>(FindObjectsSortMode.None))
            {
                if (!enemy.Alive || !enemy.HasStateAuthority || enemy.gameObject.scene != actor.gameObject.scene) continue;
                var delta = enemy.transform.position - actor.transform.position;
                if (CombatRules.InArc(delta.x, delta.y, melee.AttackFacing.x, melee.AttackFacing.y, range)
                    && ClearCombatPath(actor.transform.position, enemy.transform.position))
                    enemy.ReceiveHostHit(damage, ((Vector2)delta).normalized * knockback);
            }
        }
        public bool RequestEnemyContact(TreeMeleeEnemy enemy, CharacterInventory actor)
        {
            if (enemy == null || !enemy.CanContact || !enemy.HasStateAuthority || !CanFight(actor)
                || enemy.gameObject.scene != actor.gameObject.scene
                || !ClearCombatPath(enemy.transform.position, actor.transform.position)) return false;
            var enemyCollider = enemy.GetComponent<Collider2D>();
            bool touching = false;
            foreach (var collider in actor.GetComponents<Collider2D>())
                if (!collider.isTrigger && enemyCollider.IsTouching(collider)) { touching = true; break; }
            if (!touching) return false;
            var melee = actor.GetComponent<PlayerMelee>();
            if (melee == null || !melee.HurtCooldown.TryUse(Time.time, .8)) return false;
            return ApplyDamage(actor, 1);
        }
        // Trusted host gameplay entry points. A future network adapter must validate
        // sender ownership and derive amounts from attacks/items, never client numbers.
        private bool CanChangeVitals(CharacterInventory actor) => !Busy && actor != null && actor.isActiveAndEnabled && actor.HasStateAuthority;
        public bool ApplyDamage(CharacterInventory actor, int amount) => CanChangeVitals(actor) && World.CharacterVitals(actor.CharacterId).Damage(amount);
        public bool ApplyHealing(CharacterInventory actor, int amount) => CanChangeVitals(actor) && World.CharacterVitals(actor.CharacterId).Heal(amount);
        public bool TrySpendMana(CharacterInventory actor, int amount) => CanChangeVitals(actor) && World.CharacterVitals(actor.CharacterId).SpendMana(amount);
        public bool RestoreMana(CharacterInventory actor, int amount) => CanChangeVitals(actor) && World.CharacterVitals(actor.CharacterId).RestoreMana(amount);
        public bool AddHeartContainer(CharacterInventory actor) => CanChangeVitals(actor) && World.CharacterVitals(actor.CharacterId).AddHeartContainer();
        public bool RequestAreaChange(AreaPortal portal, CharacterInventory actor)
        {
            if (Busy || Time.unscaledTime < portalCooldown || portal == null || !portal.CanUse(actor)
                || SaveBook.IsOpen || actor.GetComponent<InventoryInteraction>()?.IsOpen == true) return false;
            // One host entry point. Replicated party readiness/spawning will be supplied by the network layer.
            if (FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None).Length != 1)
            { Status = "Gemeinsamer Koop-Gebietswechsel ist noch nicht angebunden."; return false; }
            try { ValidateScene(portal.TargetScenePath); }
            catch (Exception error) { Status = error.Message; Debug.LogError(Status); return false; }
            Busy = true; StartCoroutine(ChangeArea(portal.TargetScenePath, portal.TargetEntranceId, actor)); return true;
        }
        private static void ValidateScene(string path)
        {
#if UNITY_EDITOR
            if (!System.IO.File.Exists(path)) throw new Exception("Zielszene fehlt: " + path);
#else
            if (!Application.CanStreamedLevelBeLoaded(path)) throw new Exception("Zielszene fehlt im Build: " + path);
#endif
        }
        private static AsyncOperation OpenScene(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            return SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
#endif
        }
        private IEnumerator ChangeArea(string path, string entranceId, CharacterInventory actor)
        {
            var movement = actor.GetComponent<PlayerMovement>(); bool enabled = movement != null && movement.enabled;
            if (movement != null) movement.enabled = false;
            AsyncOperation operation;
            try { operation = OpenScene(path); }
            catch (Exception error)
            { if (movement != null) movement.enabled = enabled; Busy = false; Status = error.Message; Debug.LogError(Status); yield break; }
            yield return operation; yield return null;
            try
            {
                AreaEntrance entrance = null;
                foreach (var candidate in FindObjectsByType<AreaEntrance>(FindObjectsSortMode.None))
                    if (candidate.EntranceId == entranceId) { if (entrance != null) throw new Exception("Eintrittspunkt doppelt: " + entranceId); entrance = candidate; }
                if (entrance == null) throw new Exception("Eintrittspunkt fehlt: " + entranceId);
                foreach (var character in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None))
                {
                    Warp(character, entrance.transform.position);
                    var p = character.transform.position;
                    World.SetPosition(character.CharacterId, character.gameObject.scene.path, p.x, p.y, p.z);
                }
                Status = "Gebiet betreten.";
            }
            catch (Exception error) { Status = error.Message; Debug.LogError(Status); }
            finally { Busy = false; portalCooldown = Time.unscaledTime + 1; }
        }
        private static void Warp(CharacterInventory character, Vector3 position)
        {
            var body = character.GetComponent<Rigidbody2D>();
            if (body != null) { body.linearVelocity = Vector2.zero; body.position = position; }
            character.transform.position = position;
            Physics2D.SyncTransforms();
            var camera = Camera.main;
            if (camera != null && camera.GetComponent<CameraFollow>()?.Target == character.transform)
                camera.transform.position = new Vector3(position.x, position.y, camera.transform.position.z);
        }
        public static GameSession Instance
        {
            get
            {
                if (instance == null)
                {
                    var existing = FindFirstObjectByType<GameSession>();
                    if (existing != null) { instance = existing; instance.World = new WorldSessionState("prototype-world"); }
                    else instance = new GameObject("Host session").AddComponent<GameSession>();
                    DontDestroyOnLoad(instance.gameObject);
                    if (instance.GetComponent<CharacterVitalsHud>() == null) instance.gameObject.AddComponent<CharacterVitalsHud>();
                }
                return instance;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeginPlaySession() { var session = Instance; }
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this; World = new WorldSessionState("prototype-world"); DontDestroyOnLoad(gameObject);
        }
        public bool SaveAt(SaveBook book, CharacterInventory actor, int slot)
        {
            if (Busy || book == null || !book.CanUse(actor)) return false;
            try
            {
                foreach (var character in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None))
                {
                    var p = character.transform.position;
                    World.SetPosition(character.CharacterId, character.gameObject.scene.path, p.x, p.y, p.z);
                }
                World.SetSavedScene(actor.gameObject.scene.path);
                SaveGameStore.Save(World, SaveGameStore.SlotPath(slot));
                Status = "Slot " + (slot + 1) + " gespeichert."; return true;
            }
            catch (Exception error) { Status = "Speichern fehlgeschlagen: " + error.Message; Debug.LogError(Status); return false; }
        }
        public bool LoadAt(SaveBook book, CharacterInventory actor, int slot)
        {
            if (Busy || book == null || !book.CanUse(actor)) return false;
            try
            {
                string path = SaveGameStore.SlotPath(slot);
                if (!SaveGameStore.Exists(path)) { Status = "Dieser Slot ist leer."; return false; }
                var restored = SaveGameStore.Load(path);
                var position = restored.Position(actor.CharacterId);
                string target = position != null ? position.scenePath : actor.gameObject.scene.path;
                if (target != SceneManager.GetActiveScene().path)
                {
                    ValidateScene(target);
                }
                book.Close(); Busy = true;
                StartCoroutine(ApplyLoaded(restored, target)); return true;
            }
            catch (Exception error) { Status = "Laden fehlgeschlagen: " + error.Message; Debug.LogError(Status); return false; }
        }
        private IEnumerator ApplyLoaded(WorldSessionState restored, string target)
        {
            var previous = World; World = restored;
            AsyncOperation operation = null;
            try
            {
                if (target != SceneManager.GetActiveScene().path)
                {
                    operation = OpenScene(target);
                }
            }
            catch (Exception error) { World = previous; Busy = false; Status = "Gebiet konnte nicht geladen werden: " + error.Message; yield break; }
            if (operation != null) yield return operation;
            yield return null;
            foreach (var character in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None))
            {
                character.RefreshSession();
                var p = World.Position(character.CharacterId);
                if (p != null && p.scenePath == character.gameObject.scene.path)
                {
                    Warp(character, new Vector3(p.x, p.y, p.z));
                }
            }
            Physics2D.SyncTransforms();
            foreach (var item in FindObjectsByType<WorldItem>(FindObjectsInactive.Include, FindObjectsSortMode.None)) item.RefreshSession();
            foreach (var puzzle in FindObjectsByType<SanctuaryPuzzle>(FindObjectsSortMode.None)) puzzle.RefreshSession();
            foreach (var enemy in FindObjectsByType<TreeMeleeEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)) enemy.RefreshSession();
            Status = "Savegame geladen."; Busy = false;
        }
        private void Update()
        {
            if (Application.isFocused && !Busy && Time.timeScale > 0)
                World.AdvancePlayTime(Time.unscaledDeltaTime);
        }
    }
}
