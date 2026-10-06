using UnityEditor;
using UnityEngine;

namespace SecretsReborn.Editor
{
    public static class PartyDeathTools
    {
        private static CharacterInventory Companion()
        {
            foreach (var actor in Object.FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None))
                if (actor.CharacterId == "test-companion") return actor;
            return null;
        }
        private static CharacterInventory Local()
        { return Camera.main?.GetComponent<CameraFollow>()?.Target?.GetComponent<CharacterInventory>(); }
        [MenuItem("SecretsReborn/Character/Test/Party/Add living test companion")]
        private static void Add()
        {
            if (!EditorApplication.isPlaying || Companion() != null) return;
            var actor = Local(); if (actor == null) return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/World/Sanctuary/Prefabs/Player.prefab");
            var obj = Object.Instantiate(prefab, actor.transform.position + Vector3.right * 2, Quaternion.identity);
            obj.name = "Test companion (local party test)";
            var companion = obj.GetComponent<CharacterInventory>();
            var serialized = new SerializedObject(companion); serialized.FindProperty("characterId").stringValue = "test-companion"; serialized.ApplyModifiedPropertiesWithoutUndo();
            companion.RefreshSession();
            obj.GetComponent<PlayerMovement>().enabled = false;
            foreach (var component in new MonoBehaviour[] { obj.GetComponent<PlayerMelee>(), obj.GetComponent<InventoryInteraction>() })
            { var settings = new SerializedObject(component); settings.FindProperty("localInput").boolValue = false; settings.ApplyModifiedPropertiesWithoutUndo(); }
            obj.GetComponent<PlayerLantern>().enabled = false;
            GameSession.Instance.RegisterSpawn(companion);
        }
        [MenuItem("SecretsReborn/Character/Test/Party/Down local character")]
        private static void DownLocal() => Down(Local());
        [MenuItem("SecretsReborn/Character/Test/Party/Down test companion")]
        private static void DownCompanion() => Down(Companion());
        private static void Down(CharacterInventory actor)
        {
            if (!EditorApplication.isPlaying || actor == null) return;
            for (int i = 0; i < CharacterVitalsState.MaximumHearts * 2; i++)
                if (!GameSession.Instance.ApplyDamage(actor, 1)) break;
        }
        [MenuItem("SecretsReborn/Character/Test/Party/Revive local character")]
        private static void Revive()
        { if (EditorApplication.isPlaying) GameSession.Instance.ReviveCharacter(Local(), 2); }
        [MenuItem("SecretsReborn/Character/Test/Party/Remove test companion")]
        private static void Remove()
        {
            if (!EditorApplication.isPlaying) return;
            var actor = Companion(); if (actor == null) return;
            GameSession.Instance.RemovePartyMember(actor.CharacterId); Object.Destroy(actor.gameObject);
        }
    }
}
