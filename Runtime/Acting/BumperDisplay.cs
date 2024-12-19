using com.convalise.UnityMaterialSymbols;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Readymade.Machinery.Acting
{
    public class BumperDisplay : MonoBehaviour
    {
        [SerializeField] public Image graphic;
        [SerializeField] public MaterialSymbol symbol;
        [SerializeField] public TMP_Text text;
        [SerializeField] public RectTransform animationTarget;
        [SerializeField] public CanvasGroup group;
        public object ID;
        public int Accumulator;
    }
}