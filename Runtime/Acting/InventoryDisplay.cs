using com.convalise.UnityMaterialSymbols;
using Sirenix.OdinInspector;
using Readymade.Utils;
using Readymade.Utils.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Readymade.Machinery.Acting
{
    public class InventoryDisplay : MonoBehaviour
    {
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [Required]
        [BoxGroup("Panel")]
        [SerializeField]
        public CanvasToggle canvas;

#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Panel")]
        [Required]
        [SerializeField]
        public Button background;

#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("User")]
        [Required]
        [SerializeField]
        public RectTransform userItems;

#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("User")]
        [Required]
        [SerializeField]
        public CanvasGroup userPanel;

#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("User")]
        [SerializeField]
        [Required]
        public TMP_Text userName;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("User")]
        [SerializeField]
        [Required]
        public Button putAll;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("User")]
        [SerializeField]
        public TMP_Text userAvailableCapacity;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("User")]
        [SerializeField]
        public TMP_Text userTotalCapacity;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("User")]
        [SerializeField]
        public TMP_Text userStoredBulk;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Other")]
        [SerializeField]
        [Required]
        public CanvasGroup otherPanel;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Other")]
        [SerializeField]
        [Required]
        public RectTransform otherItems;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Other")]
        [SerializeField]
        [Required]
        public TMP_Text otherName;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Other")]
        [SerializeField]
        [Required]
        public Button takeAll;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Other")]
        [SerializeField]
        public TMP_Text otherAvailableCapacity;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Other")]
        [SerializeField]
        public TMP_Text otherTotalCapacity;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Other")]
        [SerializeField]
        public TMP_Text otherStoredBulk;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Detail")]
        [SerializeField]
        [Required]
        public CanvasGroup detailPanel;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Detail")]
        [SerializeField]
        [Required]
        public TMP_Text detailTitle;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Detail")]
        [SerializeField]
        [Required]
        public TMP_Text detailOwner;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Detail")]
        [SerializeField]
        [Required]
        public TMP_Text detailDescription;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Detail")]
        [SerializeField]
        [Required]
        public Image detailPicture;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Detail")]
        [SerializeField]
        [Required]
        public Image detailIconSprite;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Detail")]
        [SerializeField]
        [Required]
        public MaterialSymbol detailIconSymbol;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Detail")]
        [SerializeField]
        [Required]
        public RectTransform detailContent;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Detail")]
        [SerializeField]
        [Required]
        public TMP_Text detailBulk;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Detail")]
        [SerializeField]
        [Required]
        public TMP_Text detailCount;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Detail")]
        [SerializeField]
        public TMP_Text detailCanUseInfo;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Detail")]
        [SerializeField]
        public TMP_Text detailInUseInfo;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Detail")]
        [SerializeField]
        public TMP_Text detailCanConsumeInfo;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Detail")]
        [SerializeField]
        public GameObject detailInUse;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [FormerlySerializedAs("detailUsable")]
        [BoxGroup("Detail")]
        [SerializeField]
        public GameObject detailCanUse;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [FormerlySerializedAs("detailConsumable")]
        [BoxGroup("Detail")]
        [SerializeField]
        public GameObject detailCanConsume;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [FormerlySerializedAs("detailUnequip")]
        [BoxGroup("Detail")]
        [SerializeField]
        public Button detailUnequipAction;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [FormerlySerializedAs("detailEquip")]
        [BoxGroup("Detail")]
        [SerializeField]
        public Button detailEquipAction;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [FormerlySerializedAs("detailConsume")]
        [BoxGroup("Detail")]
        [SerializeField]
        public Button detailConsumeAction;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [BoxGroup("Detail")]
        [SerializeField]
        public Button detailDropAction;
#if ODIN_INSPECTOR
        [ChildGameObjectsOnly]
#endif
        [FormerlySerializedAs("previewRenderer")]
        [BoxGroup("Detail")]
        [SerializeField]
        [Required]
        public LivePreviewRender livePreviewRenderer;
        
        [BoxGroup("Audio")]
        [SerializeField]
        [Required]
        public AudioSource audioSource;
    }
}