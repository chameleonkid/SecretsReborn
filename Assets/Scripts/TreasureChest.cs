using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsReborn
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TreasureChest : MonoBehaviour
    {
        [SerializeField] private string chestId;
        [SerializeField] private LootTable loot;
        [SerializeField] private Sprite[] openingFrames;
        private CharacterInventory recipient;
        private float presentationSince;
        private GameObject rewardVisual;
        private Sprite rewardSprite;
        private Material rewardMaterial;
        private string rewardLabel;
        public string ChestId => chestId;
        internal bool HasRewardVisual => rewardVisual != null && rewardLabel != null;
        public void RefreshReplica()
        {
            RefreshPresentation();
        }
        public void ConfigureVisual(Sprite[] frames) { openingFrames = frames; RefreshSession(); }
        public bool Opened => GameSession.Instance.World.IsCollected("chest:" + chestId);
        public string Label => Opened ? "Truhe (geöffnet)" : "Truhe öffnen";
        public void Configure(string id, LootTable table) { chestId = id; loot = table; }
        private void Start() => RefreshSession();
        public void RefreshSession()
        {
            ClearPresentation();
            var renderer = GetComponent<SpriteRenderer>(); renderer.color = Color.white;
            if (openingFrames != null && openingFrames.Length == 4) renderer.sprite = openingFrames[Opened ? 3 : 0];
        }
        private void Update()
        {
            RefreshPresentation();
            if (recipient == null || Time.unscaledTime - presentationSince < .45f || !Application.isFocused) return;
            var key = Keyboard.current; var pad = Gamepad.current;
            if (key?.enterKey.wasPressedThisFrame == true || pad?.buttonSouth.wasPressedThisFrame == true || pad?.buttonWest.wasPressedThisFrame == true)
                GameSession.Instance.ConfirmChestReward(recipient, chestId);
        }
        private void RefreshPresentation()
        {
            var local = NetworkCoop.Running ? NetworkCoop.Active.LocalCharacter
                : Camera.main?.GetComponent<CameraFollow>()?.Target?.GetComponent<CharacterInventory>();
            var reward = GameSession.Instance.RewardFor(local);
            if (reward == null || reward.chestId != chestId)
            {
                if (recipient != null) ClearPresentation();
                if (openingFrames != null && openingFrames.Length == 4) GetComponent<SpriteRenderer>().sprite = openingFrames[Opened ? 3 : 0];
                return;
            }
            if (recipient == null)
            {
                recipient = local; presentationSince = Time.unscaledTime;
                var item = local.Find(reward.itemId); if (item != null) ShowReward(item, 1);
            }
            float elapsed = Time.unscaledTime - presentationSince;
            if (openingFrames != null && openingFrames.Length == 4) GetComponent<SpriteRenderer>().sprite = openingFrames[Mathf.Clamp((int)(elapsed / .1f), 0, 3)];
            if (rewardVisual != null) rewardVisual.transform.position = transform.position + Vector3.up * (1.1f + .15f * Mathf.Clamp01(elapsed / .3f));
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
            recipient = null;
        }
        private void OnDisable()
        {
            ClearPresentation();
            var session = GameSession.Existing;
            if (session != null && !NetworkCoop.IsReplica)
                foreach (var reward in session.CaptureChestRewards()) if (reward.chestId == chestId) session.ReleaseChestReward(reward.characterId);
        }
        private void OnGUI()
        {
            if (rewardLabel == null || Camera.main == null) return;
            var point = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.7f);
            var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
            GUI.Box(new Rect(point.x - 180, Screen.height - point.y - 20, 360, 65), rewardLabel + "\nA / X / Enter: bestätigen", style);
            if (Time.unscaledTime - presentationSince >= .45f && GUI.Button(new Rect(point.x - 60, Screen.height - point.y + 48, 120, 28), "Bestätigen"))
                GameSession.Instance.ConfirmChestReward(recipient, chestId);
        }
        public bool CanUse(CharacterInventory actor) => !string.IsNullOrWhiteSpace(chestId) && actor != null
            && (actor.HasStateAuthority || NetworkCoop.IsReplica && actor.LocalInput) && isActiveAndEnabled && actor.gameObject.scene == gameObject.scene
            && Vector2.Distance(actor.transform.position, transform.position) <= 1.6f;
        public bool TryOpen(CharacterInventory actor)
        {
            if (CanUse(actor) && NetworkCoop.Request(actor, CoopAction.Chest, target: chestId)) return true;
            if (!CanUse(actor) || Opened || loot == null || loot.Source != LootSourceKind.Chest || !loot.IsValid
                || !GameSession.Instance.CanFight(actor)) return false;
            var entries = loot.Entries;
            if (entries.Length != 1 || entries[0].count != 1) { Debug.LogWarning("Truhen benötigen genau einen Gegenstand.", this); return false; }
            bool received = GameSession.Instance.World.TryCollect("chest:" + chestId, () => actor.TryReceiveBatch(loot.Rewards()));
            if (received)
            {
                GameSession.Instance.World.DiscoverChestItem(entries[0].item.ItemId);
                GameSession.Instance.BeginChestReward(this, actor, entries[0].item);
                RefreshSession();
            }
            return received;
        }
    }
}
