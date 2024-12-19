using com.convalise.UnityMaterialSymbols;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Readymade.Machinery.Acting
{
    public class InventoryItemDisplay : MonoBehaviour
    {
        [SerializeField] public TMP_Text label;
        [SerializeField] public TMP_Text count;

        [FormerlySerializedAs("graphicIcon")]
        [FormerlySerializedAs("graphic")]
        [SerializeField]
        public Image iconGraphic;

        [FormerlySerializedAs("symbolIcon")]
        [FormerlySerializedAs("symbol")]
        [SerializeField]
        public MaterialSymbol iconSymbol;

        [SerializeField] public Image marker;
        [SerializeField] public Button button;
        [SerializeField] public CanvasGroup group;
        [SerializeField] public GameObject inUse;
        [SerializeField] public GameObject canUse;
        [SerializeField] public GameObject canConsume;
    }
}