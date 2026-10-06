using UnityEditor;
using UnityEngine;

namespace SecretsReborn.Editor
{
    public static class CharacterVitalsTools
    {
        private static CharacterInventory Actor()
        {
            if (!EditorApplication.isPlaying) return null;
            var follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            return follow != null && follow.Target != null ? follow.Target.GetComponent<CharacterInventory>() : null;
        }
        [MenuItem("SecretsReborn/Character/Test/Take 25 damage")]
        private static void Damage() => GameSession.Instance.ApplyDamage(Actor(), 25);
        [MenuItem("SecretsReborn/Character/Test/Heal 25 HP")]
        private static void Heal() => GameSession.Instance.ApplyHealing(Actor(), 25);
        [MenuItem("SecretsReborn/Character/Test/Spend 10 mana")]
        private static void Spend() => GameSession.Instance.TrySpendMana(Actor(), 10);
        [MenuItem("SecretsReborn/Character/Test/Restore 10 mana")]
        private static void Restore() => GameSession.Instance.RestoreMana(Actor(), 10);
        [MenuItem("SecretsReborn/Character/Test/Take 25 damage", true)]
        [MenuItem("SecretsReborn/Character/Test/Heal 25 HP", true)]
        [MenuItem("SecretsReborn/Character/Test/Spend 10 mana", true)]
        [MenuItem("SecretsReborn/Character/Test/Restore 10 mana", true)]
        private static bool Validate() => Actor() != null;
    }
}
