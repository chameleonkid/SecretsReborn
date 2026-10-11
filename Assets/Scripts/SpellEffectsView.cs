using System.Collections.Generic;
using UnityEngine;

namespace SecretsReborn
{
    // Presentation only. These sprites have no collider and never apply gameplay effects.
    public sealed class SpellEffectsView : MonoBehaviour
    {
        private readonly Dictionary<int, SpriteRenderer> visuals = new Dictionary<int, SpriteRenderer>();
        private Sprite[] flying, hit, heal;
        private SpriteRenderer casting;
        private Material material;
        private float castAge;
        private void Awake()
        {
            flying = Resources.LoadAll<Sprite>("Magic/Effects/FireballFlying");
            hit = Resources.LoadAll<Sprite>("Magic/Effects/FireballHit");
            heal = Resources.LoadAll<Sprite>("Magic/Effects/Heal");
            System.Array.Sort(flying, (a,b) => string.CompareOrdinal(a.name,b.name));
            System.Array.Sort(hit, (a,b) => string.CompareOrdinal(a.name,b.name));
            System.Array.Sort(heal, (a,b) => string.CompareOrdinal(a.name,b.name));
            material = new Material(Shader.Find("Sprites/Default"));
        }
        private SpriteRenderer Create(string name)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer)); obj.transform.SetParent(transform, false);
            var renderer = obj.GetComponent<SpriteRenderer>(); renderer.sharedMaterial = material; renderer.sortingOrder = 1100;
            return renderer;
        }
        private static void Animate(SpriteRenderer renderer, Sprite[] frames, float age, bool loop, float size)
        {
            if (frames.Length == 0) { renderer.enabled = false; return; }
            int frame = loop ? Mathf.FloorToInt(age * 12) % frames.Length : Mathf.Min(frames.Length - 1, Mathf.FloorToInt(age / .8f * frames.Length));
            renderer.sprite = frames[frame]; renderer.enabled = true;
            renderer.transform.localScale = Vector3.one * (size / Mathf.Max(.01f, renderer.sprite.bounds.size.y));
        }
        public void Present(bool isCasting, string spell, SpellEffectSnapshot[] effects)
        {
            if (casting == null) casting = Create("Spell casting aura");
            if (isCasting)
            {
                castAge += Time.deltaTime; casting.transform.position = transform.position + Vector3.up * .35f;
                casting.color = spell == "fireball" ? new Color(1,.45f,.15f,.7f) : new Color(.4f,1,.7f,.7f);
                Animate(casting, heal, castAge, true, 1.25f);
            }
            else { castAge = 0; casting.enabled = false; }
            var present = new HashSet<int>();
            foreach (var effect in effects ?? System.Array.Empty<SpellEffectSnapshot>())
            {
                if (effect == null || !present.Add(effect.id)) continue;
                if (!visuals.TryGetValue(effect.id, out var renderer)) { renderer = Create("Spell " + effect.kind); visuals.Add(effect.id,renderer); }
                renderer.transform.position = new Vector3(effect.x,effect.y,0);
                renderer.transform.rotation = effect.kind == "fireball" ? Quaternion.Euler(0,0,effect.angle) : Quaternion.identity;
                Animate(renderer, effect.kind == "fireball" ? flying : effect.kind == "hit" ? hit : heal, effect.age, effect.kind == "fireball", effect.kind == "fireball" ? .5f : 1.1f);
                renderer.color = effect.kind == "heal" ? new Color(.4f,1,.7f,1) : Color.white;
            }
            var remove = new List<int>(); foreach (var pair in visuals) if (!present.Contains(pair.Key)) remove.Add(pair.Key);
            foreach (int id in remove) { Destroy(visuals[id].gameObject); visuals.Remove(id); }
        }
        public void Clear()
        {
            foreach (var renderer in visuals.Values) if (renderer != null) Destroy(renderer.gameObject);
            visuals.Clear(); if (casting != null) casting.enabled = false;
        }
        private void OnDisable() => Clear();
        private void OnDestroy() { Clear(); if (material != null) Destroy(material); }
    }
}
