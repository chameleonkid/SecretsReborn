using UnityEngine;

namespace SecretsReborn
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TreasureChest : MonoBehaviour
    {
        [SerializeField] private string chestId;
        [SerializeField] private LootTable loot;
        [SerializeField] private Sprite[] openingFrames;
        [SerializeField, Min(.1f)] private float rewardDisplaySeconds = 1.2f;
        private GameSession presentationSession;
        private GameObject rewardVisual;
        private Sprite rewardSprite;
        private Material rewardMaterial;
        private string rewardLabel;
        public void ConfigureVisual(Sprite[] frames) { openingFrames = frames; RefreshSession(); }
        public bool Opened => GameSession.Instance.World.IsCollected("chest:" + chestId);
        public string Label => Opened ? "Truhe (geöffnet)" : "Truhe öffnen";
        public void Configure(string id, LootTable table) { chestId = id; loot = table; }
        private void Start() => RefreshSession();
        public void RefreshSession()
        {
            StopAllCoroutines();
            ClearPresentation();
            var renderer = GetComponent<SpriteRenderer>(); renderer.color = Color.white;
            if (openingFrames != null && openingFrames.Length == 4) renderer.sprite = openingFrames[Opened ? 3 : 0];
        }
        private System.Collections.IEnumerator AnimateOpen()
        {
            presentationSession = GameSession.Instance;
            if (!presentationSession.BeginRewardPresentation(this)) { presentationSession = null; yield break; }
            try
            {
                var renderer = GetComponent<SpriteRenderer>();
                if (openingFrames != null && openingFrames.Length == 4)
                    foreach (var sprite in openingFrames) { renderer.sprite = sprite; yield return new WaitForSecondsRealtime(.1f); }
                foreach (var entry in loot.Entries)
                {
                    if (entry == null || entry.item == null) continue;
                    ShowReward(entry.item, entry.count);
                    float elapsed = 0;
                    while (elapsed < rewardDisplaySeconds)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        if (rewardVisual != null) rewardVisual.transform.position = transform.position
                            + Vector3.up * (1.1f + .15f * Mathf.Clamp01(elapsed / .3f));
                        yield return null;
                    }
                    RemoveRewardVisual();
                }
            }
            finally { ClearPresentation(); }
        }
        private void ShowReward(ItemDefinition item, int count)
        {
            rewardLabel = item.DisplayName + (count > 1 ? " × " + count : "");
            if (item.Icon == null) return;
            var full = item.Icon.textureRect; var content = item.IconContent;
            var rect = new Rect(full.x + full.width * content.x, full.y + full.height * content.y,
                full.width * content.width, full.height * content.height);
            rewardSprite = Sprite.Create(item.Icon.texture, rect, new Vector2(.5f, .5f), 32);
            rewardVisual = new GameObject("Chest reward", typeof(SpriteRenderer));
            rewardVisual.transform.SetParent(transform, false);
            var image = rewardVisual.GetComponent<SpriteRenderer>(); image.sprite = rewardSprite; image.color = item.IconTint;
            image.sortingLayerID = GetComponent<SpriteRenderer>().sortingLayerID; image.sortingOrder = 1100;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader != null) { rewardMaterial = new Material(shader); image.sharedMaterial = rewardMaterial; }
            float width = Mathf.Max(rewardSprite.bounds.size.x, rewardSprite.bounds.size.y);
            float parentScale = Mathf.Max(.01f, Mathf.Abs(transform.lossyScale.x));
            rewardVisual.transform.localScale = Vector3.one * (.65f / width / parentScale);
        }
        private void RemoveRewardVisual()
        {
            if (rewardVisual != null) Destroy(rewardVisual);
            if (rewardSprite != null) Destroy(rewardSprite);
            if (rewardMaterial != null) Destroy(rewardMaterial);
            rewardVisual = null; rewardSprite = null; rewardMaterial = null; rewardLabel = null;
        }
        private void ClearPresentation()
        {
            RemoveRewardVisual();
            if (presentationSession != null) presentationSession.EndRewardPresentation(this);
            presentationSession = null;
        }
        private void OnDisable() { StopAllCoroutines(); ClearPresentation(); }
        private void OnGUI()
        {
            if (rewardLabel == null || Camera.main == null) return;
            var point = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.7f);
            var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
            GUI.Box(new Rect(point.x - 150, Screen.height - point.y - 20, 300, 35), rewardLabel, style);
        }
        public bool CanUse(CharacterInventory actor) => !string.IsNullOrWhiteSpace(chestId) && actor != null
            && actor.HasStateAuthority && isActiveAndEnabled && actor.gameObject.scene == gameObject.scene
            && Vector2.Distance(actor.transform.position, transform.position) <= 1.6f;
        public bool TryOpen(CharacterInventory actor)
        {
            if (!CanUse(actor) || Opened || loot == null || loot.Source != LootSourceKind.Chest || !loot.IsValid
                || !GameSession.Instance.CanFight(actor)) return false;
            bool received = GameSession.Instance.World.TryCollect("chest:" + chestId, () => actor.TryReceiveBatch(loot.Rewards()));
            if (received) { RefreshSession(); StartCoroutine(AnimateOpen()); }
            return received;
        }
    }
}
