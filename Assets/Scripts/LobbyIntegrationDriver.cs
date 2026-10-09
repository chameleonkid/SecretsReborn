using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SecretsReborn
{
    // Opt-in development test; uses no user save files or character profiles.
    public sealed class LobbyIntegrationDriver : MonoBehaviour
    {
        private bool finished, host;
        private string report;
        private float deadline;
        private void Awake()
        {
            var args = Environment.GetCommandLineArgs(); host = Array.IndexOf(args, "--lobby-test-host") >= 0;
            int index = Array.IndexOf(args, "--lobby-test-report"); report = index >= 0 ? args[index + 1] : null;
            deadline = Time.unscaledTime + 75;
        }
        private void Update() { if (!finished && Time.unscaledTime > deadline) Finish("FAIL: lobby timeout: " + NetworkCoop.Active.Status); }
        private void Finish(string result)
        { if (finished) return; finished = true; Debug.Log(result); if (report != null) File.WriteAllText(report, result); Application.Quit(result.StartsWith("PASS") ? 0 : 1); }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            var looks = Resources.LoadAll<ClothingAppearance>("CharacterLooks");
            if (looks.Length != 78) { Finish("FAIL: incomplete RetroPixel look library"); yield break; }
            foreach (var look in looks)
                for (int frame = 0; frame < 128; frame++)
                {
                    var sprite = look.Frame(frame);
                    if (sprite == null || sprite.name != "Frame_" + frame.ToString("D3")
                        || sprite.rect != new Rect(frame%16*32,224-frame/16*32,32,32)
                        || sprite.pivot != new Vector2(16,0) || sprite.texture.filterMode != FilterMode.Point)
                    { Finish("FAIL: pixel-art frame alignment/filter " + look.AppearanceId); yield break; }
                }
            var net = NetworkCoop.Active;
            if (!net.OpenLobby(host, "127.0.0.1", host ? "Testhost" : "Testgast")) { Finish("FAIL: open lobby"); yield break; }
            if (host)
            {
                var world = GameSession.Instance.World;
                for (int i = 0; i < 3; i++) world.CreateWorldCharacter("Figur " + (i + 1));
                string removable = world.CreateWorldCharacter("Löschtest");
                if (net.DeleteLobbyCharacter(world.FounderCharacterId) || !net.DeleteLobbyCharacter(removable))
                { Finish("FAIL: founder protection or host deletion"); yield break; }
                string path = Path.Combine(Application.temporaryCachePath, "lobby-check-" + Guid.NewGuid().ToString("N") + ".es3");
                SaveGameStore.Save(world, path); var restored = SaveGameStore.Load(path); ES3.DeleteFile(path);
                if (restored.CharacterSlots.Length != 3 || restored.CharacterSlots[1].name != "Figur 2" || !restored.Multiplayer)
                { Finish("FAIL: persistent character roster"); yield break; }
                net.ChooseLobbyCharacter(world.CharacterSlots[0].id, ready: true);
                while (net.Lobby.players.Length != 2 || !net.Lobby.players[1].ready) yield return null;
                string guestId = net.Lobby.players[1].character;
                SaveGameStore.Save(world,path); var styledRestore = SaveGameStore.Load(path); ES3.DeleteFile(path);
                if (styledRestore.CharacterProfile(guestId).hairStyle != CharacterCustomization.Hair(true,12))
                { Finish("FAIL: Easy Save character layer roundtrip"); yield break; }
                if (net.DeleteLobbyCharacter(net.Lobby.players[1].character)) { Finish("FAIL: selected guest character deleted"); yield break; }
                if (net.Lobby.players[1].name != "Testgast" || net.Lobby.players[0].character == net.Lobby.players[1].character || !net.StartLobbyAdventure())
                { Finish("FAIL: roster ownership/start"); yield break; }
                while (net.ChangingArea) yield return null;
                if (net.ConnectedCount != 2 || net.LocalCharacter == null || GameSession.Instance.World.CharacterSlots.Length != 4)
                { Finish("FAIL: scene binding or absent characters"); yield break; }
                while (net.ConnectedCount != 1) yield return null;
                if (GameSession.Instance.World.CharacterSlots.Length != 4) { Finish("FAIL: disconnect deleted character"); yield break; }
                Finish("PASS: lobby names, four persistent slots, Easy Save RetroPixel layer roundtrip, exclusive selection, ready start, two active actors and preserved absent/disconnected characters.");
            }
            else
            {
                while (net.Lobby.characters.Length != 3 || net.Lobby.players.Length != 2 || string.IsNullOrEmpty(net.Lobby.players[0].character)) yield return null;
                string occupied = net.Lobby.players[0].character;
                if (net.DeleteLobbyCharacter(net.Lobby.characters[1].id)) { Finish("FAIL: client deleted a character"); yield break; }
                net.ChooseLobbyCharacter(occupied, ready: true);
                yield return new WaitForSecondsRealtime(.7f);
                foreach (var p in net.Lobby.players) if (p.name == "Testgast" && !string.IsNullOrEmpty(p.character))
                { Finish("FAIL: occupied slot accepted"); yield break; }
                net.ChooseLobbyCharacter(null, "Gastfigur", hair: 5, eyes: -1,
                    bodyStyle: CharacterCustomization.Body(true,3), hairStyle: CharacterCustomization.Hair(true,12), eyeStyle: CharacterCustomization.Eyes(true,4));
                while (net.Lobby.characters.Length != 4) yield return null;
                string chosen = net.Lobby.characters[3].id;
                net.ChooseLobbyCharacter(chosen, ready: true);
                while (net.LobbyActive || net.ChangingArea || net.LocalCharacter == null) yield return null;
                if (net.LocalCharacter.CharacterId != chosen || net.LocalCharacter.HasStateAuthority || GameSession.Instance.World.CharacterSlots.Length != 4)
                { Finish("FAIL: selected client character binding"); yield break; }
                yield return new WaitForSecondsRealtime(.5f);
                var profile = GameSession.Instance.World.CharacterProfile(chosen);
                var appearance = net.LocalCharacter.GetComponent<CharacterAppearance>();
                appearance.FrontPreviewLayer(3, out var hairTint);
                if (profile.hairColor != 5 || profile.eyeStyle != CharacterCustomization.Eyes(true,4) || hairTint != CharacterPalette.Hair[5]
                    || !appearance.MaleBody || appearance.FrontPreviewMaterial(2) != null
                    || appearance.FrontPreviewLayer(0,out _) != CharacterLookLibrary.Find(profile.bodyStyle).Frame(0)
                    || appearance.FrontPreviewLayer(3,out _) != CharacterLookLibrary.Find(profile.hairStyle).Frame(0))
                { Finish("FAIL: replicated character appearance"); yield break; }
                Finish("PASS: host lobby received, occupied selection rejected, chosen RetroPixel body, hair and eye layers replicated and roster retained.");
            }
        }
    }
}
