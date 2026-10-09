using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsReborn
{
    public sealed partial class GameSession : MonoBehaviour
    {
        private static GameSession instance;
        public WorldSessionState World { get; private set; }
        public string Status { get; private set; }
        public bool Busy { get; private set; }
        private float portalCooldown;
        private SaveGameData checkpoint;
        private readonly HashSet<string> party = new HashSet<string>();
        private float partyDownAt = -1;
        private bool replicaGameOver;
        public void AcceptReplica(WorldSessionState snapshot, bool gameOver, bool paused)
        {
            if (!NetworkCoop.IsReplica) return;
            World = snapshot; replicaGameOver = gameOver;
        }
        public void SetReplicaParty(IEnumerable<string> ids)
        { if (NetworkCoop.IsReplica) { party.Clear(); foreach (var id in ids) party.Add(id); } }
        public void ClearReplicaFlags() { replicaGameOver = false; chestRewards.Clear(); Time.timeScale = 1; }
        private sealed class Revival
        {
            public CharacterInventory target;
            public WorldSessionState world;
            public Vector2 origin;
            public float elapsed, lastTick;
        }
        private readonly Dictionary<string, Revival> revivals = new Dictionary<string, Revival>();
        public const float ReviveDuration = 3f;
        public bool IsReviving(CharacterInventory actor) => NetworkCoop.IsReplica ? NetworkCoop.Active.HasReviveIntent(actor)
            : actor != null && revivals.ContainsKey(actor.CharacterId);
        public float ReviveProgress(CharacterInventory actor) => NetworkCoop.IsReplica ? NetworkCoop.Active.ReplicaProgress(actor)
            : actor != null && revivals.TryGetValue(actor.CharacterId, out var channel)
            ? Mathf.Clamp01(channel.elapsed / ReviveDuration) : 0;
        public void CancelRevive(CharacterInventory actor)
        { if (NetworkCoop.IsReplica) NetworkCoop.Active.SetReviveIntent(actor, null); else if (actor != null) revivals.Remove(actor.CharacterId); }
        public bool CanRevive(CharacterInventory helper, CharacterInventory target) => (NetworkCoop.IsReplica
            ? helper != null && helper.LocalInput && !Busy && !RewardPresentationActive && !SaveBook.IsOpen
                && helper.GetComponent<InventoryInteraction>()?.IsOpen != true && helper.GetComponent<SpellCaster>()?.IsCasting!=true && !SpellRingMenu.Selecting(helper) && !World.CharacterVitals(helper.CharacterId).IsDown
            : CanFight(helper))
            && target != null && target.isActiveAndEnabled && (NetworkCoop.IsReplica || CanChangeVitals(target))
            && helper != target && helper.CharacterId != target.CharacterId
            && party.Contains(helper.CharacterId) && party.Contains(target.CharacterId)
            && helper.gameObject.scene == target.gameObject.scene && !IsGameOver
            && World.CharacterVitals(target.CharacterId).IsDown
            && target.GetComponent<CharacterDeath>()?.Phase == CharacterLifePhase.Downed
            && Vector2.Distance(helper.transform.position, target.transform.position) <= 1.6f
            && ClearCombatPath(helper.transform.position, target.transform.position)
            && helper.GetComponent<PlayerMelee>()?.IsSwinging != true;
        // Called while interaction is held. Duration and restored health are host-owned.
        public bool HoldRevive(CharacterInventory helper, CharacterInventory target)
        {
            if (!CanRevive(helper, target)) { CancelRevive(helper); return false; }
            if (NetworkCoop.IsReplica) return NetworkCoop.Active.SetReviveIntent(helper, target.CharacterId);
            if (!revivals.TryGetValue(helper.CharacterId, out var channel) || channel.target != target
                || channel.world != World || Time.time - channel.lastTick > .2f)
            {
                channel = new Revival { target = target, world = World, origin = helper.transform.position, lastTick = Time.time };
                revivals[helper.CharacterId] = channel;
            }
            if (Vector2.Distance(helper.transform.position, channel.origin) > .1f) { CancelRevive(helper); return false; }
            channel.elapsed += Mathf.Max(0, Time.time - channel.lastTick); channel.lastTick = Time.time;
            if (channel.elapsed >= ReviveDuration)
            { CancelRevive(helper); return ReviveCharacter(target, 2); }
            return true;
        }
        private IEnumerable<CharacterVitalsState> PartyVitals()
        { foreach (var id in party) yield return World.CharacterVitals(id); }
        public bool PartyDefeated => PartyRules.AllDown(PartyVitals());
        public bool IsGameOver => NetworkCoop.IsReplica ? replicaGameOver : !Busy && PartyDefeated && partyDownAt >= 0
            && Time.unscaledTime - partyDownAt >= CharacterDeath.AnimationSeconds + .15f;
        public bool CanRetryCheckpoint => !Busy && IsGameOver && checkpoint != null
            && (NetworkCoop.Running ? !NetworkCoop.IsReplica : party.Count == 1);
        public bool HasSavedCheckpoint { get; private set; }
        // Explicit host roster: scene unloads never remove a party member. The future
        // network adapter owns join/leave decisions and validates participant identity.
        public void RemovePartyMember(string characterId) => party.Remove(characterId);
        public bool ReviveCharacter(CharacterInventory actor, int halfHearts)
        {
            if (!CanChangeVitals(actor) || !World.CharacterVitals(actor.CharacterId).Revive(halfHearts)) return false;
            actor.GetComponent<CharacterDeath>()?.RefreshState(); return true;
        }
        public void RegisterSpawn(CharacterInventory actor)
        {
            if (actor == null || !actor.HasStateAuthority) return;
            party.Add(actor.CharacterId);
            if (checkpoint != null) return;
            var p = actor.transform.position;
            World.SetPosition(actor.CharacterId, actor.gameObject.scene.path, p.x, p.y, p.z);
            checkpoint = World.Capture();
        }
        public bool CanFight(CharacterInventory actor) => CanChangeVitals(actor)
            && !World.CharacterVitals(actor.CharacterId).IsDown && !SaveBook.IsOpen
            && (NetworkCoop.Active == null || !NetworkCoop.Active.MenuOpen(actor))
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
            if (NetworkCoop.Request(actor, CoopAction.Attack)) return true;
            if (!CanFight(actor) || IsReviving(actor) || actor.GetComponent<SpellCaster>()?.IsCasting==true || SpellRingMenu.BlocksInput(actor)) return false;
            var melee = actor.GetComponent<PlayerMelee>();
            if (melee == null || !melee.isActiveAndEnabled) return false;
            var weapon = actor.Find(actor.State.GetEquipment(EquipmentSlot.MainHand));
            if (weapon != null && (weapon.Rules.kind != ItemKind.Weapon || weapon.Weapon == null)) return false;
            var profile = weapon != null ? weapon.Weapon : null;
            if (!melee.AttackCooldown.TryUse(Time.time, profile != null ? profile.Cooldown : .45)) return false;
            melee.PresentSwing(profile, weapon != null);
            NetworkCoop.Active?.RecordSwing(actor);
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
        internal bool CanChangeVitals(CharacterInventory actor) => !NetworkCoop.IsReplica && !Busy && actor != null && !IsReceivingReward(actor) && actor.isActiveAndEnabled && actor.HasStateAuthority;
        public bool ApplyDamage(CharacterInventory actor, int amount)
        {
            if (!CanChangeVitals(actor) || !World.CharacterVitals(actor.CharacterId).Damage(CombatRules.MitigatedDamage(amount, actor.TotalArmor))) return false;
            CancelRevive(actor); return true;
        }
        public bool ApplyHealing(CharacterInventory actor, int amount) => CanChangeVitals(actor) && World.CharacterVitals(actor.CharacterId).Heal(amount);
        public bool TrySpendMana(CharacterInventory actor, int amount) => CanChangeVitals(actor) && World.CharacterVitals(actor.CharacterId).SpendMana(amount);
        public bool RestoreMana(CharacterInventory actor, int amount) => CanChangeVitals(actor) && World.CharacterVitals(actor.CharacterId).RestoreMana(amount);
        public bool AddHeartContainer(CharacterInventory actor) => CanChangeVitals(actor) && World.CharacterVitals(actor.CharacterId).AddHeartContainer();
        public bool AddManaCrystal(CharacterInventory actor,int amount=20) => CanChangeVitals(actor) && World.CharacterVitals(actor.CharacterId).AddManaCrystal(amount);
        public bool RequestAreaChange(AreaPortal portal, CharacterInventory actor)
        {
            if (Busy || HasAnyReward || Time.unscaledTime < portalCooldown || portal == null || !portal.CanUse(actor)
                || actor == null || World.CharacterVitals(actor.CharacterId).IsDown
                || SaveBook.IsOpen || actor.GetComponent<InventoryInteraction>()?.IsOpen == true) return false;
            if (NetworkCoop.Running) return !NetworkCoop.IsReplica && !NetworkCoop.Active.MenuOpen(actor) && NetworkCoop.Active.BeginArea(portal);
            // One host entry point. Replicated party readiness/spawning will be supplied by the network layer.
            if (FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None).Length != 1)
            { Status = "Gemeinsamer Koop-Gebietswechsel ist noch nicht angebunden."; return false; }
            try { ValidateScene(portal.TargetScenePath); }
            catch (Exception error) { Status = error.Message; Debug.LogError(Status); return false; }
            Busy = true; StartCoroutine(ChangeArea(portal.TargetScenePath, portal.TargetEntranceId, actor)); return true;
        }
        internal static void ValidateScene(string path)
        {
#if UNITY_EDITOR
            if (!System.IO.File.Exists(path)) throw new Exception("Zielszene fehlt: " + path);
#else
            if (!Application.CanStreamedLevelBeLoaded(path)) throw new Exception("Zielszene fehlt im Build: " + path);
#endif
        }
        internal static AsyncOperation OpenScene(string path)
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
        internal static void Warp(CharacterInventory character, Vector3 position)
        {
            var body = character.GetComponent<Rigidbody2D>();
            if (body != null) { body.linearVelocity = Vector2.zero; body.position = position; }
            character.transform.position = position;
            Physics2D.SyncTransforms();
            var camera = Camera.main;
            if (camera != null && camera.GetComponent<CameraFollow>()?.Target == character.transform)
                camera.transform.position = new Vector3(position.x, position.y, camera.transform.position.z);
        }
        // Cleanup callbacks must never create a replacement session during scene teardown.
        public static GameSession Existing => instance != null ? instance : null;
        internal void BeginNetworkArea()
        { Busy = true; revivals.Clear(); if (!PartyDefeated) partyDownAt = -1; }
        internal void EndNetworkArea()
        { Busy = false; portalCooldown = Time.unscaledTime + 1; Status = "Gruppe hat das Gebiet betreten."; }
        internal void ReplaceNetworkWorld(WorldSessionState restored, bool markSaved = true)
        { World = restored; checkpoint = restored.Capture(); if (markSaved) HasSavedCheckpoint = true; partyDownAt = -1; }
        internal void BeginFrontendWorld(WorldSessionState world)
        {
            World = world; party.Clear(); revivals.Clear(); chestRewards.Clear(); replicaGameOver = false;
            Busy = false; partyDownAt = -1; HasSavedCheckpoint = !string.IsNullOrEmpty(world.SavedScenePath);
            checkpoint = HasSavedCheckpoint ? world.Capture() : null;
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
                    if (instance.GetComponent<GameOverScreen>() == null) instance.gameObject.AddComponent<GameOverScreen>();
                    if (instance.GetComponent<CharacterNameLabels>() == null) instance.gameObject.AddComponent<CharacterNameLabels>();
                    if (instance.GetComponent<NetworkCoop>() == null) instance.gameObject.AddComponent<NetworkCoop>();
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
            if (NetworkCoop.IsReplica) return false;
            if (Busy || book == null || !book.CanUse(actor)) return false;
            try
            {
                foreach (var character in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None))
                {
                    var p = character.transform.position;
                    World.SetPosition(character.CharacterId, character.gameObject.scene.path, p.x, p.y, p.z);
                }
                World.SetSavedScene(actor.gameObject.scene.path);
                World.SetSavedHost(NetworkCoop.Running ? NetworkCoop.Active.LocalCharacter.CharacterId : actor.CharacterId);
                World.MarkSaved(NetworkCoop.Running ? NetworkCoop.Active.ActiveCharacterIds : new[] { actor.CharacterId });
                SaveGameStore.Save(World, SaveGameStore.SlotPath(slot));
                checkpoint = World.Capture();
                HasSavedCheckpoint = true;
                Status = "Slot " + (slot + 1) + " gespeichert."; return true;
            }
            catch (Exception error) { Status = "Speichern fehlgeschlagen: " + error.Message; Debug.LogError(Status); return false; }
        }
        public bool LoadAt(SaveBook book, CharacterInventory actor, int slot)
        {
            if (NetworkCoop.IsReplica) return false;
            if (Busy || book == null || !book.CanUse(actor)) return false;
            try
            {
                string path = SaveGameStore.SlotPath(slot);
                if (!SaveGameStore.Exists(path)) { Status = "Dieser Slot ist leer."; return false; }
                var restored = SaveGameStore.Load(path);
                if (NetworkCoop.Running)
                {
                    if (restored.WorldId != World.WorldId)
                    { Status = "Andere Welt: Bitte diesen Spielstand über Hauptmenü und Lobby starten."; return false; }
                    string destination = restored.SavedScenePath ?? restored.Position(actor.CharacterId)?.scenePath ?? actor.gameObject.scene.path;
                    ValidateScene(destination);
                    if (!NetworkCoop.Active.BeginLoad(restored, destination)) return false;
                    book.Close(); Status = "Alle Spieler müssen das Laden bestätigen."; return true;
                }
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
        private IEnumerator ApplyLoaded(WorldSessionState restored, string target, bool markSaved = true)
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
            foreach (var chest in FindObjectsByType<TreasureChest>(FindObjectsSortMode.None)) chest.RefreshSession();
            foreach (var loot in FindObjectsByType<EnemyLoot>(FindObjectsSortMode.None)) loot.RefreshSession();
            checkpoint = World.Capture();
            if (markSaved) HasSavedCheckpoint = true;
            Status = "Savegame geladen."; Busy = false;
        }
        private CharacterInventory DownedActor()
        {
            foreach (var actor in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None))
                if (actor.HasStateAuthority && World.CharacterVitals(actor.CharacterId).IsDown) return actor;
            return null;
        }
        public bool ReturnToCheckpoint()
        {
            if (NetworkCoop.Running)
            {
                if (!CanRetryCheckpoint) return false;
                try
                {
                    var restoredParty = WorldSessionState.Restore(checkpoint);
                    string hostId = NetworkCoop.Active.LocalCharacter.CharacterId;
                    string targetScene = restoredParty.SavedScenePath;
                    if (string.IsNullOrWhiteSpace(targetScene)) targetScene = restoredParty.Position(hostId)?.scenePath;
                    if (string.IsNullOrWhiteSpace(targetScene)) return false;
                    ValidateScene(targetScene);
                    foreach (var id in NetworkCoop.Active.ActiveCharacterIds)
                    {
                        var memberVitals = restoredParty.CharacterVitals(id);
                        if (memberVitals.IsDown) memberVitals.Revive(memberVitals.MaxHealth); else memberVitals.Heal(memberVitals.MaxHealth);
                        memberVitals.RestoreMana(memberVitals.MaxMana);
                    }
                    return NetworkCoop.Active.BeginLoad(restoredParty, targetScene, true);
                }
                catch (Exception error) { Status = "Gemeinsamer Neustart fehlgeschlagen: " + error.Message; Debug.LogError(Status); return false; }
            }
            var actor = DownedActor();
            if (!CanRetryCheckpoint || Busy || checkpoint == null || actor == null) return false;
            // Solo retry restores the host snapshot. Party revival requires a separate network policy.
            if (FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None).Length != 1) return false;
            var restored = WorldSessionState.Restore(checkpoint);
            var pose = restored.Position(actor.CharacterId);
            if (pose == null) return false;
            try { ValidateScene(pose.scenePath); }
            catch (Exception error) { Status = error.Message; return false; }
            var vitals = restored.CharacterVitals(actor.CharacterId);
            if (vitals.IsDown) vitals.Revive(vitals.MaxHealth); else vitals.Heal(vitals.MaxHealth);
            vitals.RestoreMana(vitals.MaxMana);
            Busy = true; StartCoroutine(ApplyLoaded(restored, pose.scenePath, false)); return true;
        }
        private void Update()
        {
            var stale = new List<string>();
            foreach (var pair in revivals)
                if (pair.Value.world != World || Time.time - pair.Value.lastTick > .2f || Busy || chestRewards.ContainsKey(pair.Key))
                    stale.Add(pair.Key);
            foreach (var id in stale) revivals.Remove(id);
            if (PartyDefeated)
            { if (!Busy && partyDownAt < 0) partyDownAt = Time.unscaledTime; }
            else partyDownAt = -1;
            if (IsGameOver && Application.isFocused)
            {
                var key = UnityEngine.InputSystem.Keyboard.current; var pad = UnityEngine.InputSystem.Gamepad.current;
                if (key != null && key.enterKey.wasPressedThisFrame || pad != null && pad.buttonSouth.wasPressedThisFrame) ReturnToCheckpoint();
            }
            if (SceneManager.GetActiveScene().path != NetworkCoop.MenuScene && !NetworkCoop.IsReplica && (Application.isFocused || NetworkCoop.Running) && !Busy && !IsGameOver && Time.timeScale > 0)
                World.AdvancePlayTime(Time.unscaledDeltaTime);
        }
    }
}
