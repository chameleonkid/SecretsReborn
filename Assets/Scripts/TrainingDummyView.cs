using UnityEngine;
namespace SecretsReborn
{
    public sealed class TrainingDummyView : MonoBehaviour
    {
        [SerializeField] private TextMesh label;
        private TreeMeleeEnemy enemy;
        private SpriteRenderer[] parts;
        private int displayedHealth=-1;
        public void Configure(TextMesh text) => label=text;
        private void Awake() { enemy=GetComponent<TreeMeleeEnemy>(); parts=GetComponentsInChildren<SpriteRenderer>(); }
        private void LateUpdate()
        {
            int depth=500-Mathf.RoundToInt(Mathf.Atan(transform.position.y*.04f)*250);
            for(int i=0;i<parts.Length;i++) parts[i].sortingOrder=depth+i;
            if (label!=null && enemy!=null && displayedHealth!=enemy.Health)
            {
                displayedHealth=enemy.Health;
                label.text="TRAININGSPUPPE\n"+displayedHealth.ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("de-DE"))+" / 1.000.000 HP";
            }
        }
    }
}
