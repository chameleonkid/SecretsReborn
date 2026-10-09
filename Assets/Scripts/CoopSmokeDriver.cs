using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SecretsReborn
{
    // Opt-in development-build integration checks, run in two independent players.
    // This component is never added in a normal session or release build.
    public sealed class CoopSmokeDriver : MonoBehaviour
    {
        private NetworkCoop net;
        private float deadline;
        private bool host;
        private string report;
        private bool finished;
        private void Awake()
        {
            net = GetComponent<NetworkCoop>(); deadline = Time.unscaledTime + 100;
            var args = Environment.GetCommandLineArgs(); host = Array.IndexOf(args, "--coop-smoke-host") >= 0;
            int index = Array.IndexOf(args, "--coop-smoke-report");
            report = index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
        private void Update()
        { if (!finished && Time.unscaledTime > deadline) Finish("FAIL: integration timeout; " + net.Status); }
        private void Finish(string result)
        {
            if (finished) return; finished = true;
            Debug.Log(result); if (!string.IsNullOrWhiteSpace(report)) File.WriteAllText(report, result);
            Application.Quit(result.StartsWith("PASS") ? 0 : 1);
        }
        internal void Disconnected() => Finish("FAIL: connection ended before integration checks completed");
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            bool started = host ? net.StartHost() : net.StartClient("127.0.0.1");
            if (!started) { Finish("FAIL: transport start; " + net.Status); yield break; }
            if (host)
            {
                foreach (var enemy in FindObjectsByType<TreeMeleeEnemy>(FindObjectsSortMode.None)) enemy.enabled = false;
                while (net.ConnectedCount < 2) yield return null;
                CharacterInventory guest = null;
                foreach (var actor in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None)) if (actor != net.LocalCharacter) guest = actor;
                if (guest == null || !guest.HasStateAuthority || guest.CharacterId == net.LocalCharacter.CharacterId)
                { Finish("FAIL: distinct host-owned characters"); yield break; }
                string initialGuestId = guest.CharacterId;
                GameSession.Instance.ApplyDamage(net.LocalCharacter, 999); GameSession.Instance.ApplyDamage(guest, 999);
                yield return new WaitForSecondsRealtime(2);
                if (GameSession.Instance.HasSavedCheckpoint || !GameSession.Instance.IsGameOver || !GameSession.Instance.ReturnToCheckpoint())
                { Finish("FAIL: initial party checkpoint retry"); yield break; }
                while (net.VotePending || net.ChangingArea) yield return null;
                guest = null;
                foreach (var member in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None)) if (member.CharacterId == initialGuestId) guest = member;
                if (guest == null || net.ConnectedCount != 2 || GameSession.Instance.IsGameOver || GameSession.Instance.HasSavedCheckpoint
                    || GameSession.Instance.World.CharacterVitals(initialGuestId).Health != 6 || guest.State.EquippedArmorId != null)
                { Finish("FAIL: initial checkpoint restored party or identities"); yield break; }
                foreach (var enemy in FindObjectsByType<TreeMeleeEnemy>(FindObjectsSortMode.None)) enemy.enabled = false;
                Vector2 initial = guest.transform.position;
                float until = Time.unscaledTime + 10;
                while (Vector2.Distance(initial, guest.transform.position) < .2f && Time.unscaledTime < until) yield return null;
                if (Vector2.Distance(initial, guest.transform.position) < .2f) { Finish("FAIL: client movement not simulated on host"); yield break; }
                var armor = guest.Find("training-armor");
                if (armor == null || !guest.TryReceive(armor, 1)) { Finish("FAIL: host equipment catalog"); yield break; }
                until = Time.unscaledTime + 10;
                while (guest.State.EquippedArmorId != "training-armor" && Time.unscaledTime < until) yield return null;
                if (guest.State.EquippedArmorId != "training-armor" || net.LocalCharacter.State.EquippedArmorId != null)
                { Finish("FAIL: independent replicated equipment"); yield break; }
                string guestId = guest.CharacterId;
                var chest = FindFirstObjectByType<TreasureChest>();
                if (chest == null || chest.Opened) { Finish("FAIL: test chest missing or opened"); yield break; }
                GameSession.Warp(guest, chest.transform.position + Vector3.down * .9f);
                until = Time.unscaledTime + 10;
                while (!GameSession.Instance.IsReceivingReward(guest) && Time.unscaledTime < until) yield return null;
                if (!GameSession.Instance.IsReceivingReward(guest) || Time.timeScale != 1
                    || GameSession.Instance.ApplyDamage(guest, 999) || !GameSession.Instance.CanFight(net.LocalCharacter))
                { Finish("FAIL: chest reward must protect only recipient without world pause"); yield break; }
                Vector2 rewardOrigin = guest.transform.position;
                var recipientBody = guest.GetComponent<Rigidbody2D>();
                if (recipientBody.constraints != RigidbodyConstraints2D.FreezeAll)
                { Finish("FAIL: chest recipient physics not frozen"); yield break; }
                var solid = Array.Find(guest.GetComponents<Collider2D>(), collider => !collider.isTrigger);
                var pushers = new GameObject[2];
                for (int side = 0; side < pushers.Length; side++)
                {
                    // Dynamic collision bodies model players/enemies pushing the
                    // stationary recipient. Initial overlap forces contact resolution.
                    pushers[side] = new GameObject("Chest collision regression", typeof(Rigidbody2D), typeof(CircleCollider2D));
                    Vector3 direction = side == 0 ? Vector3.right : Vector3.up;
                    pushers[side].transform.position = solid.bounds.center - direction * .2f;
                    var pusher = pushers[side].GetComponent<Rigidbody2D>(); pusher.gravityScale = 0; pusher.mass = 50;
                    pushers[side].GetComponent<CircleCollider2D>().radius = .35f;
                    pusher.linearVelocity = direction * 8;
                }
                double playTime = GameSession.Instance.World.PlayTimeSeconds;
                yield return new WaitForSecondsRealtime(.35f);
                foreach (var pusher in pushers) Destroy(pusher);
                if (Vector2.Distance(rewardOrigin, guest.transform.position) > .01f || GameSession.Instance.World.PlayTimeSeconds <= playTime)
                { Finish("FAIL: chest movement lock or running world time"); yield break; }
                while (GameSession.Instance.IsReceivingReward(guest) && Time.unscaledTime < until) yield return null;
                if (GameSession.Instance.IsReceivingReward(guest) || !GameSession.Instance.CanFight(guest)
                    || chest.TryOpen(guest) || !GameSession.Instance.World.IsChestItemDiscovered("training-armor"))
                { Finish("FAIL: chest confirmation, duplicate claim or shared discovery"); yield break; }
                if (recipientBody.constraints == RigidbodyConstraints2D.FreezeAll)
                { Finish("FAIL: chest confirmation did not restore physics constraints"); yield break; }
                var runeLamp = guest.Find("rune-lamp");
                if (runeLamp == null || !guest.TryReceive(runeLamp, 1))
                { Finish("FAIL: guest rune lamp equipment"); yield break; }
                int lampSlot = -1;
                for (int slot = 0; slot < InventoryState.Capacity; slot++) if (guest.State.GetSlot(slot)?.itemId == "rune-lamp") lampSlot = slot;
                if (!guest.TryEquip(lampSlot, EquipmentSlot.Lamp)) { Finish("FAIL: guest rune lamp slot"); yield break; }
                guest.GetComponent<PlayerLantern>().SetLit(true);
                for (int rune = 0; rune < 4; rune++)
                {
                    RuneCircle circle = null;
                    foreach (var candidate in FindObjectsByType<RuneCircle>(FindObjectsSortMode.None)) if (candidate.Order == rune) circle = candidate;
                    if (circle == null) { Finish("FAIL: rune circle missing"); yield break; }
                    GameSession.Warp(guest, circle.transform.position);
                    yield return new WaitForSecondsRealtime(.15f);
                    if (GameSession.Instance.World.Puzzle("forest-sanctuary-source").Progress != rune + 1)
                    { Finish("FAIL: guest cannot activate rune " + rune); yield break; }
                }
                GameSession.Warp(guest, net.LocalCharacter.transform.position + Vector3.right * .9f);
                var testWorld = GameSession.Instance.World;
                testWorld.TryCollect("coop-test:chest", () => true);
                for (int rune = 0; rune < 4; rune++) testWorld.Puzzle("coop-test:runes").Enter(rune);
                int guestHealth = testWorld.CharacterVitals(guestId).Health;
                string unchangedScene = guest.gameObject.scene.path;
                net.TestVoteAuto = false;
                if (!net.BeginArea(FindFirstObjectByType<AreaPortal>())) { Finish("FAIL: decline vote start"); yield break; }
                yield return new WaitForSecondsRealtime(.5f);
                if (!net.VotePending || net.ChangingArea || guest.gameObject.scene.path != unchangedScene)
                { Finish("FAIL: transitioned without unanimous consent"); yield break; }
                net.AnswerVote(false);
                yield return new WaitForSecondsRealtime(.5f);
                if (net.VotePending || GameSession.Instance.Busy || guest.gameObject.scene.path != unchangedScene)
                { Finish("FAIL: rejected transition failed to cancel"); yield break; }
                net.TestVoteAuto = true;
                string testSave = Path.Combine(Path.GetDirectoryName(report), "CoopLoadCheck-" + Guid.NewGuid().ToString("N") + ".es3");
                for (int trip = 0; trip < 3; trip++)
                {
                    AreaPortal exit = FindFirstObjectByType<AreaPortal>();
                    if (trip == 2)
                    {
                        // Deliberately save a distant guest coordinate: loading must
                        // regroup the party around the host rather than restore it.
                        testWorld.SetPosition(guestId, guest.gameObject.scene.path,
                            net.LocalCharacter.transform.position.x + 20, net.LocalCharacter.transform.position.y, 0);
                        SaveGameStore.Save(testWorld, testSave);
                        var restored = SaveGameStore.Load(testSave);
                        guest.TryUnequip(EquipmentSlot.Armor);
                        testWorld.TryCollect("coop-test:after-save", () => true);
                        if (!net.BeginLoad(restored, guest.gameObject.scene.path)) { Finish("FAIL: network load start"); yield break; }
                    }
                    else if (exit == null || !net.BeginArea(exit)) { Finish("FAIL: coordinated area start"); yield break; }
                    while (net.VotePending || net.ChangingArea) yield return null;
                    if (trip == 2) testWorld = GameSession.Instance.World;
                    guest = null;
                    foreach (var member in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None))
                        if (member.CharacterId == guestId) guest = member;
                    if (guest == null || net.ConnectedCount != 2 || guest.State.EquippedArmorId != "training-armor"
                        || net.LocalCharacter.State.EquippedArmorId != null || GameSession.Instance.Busy
                        || !ReferenceEquals(testWorld, GameSession.Instance.World) || !testWorld.IsCollected("coop-test:chest")
                        || !testWorld.Puzzle("coop-test:runes").IsComplete || testWorld.CharacterVitals(guestId).Health != guestHealth
                        || testWorld.Position(guestId)?.scenePath != guest.gameObject.scene.path)
                    { Finish("FAIL: area readiness trip=" + trip + " owners=" + net.ConnectedCount + " guest=" + (guest != null)
                        + " armor=" + guest?.State.EquippedArmorId + " busy=" + GameSession.Instance.Busy
                        + " sameWorld=" + ReferenceEquals(testWorld, GameSession.Instance.World)
                        + " health=" + (guest == null ? -1 : testWorld.CharacterVitals(guestId).Health)
                        + " positionScene=" + testWorld.Position(guestId)?.scenePath + " actorScene=" + guest?.gameObject.scene.path); yield break; }
                    if (trip == 2 && testWorld.IsCollected("coop-test:after-save")) { Finish("FAIL: loaded world failed to roll back"); yield break; }
                    if (trip == 2 && Vector2.Distance(guest.transform.position, net.LocalCharacter.transform.position) > 1.6f)
                    { Finish("FAIL: loaded guest did not regroup at host spawn"); yield break; }
                    foreach (var enemy in FindObjectsByType<TreeMeleeEnemy>(FindObjectsSortMode.None)) enemy.enabled = false;
                    yield return new WaitForSecondsRealtime(1);
                }
                // Keep the revive test on known floor, away from the decorative collision shapes.
                guest.GetComponent<Rigidbody2D>().position = net.LocalCharacter.transform.position + Vector3.right * .9f;
                guest.transform.position = net.LocalCharacter.transform.position + Vector3.right * .9f;
                GameSession.Instance.ApplyDamage(net.LocalCharacter, 999);
                yield return new WaitForSecondsRealtime(1.5f);
                if (GameSession.Instance.IsGameOver) { Finish("FAIL: single death triggered party game over"); yield break; }
                until = Time.unscaledTime + 15;
                while (GameSession.Instance.World.CharacterVitals(net.LocalCharacter.CharacterId).IsDown && Time.unscaledTime < until) yield return null;
                if (GameSession.Instance.World.CharacterVitals(net.LocalCharacter.CharacterId).Health != 2)
                { Finish("FAIL: client-held authoritative revive"); yield break; }
                // Allow the client to observe the successful revival before the
                // next deliberately fatal hit; these are distinct gameplay steps.
                yield return new WaitForSecondsRealtime(.6f);
                GameSession.Instance.TrySpendMana(guest, 17);
                GameSession.Instance.ApplyDamage(net.LocalCharacter, 999); GameSession.Instance.ApplyDamage(guest, 999);
                yield return new WaitForSecondsRealtime(1.5f);
                if (!GameSession.Instance.IsGameOver) { Finish("FAIL: both-down game over"); yield break; }
                GameSession.Instance.World.TryCollect("coop-test:after-death", () => true);
                net.TestVoteAuto = false;
                if (!GameSession.Instance.ReturnToCheckpoint()) { Finish("FAIL: retry rejection start"); yield break; }
                yield return new WaitForSecondsRealtime(.4f);
                net.AnswerVote(false);
                yield return new WaitForSecondsRealtime(.2f);
                if (!GameSession.Instance.IsGameOver || GameSession.Instance.Busy || !GameSession.Instance.World.IsCollected("coop-test:after-death"))
                { Finish("FAIL: rejected retry must keep game over and world unchanged"); yield break; }
                net.TestVoteAuto = true;
                if (!GameSession.Instance.ReturnToCheckpoint()) { Finish("FAIL: saved party checkpoint retry start"); yield break; }
                while (net.VotePending || net.ChangingArea) yield return null;
                guest = null;
                foreach (var member in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None)) if (member.CharacterId == guestId) guest = member;
                if (guest == null || GameSession.Instance.IsGameOver || GameSession.Instance.World.IsCollected("coop-test:after-death")
                    || guest.State.EquippedArmorId != "training-armor"
                    || GameSession.Instance.World.CharacterVitals(guestId).Health != GameSession.Instance.World.CharacterVitals(guestId).MaxHealth
                    || GameSession.Instance.World.CharacterVitals(guestId).Mana != GameSession.Instance.World.CharacterVitals(guestId).MaxMana
                    || GameSession.Instance.World.CharacterVitals(net.LocalCharacter.CharacterId).Health != GameSession.Instance.World.CharacterVitals(net.LocalCharacter.CharacterId).MaxHealth
                    || GameSession.Instance.World.CharacterVitals(net.LocalCharacter.CharacterId).Mana != GameSession.Instance.World.CharacterVitals(net.LocalCharacter.CharacterId).MaxMana
                    || Vector2.Distance(guest.transform.position, net.LocalCharacter.transform.position) > 1.6f)
                { Finish("FAIL: saved party retry rollback, health, equipment or shared spawn"); yield break; }
                until = Time.unscaledTime + 10;
                while (net.ConnectedCount > 1 && Time.unscaledTime < until) yield return null;
                if (net.ConnectedCount != 1 || GameSession.Instance.World.CharacterInventory(guest.CharacterId).EquippedArmorId != "training-armor")
                { Finish("FAIL: disconnected participant cleanup or persisted guest equipment; remaining=" + net.ConnectedCount); yield break; }
                Finish("PASS: two-process transport, equipment, coordinated area round trip, client-held revive, party game over and disconnect persistence.");
            }
            else
            {
                while (net.LocalCharacter == null) yield return null;
                var actor = net.LocalCharacter;
                string originalId = actor.CharacterId;
                while (!GameSession.Instance.IsGameOver) yield return null;
                if (GameSession.Instance.ReturnToCheckpoint()) { Finish("FAIL: client initiated authoritative retry"); yield break; }
                while (net.LocalCharacter == actor || GameSession.Instance.Busy) yield return null;
                actor = net.LocalCharacter;
                if (actor.CharacterId != originalId || GameSession.Instance.IsGameOver || GameSession.Instance.World.CharacterVitals(originalId).Health != 6)
                { Finish("FAIL: initial checkpoint client state"); yield break; }
                actor.GetComponent<InventoryInteraction>().enabled = false;
                if (actor.HasStateAuthority) { Finish("FAIL: client claimed state authority"); yield break; }
                net.TestMotion = Vector2.down; yield return new WaitForSecondsRealtime(.25f); net.TestMotion = Vector2.zero;
                while (actor.State.GetSlot(0)?.itemId != "training-armor") yield return null;
                net.TestRequest(new CoopCommand { action = CoopAction.Equip, from = 999, to = (int)EquipmentSlot.Armor });
                yield return new WaitForSecondsRealtime(.3f);
                if (actor.State.EquippedArmorId != null) { Finish("FAIL: invalid equipment command accepted"); yield break; }
                net.TestRequest(new CoopCommand { action = CoopAction.Equip, from = 0, to = (int)EquipmentSlot.Armor });
                while (actor.State.EquippedArmorId != "training-armor") yield return null;
                TreasureChest rewardChest = null;
                while (rewardChest == null)
                {
                    foreach (var candidate in FindObjectsByType<TreasureChest>(FindObjectsSortMode.None))
                        if (!candidate.Opened && candidate.CanUse(actor)) rewardChest = candidate;
                    yield return null;
                }
                net.TestRequest(new CoopCommand { action = CoopAction.Chest, target = rewardChest.ChestId });
                while (!GameSession.Instance.IsReceivingReward(actor)) yield return null;
                yield return new WaitForSecondsRealtime(.2f);
                if (!rewardChest.HasRewardVisual || Time.timeScale != 1 || !GameSession.Instance.World.IsChestItemDiscovered("training-armor"))
                { Finish("FAIL: client reward icon/text or shared discovery"); yield break; }
                net.TestMotion = Vector2.right;
                yield return new WaitForSecondsRealtime(.6f); net.TestMotion = Vector2.zero;
                net.TestRequest(new CoopCommand { action = CoopAction.ConfirmReward, target = rewardChest.ChestId });
                while (GameSession.Instance.IsReceivingReward(actor)) yield return null;
                string characterId = actor.CharacterId;
                string firstScene = actor.gameObject.scene.path;
                for (int trip = 0; trip < 3; trip++)
                {
                    while (net.LocalCharacter == actor || net.ChangingArea || GameSession.Instance.Busy) yield return null;
                    actor = net.LocalCharacter;
                    actor.GetComponent<InventoryInteraction>().enabled = false;
                    if (actor.CharacterId != characterId || actor.HasStateAuthority || actor.State.EquippedArmorId != "training-armor"
                        || (trip == 0 ? actor.gameObject.scene.path == firstScene : actor.gameObject.scene.path != firstScene)
                        || !GameSession.Instance.World.IsCollected("coop-test:chest") || !GameSession.Instance.World.Puzzle("coop-test:runes").IsComplete)
                    { Finish("FAIL: client identity, scene or equipment after area change"); yield break; }
                }
                CharacterInventory target = null;
                while (target == null)
                {
                    foreach (var member in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None))
                        if (member != actor && GameSession.Instance.World.CharacterVitals(member.CharacterId).IsDown) target = member;
                    yield return null;
                }
                while (target.GetComponent<CharacterDeath>().Phase != CharacterLifePhase.Downed) yield return null;
                if (GameSession.Instance.IsGameOver) { Finish("FAIL: client premature game over"); yield break; }
                net.SetReviveIntent(actor, target.CharacterId);
                while (GameSession.Instance.World.CharacterVitals(target.CharacterId).IsDown) yield return null;
                net.SetReviveIntent(actor, null);
                while (!GameSession.Instance.IsGameOver) yield return null;
                while (net.LocalCharacter == actor || GameSession.Instance.Busy) yield return null;
                actor = net.LocalCharacter;
                if (actor.CharacterId != originalId || GameSession.Instance.IsGameOver || actor.State.EquippedArmorId != "training-armor"
                    || GameSession.Instance.World.CharacterVitals(originalId).IsDown || GameSession.Instance.World.IsCollected("coop-test:after-death"))
                { Finish("FAIL: saved checkpoint client recovery"); yield break; }
                Finish("PASS: client ownership, initial checkpoint retry, chest, equipment, revive and saved party retry observed in second process.");
            }
        }
    }
}
