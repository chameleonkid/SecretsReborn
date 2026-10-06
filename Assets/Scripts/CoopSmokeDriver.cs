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
                GameSession.Instance.ApplyDamage(net.LocalCharacter, 999); GameSession.Instance.ApplyDamage(guest, 999);
                yield return new WaitForSecondsRealtime(1.5f);
                if (!GameSession.Instance.IsGameOver) { Finish("FAIL: both-down game over"); yield break; }
                until = Time.unscaledTime + 10;
                while (net.ConnectedCount > 1 && Time.unscaledTime < until) yield return null;
                if (net.ConnectedCount != 1 || GameSession.Instance.World.CharacterInventory(guest.CharacterId).EquippedArmorId != "training-armor")
                { Finish("FAIL: disconnected participant cleanup or persisted guest equipment; remaining=" + net.ConnectedCount); yield break; }
                Finish("PASS: two-process transport, client input with host movement, independent equipment, single death without game over, client-held revive and all-down game over.");
            }
            else
            {
                while (net.LocalCharacter == null) yield return null;
                var actor = net.LocalCharacter;
                actor.GetComponent<InventoryInteraction>().enabled = false;
                if (actor.HasStateAuthority) { Finish("FAIL: client claimed state authority"); yield break; }
                net.TestMotion = Vector2.down; yield return new WaitForSecondsRealtime(.25f); net.TestMotion = Vector2.zero;
                while (actor.State.GetSlot(0)?.itemId != "training-armor") yield return null;
                net.TestRequest(new CoopCommand { action = CoopAction.Equip, from = 999, to = (int)EquipmentSlot.Armor });
                yield return new WaitForSecondsRealtime(.3f);
                if (actor.State.EquippedArmorId != null) { Finish("FAIL: invalid equipment command accepted"); yield break; }
                net.TestRequest(new CoopCommand { action = CoopAction.Equip, from = 0, to = (int)EquipmentSlot.Armor });
                while (actor.State.EquippedArmorId != "training-armor") yield return null;
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
                Finish("PASS: client-owned input, mirrored equipment, death, revive and party game over observed in second process.");
            }
        }
    }
}
