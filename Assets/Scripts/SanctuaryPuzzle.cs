using UnityEngine;

namespace SecretsReborn
{
    public sealed class SanctuaryPuzzle : MonoBehaviour
    {
        [SerializeField] private PlayerLantern lantern;
        [Tooltip("Die vier Kreise in der Reihenfolge, in der sie betreten werden sollen.")]
        [SerializeField] private RuneCircle[] circles;
        [SerializeField] private SpringSource spring;
        [SerializeField] private GameObject gate;
        [SerializeField] private string puzzleId = "forest-sanctuary-source";
        [SerializeField, Tooltip("Basis-Steuerung nur für eine Tutorial-Szene einblenden.")]
        private bool showTutorialControls = false;
        private RuneSequence sequence;
        private string message = "Finde die vier Runenkreise in der richtigen Reihenfolge.";

        public void Configure(PlayerLantern source, RuneCircle[] orderedCircles,
            SpringSource springSource, GameObject sealedGate)
        {
            lantern = source;
            circles = orderedCircles;
            spring = springSource;
            gate = sealedGate;
        }

        private void Start()
        {
            if (lantern == null || spring == null || gate == null || circles == null
                || circles.Length != 4 || System.Array.Exists(circles, circle => circle == null))
            {
                Debug.LogError("SanctuaryPuzzle: Spieler, Quelle, Durchgang und genau vier Kreise zuweisen.", this);
                enabled = false;
                return;
            }
            RefreshSession();
        }
        public void RefreshSession()
        {
            sequence = GameSession.Instance.World.Puzzle(puzzleId);
            for (int i = 0; i < circles.Length; i++)
            {
                circles[i].Initialize(lantern.transform, i, EnterCircle);
                circles[i].SetActivated(i < sequence.Progress);
            }
            spring.SetRestored(sequence.IsComplete);
            gate.SetActive(!sequence.IsComplete);
            if (sequence.IsComplete) ApplyCompletion();
            else if (sequence.Progress > 0) message = "Runenkreis aktiviert: " + sequence.Progress + "/4";
            else message = "Finde die vier Runenkreise in der richtigen Reihenfolge.";
        }

        private void EnterCircle(int index)
        {
            if (sequence.IsComplete) return;
            bool correct = sequence.Enter(index);
            for (int i = 0; i < circles.Length; i++) circles[i].SetActivated(i < sequence.Progress);
            message = correct ? "Runenkreis aktiviert: " + sequence.Progress + "/4"
                : "Falsche Reihenfolge. Zurueck zum ersten Kreis!";
            if (!sequence.IsComplete) return;
            ApplyCompletion();
        }
        private void ApplyCompletion()
        {
            spring.Restore();
            gate.SetActive(false);
            message = "Die Quelle fliesst wieder. Der Weg im Norden ist offen!";
        }

        private void OnGUI()
        {
            if (!showTutorialControls || lantern == null) return;
            GUI.Box(new Rect(12, 12, 460, 110), "Waldheiligtum\nWASD / Pfeiltasten / Gamepad: Bewegen\n"
                + "L / obere Gamepad-Taste: Laterne " + (lantern.IsLit ? "AN" : "AUS")
                + "\nMit der Laterne nahe Zeichen entdecken.\n" + message);
        }
    }
}
