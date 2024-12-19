using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Cysharp.Threading.Tasks.Triggers;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using Sequence = DG.Tweening.Sequence;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Readymade.Machinery.Acting
{
    public class ActorInventoryPresenter : MonoBehaviour
    {
        [InfoBox("This component is a simple inventory presenter that displays the contents of the inventory and " +
            "equipment components.")]
        [BoxGroup("Input")]
        [SerializeField]
        private Actor defaultActor;

        [BoxGroup("Inventory")]
        [SerializeField]
        [Required]
        private InventoryItemDisplay itemDisplayPrefab;

        [FormerlySerializedAs("panel")]
        [BoxGroup("Inventory")]
        [SerializeField]
        [Required]
        private InventoryDisplay display;

        [BoxGroup("Inventory")]
        [SerializeField]
        private Sprite defaultPicture;

        [BoxGroup("Item Drop")]
        [SerializeField]
        private LayerMask dropCrateMask;

        [BoxGroup("Item Drop")]
        [SerializeField]
        private InventoryDropCrate dropCratePrefab;

        [FormerlySerializedAs("bumpDisplays")]
        [ListDrawerSettings(ShowFoldout = true, ShowPaging = false)]
        [BoxGroup("Bumpers")]
        [SerializeField]
        private BumperDisplay[] bumperDisplays;

        [BoxGroup("Bumpers")] [SerializeField] private float bumpDuration = 2;
        [BoxGroup("Bumpers")] [SerializeField] private float bumpAmplitude = 48;
        [BoxGroup("Bumpers")] [SerializeField] private float bumpSize = 48;
        [BoxGroup("Bumpers")] [SerializeField] private float bumpStride = 48;
        [BoxGroup("Bumpers")] [SerializeField] private Axis bumpAxis = Axis.Y;
        [BoxGroup("Bumpers")] [SerializeField] private Ease bumpEase = Ease.OutBounce;

        private IActor _actor;
        private readonly Dictionary<SoProp, InventoryItemDisplay> _itemDisplays = new();
        private readonly Dictionary<SoSlot, InventoryItemDisplay> _slotDisplays = new();
        private Sequence[] _bumpSequences;
        private UniTask[] _bumperTasks;
        private float[] _bumperTimeouts;

        private Tween[] _bumperPunches;

        //private int _bumperIndex;
        private SoProp _selected;
        private IDisposable _bag;
        private float _bumperStrideY;
        private float _bumperStrideX;

        private void Awake()
        {
            _bumpSequences = new Sequence[bumperDisplays.Length];
            _bumperTasks = new UniTask[bumperDisplays.Length];
            _bumperTimeouts = new float[bumperDisplays.Length];
            _bumperPunches = new Tween[bumperDisplays.Length];
        }

        private void Start()
        {
            for (var i = 0; i < bumperDisplays.Length; i++)
            {
                bumperDisplays[i].gameObject.SetActive(false);
                _bumperStrideY = bumperDisplays.Sum(it => it.animationTarget.sizeDelta.y) / bumperDisplays.Length;
                _bumperStrideX = bumperDisplays.Sum(it => it.animationTarget.sizeDelta.x) / bumperDisplays.Length;
            }

            if (_actor == null && defaultActor)
            {
                _actor = defaultActor;
                OpenFor(_actor);
            }

            if (display.canvas)
            {
                Debug.Log($"[{nameof(ActorInventoryPresenter)}] Subscribing to canvas {display.canvas.name} changes.");
                display.canvas.Changed += CanvasChangedHandler;
            }
        }

        private void OnDestroy()
        {
            if (display.canvas)
            {
                display.canvas.Changed -= CanvasChangedHandler;
            }
        }

        private void HidePanel() => display.canvas.SignalEnabled(false);

        private void ShowPanel() => display.canvas.SignalEnabled(true);

        private void CanvasChangedHandler(bool isOn)
        {
            Debug.Log("canvas changed");
            if (isOn && _actor != null)
            {
                RebuildPanel(_actor);
            }
        }

        public void OpenFor([NotNull] IActor actor)
        {
            Close();
            _bag = DisposableBag.Create();
            _actor = actor;
            _actor.Inventory.Modified += InventoryChangedHandler;
            _actor.Equipment.Changed += EquipmentChangedHandler;
            _bag = DisposableBag.Create(
                display.background.OnClickAsAsyncEnumerable().Subscribe(BackgroundClickHandler),
                display.detailConsumeAction?.OnClickAsAsyncEnumerable().Subscribe(SelectedConsumeHandler),
                display.detailEquipAction?.OnClickAsAsyncEnumerable().Subscribe(SelectedEquipHandler),
                display.detailUnequipAction?.OnClickAsAsyncEnumerable().Subscribe(SelectedUnEquipHandler),
                display.detailDropAction?.OnClickAsAsyncEnumerable().Subscribe(SelectedDropHandler)
            );

            RebuildPanel(_actor);
        }

        public void Close()
        {
            _actor.Inventory.Modified -= InventoryChangedHandler;
            _actor.Equipment.Changed -= EquipmentChangedHandler;
            _bag?.Dispose();
            _actor = default;
        }

        private void RebuildPanel([NotNull] IActor actor)
        {
            display.userName?.SetText(actor.Name);
            RebuildInventoryItems(actor);
            RebuildEquippedItems(actor);
            ResetBumps();
        }

        private void BackgroundClickHandler(AsyncUnit unit) => ClearSelected();

        private void SelectedDropHandler(AsyncUnit unit)
        {
            if (_selected)
            {
                _actor.Inventory.TryTake(_selected, 1, out int handle);
                _actor.Inventory.Commit(handle);

                // find existing crate to use.
                InventoryDropCrate dropCrate = Physics
                    .OverlapSphere(_actor.Pose.position, 1f, dropCrateMask, QueryTriggerInteraction.Collide)
                    .Where(it => it.TryGetComponent<InventoryDropCrate>(out _))
                    .Select(it => it.GetComponent<InventoryDropCrate>())
                    .FirstOrDefault();

                // spawn crate if none found.
                if (!dropCrate && dropCratePrefab)
                {
                    dropCrate = Instantiate(
                        dropCratePrefab,
                        _actor.Pose.position, Quaternion.AngleAxis(UnityEngine.Random.Range(-180f, 180f), transform.up)
                    );
                }

                // put prop into crate
                if (dropCrate)
                {
                    Debug.Assert(dropCrate.Inventory != null, "Drop crate has no inventory component.", dropCrate);
                    dropCrate.Inventory.ForcePut(_selected, 1);
                }
                else
                {
                    Debug.LogWarning($"Failed to drop {_selected.DisplayName}.", this);
                }
            }
        }

        private void SelectedUnEquipHandler(AsyncUnit obj)
        {
            if (_selected && TryUnEquip(_selected, _actor.Equipment))
            {
                // report here
            }
        }

        private void SelectedEquipHandler(AsyncUnit obj)
        {
            if (_selected && TryEquip(_selected, out _, _actor.Equipment))
            {
                // report here
            }
        }

        private void SelectedConsumeHandler(AsyncUnit obj)
        {
            if (_selected)
            {
                TryConsume(_selected, _actor);
                // report here
            }
        }

        private bool TryConsume(SoProp selected, IActor actor) => _actor.Consumer.TryConsume(selected, actor);

        private void ResetBumps()
        {
            for (var i = 0; i < bumperDisplays.Length; i++)
            {
                if (!(_bumpSequences[i]?.IsPlaying() ?? false))
                {
                    bumperDisplays[i].gameObject.SetActive(false);
                }
            }
        }

        private void RebuildInventoryItems([NotNull] IActor actor)
        {
            for (int i = display.userItems.childCount - 1; i >= 0; i--)
            {
                Destroy(display.userItems.GetChild(i).gameObject);
            }

            _itemDisplays.Clear();

            foreach ((SoProp Prop, long Count) it in actor.Inventory.Unclaimed)
            {
                if (it.Count > 0)
                {
                    _itemDisplays.Add(it.Prop, CreateItemDisplay(it.Prop, actor, display.userItems));
                }
            }
        }

        private void RebuildEquippedItems([NotNull] IActor actor)
        {
            for (int i = display.otherItems.childCount - 1; i >= 0; i--)
            {
                Destroy(display.otherItems.GetChild(i).gameObject);
            }

            _slotDisplays.Clear();


            foreach (var slot in actor.Equipment.Slots)
            {
                if (actor.Equipment.IsSet(slot, out SoProp prop))
                {
                    if (!_slotDisplays.ContainsKey(slot))
                    {
                        _slotDisplays.Add(slot, CreateItemDisplay(prop, actor, display.otherItems));
                    }
                }
            }
        }

        private InventoryItemDisplay CreateItemDisplay([NotNull] SoProp prop, [NotNull] IActor actor,
            [NotNull] Transform container)
        {
            InventoryItemDisplay itemDisplay = Instantiate(itemDisplayPrefab, container);

            IDisposable bag = DisposableBag.Create(
                // enter -> update details
                itemDisplay.group.GetAsyncPointerEnterTrigger()
                    .AsUniTaskAsyncEnumerable()
                    .Subscribe(_ => UpdateDetailsPanel(actor, prop)),
                // exit -> clear details
                itemDisplay.group.GetAsyncPointerExitTrigger()
                    .AsUniTaskAsyncEnumerable()
                    .Subscribe(_ => FallbackToSelectedDetails()),
                // RMB down -> toggle equip
                itemDisplay.button.GetAsyncPointerDownTrigger()
                    .Subscribe(async eventData => await ItemDownHandlerAsync(eventData, prop, actor)),
                // LMB up -> click
                itemDisplay.button.GetAsyncPointerUpTrigger()
                    .Subscribe(async eventData => await ItemUpHandlerAsync(eventData, actor, prop))
            );


            UpdateItemDisplay(prop, itemDisplay, actor.Inventory, actor.Equipment, actor.Consumer);

            UniTask.Void(async args =>
            {
                await args.display.OnDestroyAsync();
                args.bag.Dispose();
            }, (display: itemDisplay, bag));

            return itemDisplay;

            async UniTask ItemDownHandlerAsync([NotNull] PointerEventData ctx, [NotNull] SoProp f_prop,
                [NotNull] IActor f_actor)
            {
                switch (ctx.button)
                {
                    case PointerEventData.InputButton.Left:
                        break;
                    case PointerEventData.InputButton.Right:
                        bool canUse = f_actor.Equipment.TryGetAnySlot(f_prop, out _);
                        bool canConsume = f_actor.Consumer.CanConsume(f_prop);
                        Debug.Assert(!(canUse && canConsume), "Prop cannot be both used and consumed.", f_prop);
                        if (canUse)
                        {
                            // toggle equip
                            if (f_actor.Equipment.IsSet(f_prop, out _))
                            {
                                TryUnEquip(f_prop, f_actor.Equipment);
                            }
                            else
                            {
                                TryEquip(f_prop, out _, f_actor.Equipment);
                            }
                        }
                        else if (canConsume)
                        {
                            TryConsume(f_prop, f_actor);
                        }

                        break;
                    case PointerEventData.InputButton.Middle:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                await UniTask.CompletedTask;
            }

            async UniTask ItemUpHandlerAsync([NotNull] PointerEventData ctx, [NotNull] IActor f_actor,
                [NotNull] SoProp f_prop)
            {
                switch (ctx.button)
                {
                    case PointerEventData.InputButton.Left:
                    case PointerEventData.InputButton.Right:
                        SetSelected(f_actor, f_prop);
                        break;
                    case PointerEventData.InputButton.Middle:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }


                await UniTask.CompletedTask;
            }
        }

        private bool TryEquip([NotNull] SoProp prop, [NotNull] out SoSlot slot,
            [NotNull] IEquipment<SoSlot, SoProp> equipment)
        {
            if (!equipment.IsSet(prop, out _) &&
                (
                    equipment.TryGetEmptySlot(prop, out SoSlot anySlot) ||
                    equipment.TryGetAnySlot(prop, out anySlot
                    )
                )
            )
            {
                equipment.AddOrSet(anySlot, prop);
                slot = anySlot;
                return true;
            }

            slot = default;
            return false;
        }

        private bool TryUnEquip([NotNull] SoProp prop, [NotNull] IEquipment<SoSlot, SoProp> equipment) =>
            equipment.UnSet(prop);

        private void UpdateItemDisplay([NotNull] SoProp prop, [NotNull] InventoryItemDisplay itemDisplay,
            [NotNull] IInventory<SoProp> inventory,
            IEquipment<SoSlot, SoProp> equipment, IPropConsumer consumer)
        {
            if (!itemDisplay)
            {
                return;
            }

            itemDisplay.count.SetText("{0}", inventory?.GetAvailableCount(prop) ?? 0);
            itemDisplay.label.SetText(prop.DisplayName);
            bool inUse = equipment?.IsSet(prop, out _) ?? false;
            bool canUse = equipment?.TryGetAnySlot(prop, out _) ?? false;
            bool canConsume = consumer?.CanConsume(prop) ?? false;
            itemDisplay.inUse?.SetActive(inUse);
            itemDisplay.canUse?.SetActive(canUse);
            itemDisplay.canConsume?.SetActive(canConsume);
            itemDisplay.iconGraphic.sprite = prop.IconSprite;
            itemDisplay.iconSymbol.symbol = prop.IconSymbol;
            itemDisplay.group.transform.DOKill(true);
            itemDisplay.group.transform.DOPunchScale(Vector3.one * -0.125f, 0.35f, 1, 0.5f);
            itemDisplay.marker?.gameObject.SetActive(prop == _selected);
        }

        private void SetSelected([NotNull] IActor actor, [NotNull] SoProp prop)
        {
            if (prop)
            {
                ClearSelected();

                _selected = prop;
                UpdateDetailsPanel(actor, _selected);
                display.detailPanel.interactable = true;
                display.detailPanel.blocksRaycasts = true;

                // mark new selection
                if (_itemDisplays.TryGetValue(_selected, out InventoryItemDisplay inItemDisplay))
                {
                    inItemDisplay.marker?.gameObject.SetActive(true);
                }

                if (_selected && _actor.Equipment.IsSet(_selected, out var inSlot) &&
                    _slotDisplays.TryGetValue(inSlot, out var inSlotDisplay))
                {
                    inSlotDisplay.marker?.gameObject.SetActive(true);
                }
            }
        }

        private void ClearSelected()
        {
            // clear old selection marker
            if (_selected && _actor.Equipment.IsSet(_selected, out var exSlot) &&
                _slotDisplays.TryGetValue(exSlot, out var exSlotDisplay))
            {
                exSlotDisplay.marker?.gameObject.SetActive(false);
            }

            if (_selected && _itemDisplays.TryGetValue(_selected, out InventoryItemDisplay exItemDisplay))
            {
                exItemDisplay.marker?.gameObject.SetActive(false);
            }

            _selected = null;
            FallbackToSelectedDetails();
        }

        private void FallbackToSelectedDetails()
        {
            if (_selected)
            {
                UpdateDetailsPanel(_actor, _selected);
            }
            else
            {
                display.detailPanel.alpha = 0;
                display.detailPanel.interactable = false;
                display.detailPanel.blocksRaycasts = false;
            }
        }

        private void UpdateDetailsPanel([NotNull] IActor actor, [NotNull] SoProp prop)
        {
            IInventory<SoProp> owner = actor.Inventory;
            if (display.detailPanel)
            {
                display.detailPanel.alpha = 1;
            }

            if (display.detailPicture)
            {
                display.detailPicture.sprite = prop.Picture ?? defaultPicture;
                display.detailPicture.enabled = !display.livePreviewRenderer || !prop.ModelPrefab;
            }

            if (display.livePreviewRenderer && prop.ModelPrefab)
            {
                display.livePreviewRenderer.Set(prop.ModelPrefab);
            }

            if (display.detailIconSymbol)
            {
                display.detailIconSymbol.symbol = prop.IconSymbol;
            }

            if (display.detailIconSprite)
            {
                display.detailIconSprite.sprite = prop.IconSprite;
            }

            display.detailDescription?.SetText(prop.Description);
            display.detailTitle?.SetText(prop.DisplayName);
            display.detailBulk?.SetText("{0}", prop.Bulk);
            display.detailCount?.SetText("{0}", actor.Inventory.GetAvailableCount(prop));
            display.detailOwner?.SetText(owner != null ? owner.DisplayName : "n/a");

            bool inUse = actor.Equipment.IsSet(prop, out var inUseSlot);
            bool canUse = actor.Equipment.TryGetAnySlot(prop, out _);
            bool canConsume = actor.Consumer.CanConsume(prop);
            display.detailInUse?.SetActive(inUse);
            display.detailCanUse?.SetActive(!inUse && canUse);
            display.detailCanConsume?.SetActive(canConsume);
            display.detailInUseInfo?.SetText(inUse ? inUseSlot.displayName : "n/a");
            display.detailCanUseInfo?.SetText(canUse
                ? string.Join(", ", actor.Equipment.GetAllSlots(prop).Select(it => it.displayName))
                : "n/a");
            display.detailCanConsumeInfo?.SetText("No info available.");
            display.detailCanConsumeInfo?.SetText(canConsume && actor.Consumer.TryGetEffect(prop, out SoEffect effect)
                ? effect.Description
                : "No info available.");
            display.detailConsumeAction?.gameObject.SetActive(canConsume);
            display.detailEquipAction?.gameObject.SetActive(!inUse && canUse);
            display.detailUnequipAction?.gameObject.SetActive(inUse);
            display.detailDropAction?.gameObject.SetActive(true);
        }

        // adapter forwarding the current actor to the equipment change handler.
        private void EquipmentChangedHandler((
            IEquipment<SoSlot, SoProp> Equipment,
            SoSlot Slot,
            SoProp OldProp,
            SoProp NewProp
            ) args) => EquipmentChangedHandler(_actor, args);

        private void EquipmentChangedHandler(
            [NotNull] IActor actor,
            (IEquipment<SoSlot, SoProp> Equipment, SoSlot Slot, SoProp OldProp, SoProp NewProp) args)
        {
            if (!display.isActiveAndEnabled)
            {
                return;
            }

            if (args.OldProp && _itemDisplays.TryGetValue(args.OldProp, out InventoryItemDisplay oldPropDisplay))
            {
                UpdateItemDisplay(args.OldProp, oldPropDisplay, actor.Inventory, actor.Equipment, actor.Consumer);
            }

            if (args.NewProp && _itemDisplays.TryGetValue(args.NewProp, out InventoryItemDisplay newPropDisplay))
            {
                UpdateItemDisplay(args.NewProp, newPropDisplay, actor.Inventory, actor.Equipment, actor.Consumer);
            }

            if (_selected && _selected == args.OldProp)
            {
                UpdateDetailsPanel(actor, args.OldProp);
            }

            if (_selected && _selected == args.NewProp)
            {
                UpdateDetailsPanel(actor, args.NewProp);
            }

            if (!_selected)
            {
                FallbackToSelectedDetails();
            }

            RebuildEquippedItems(actor);
        }

        private void InventoryChangedHandler(Phase message, IInventory<SoProp>.InventoryEventArgs args)
        {
            if (!this.display.isActiveAndEnabled)
            {
                return;
            }

            // PANEL

            this.display.userAvailableCapacity?.SetText("{0}", args.Inventory.AvailableCapacity);
            this.display.userStoredBulk?.SetText("{0}", args.Inventory.StoredBulk);
            this.display.userTotalCapacity?.SetText("{0}", args.Inventory.TotalCapacity);

            // DETAILS & SELECTION

            if (args.Available == 0 && _selected == args.Identity)
            {
                ClearSelected();
            }

            if (_selected == args.Identity)
            {
                UpdateDetailsPanel(_actor, args.Identity);
            }

            // ITEM DISPLAY
            // Note: this will also query the selection state so if selection changes, it has to be handled first.

            InventoryItemDisplay display;
            if (args.Available > 0)
            {
                if (!_itemDisplays.TryGetValue(args.Identity, out display))
                {
                    display = CreateItemDisplay(args.Identity, _actor, this.display.userItems);
                    _itemDisplays.Add(args.Identity, display);
                }
                else
                {
                    UpdateItemDisplay(args.Identity, display, args.Inventory, _actor.Equipment, _actor.Consumer);
                }
            }
            else
            {
                if (_itemDisplays.TryGetValue(args.Identity, out display))
                {
                    if (display != null && display.gameObject)
                    {
                        Destroy(display.gameObject);
                    }
                }
            }

            // BUMP

            var direction = args.Delta >= 0 ? 1f : -1f;
            var delta = args.Delta;

            if (delta != 0)
            {
                int stackedBumper = -1;
                for (int i = 0; i < bumperDisplays.Length; i++)
                {
                    if (bumperDisplays[i].ID?.Equals(args.Identity) ?? false)
                    {
                        stackedBumper = i;
                        break;
                    }
                }

                int newBumper = -1;
                for (int i = 0; i < _bumperTasks.Length; i++)
                {
                    if (_bumperTasks[i].Status != UniTaskStatus.Pending)
                    {
                        newBumper = i;
                        break;
                    }
                }


                // reuse display for already stacked props or add a new bumper for new props
                if (stackedBumper >= 0)
                {
                    bumperDisplays[stackedBumper].graphic.sprite = args.Identity.IconSprite;
                    bumperDisplays[stackedBumper].symbol.symbol = args.Identity.IconSymbol;
                    bumperDisplays[stackedBumper].Accumulator += (int)delta;
                    bumperDisplays[stackedBumper].text
                        .SetText($"{bumperDisplays[stackedBumper].Accumulator} {args.Identity.DisplayName}");
                    bumperDisplays[stackedBumper].animationTarget.gameObject.SetActive(true);
                    _bumperPunches[stackedBumper]?.Kill(true);
                    _bumperPunches[stackedBumper] = bumperDisplays[stackedBumper].animationTarget
                        .DOPunchScale(Vector3.one * 0.2f, .2f, 1);
                    _bumperTimeouts[stackedBumper] = Time.time + bumpDuration;
                }
                else
                {
                    _bumperTimeouts[newBumper] = Time.time + bumpDuration;
                    _bumperTasks[newBumper] = SequenceAsync(destroyCancellationToken);

                    async UniTask SequenceAsync(CancellationToken ct)
                    {
                        try
                        {
                            bumperDisplays[newBumper].ID = args.Identity;
                            bumperDisplays[newBumper].Accumulator = (int)delta;
                            bumperDisplays[newBumper].graphic.sprite = args.Identity.IconSprite;
                            bumperDisplays[newBumper].symbol.symbol = args.Identity.IconSymbol;
                            bumperDisplays[newBumper].text
                                .SetText($"{bumperDisplays[newBumper].Accumulator} {args.Identity.DisplayName}");
                            bumperDisplays[newBumper].animationTarget.gameObject.SetActive(true);
                            bumperDisplays[newBumper].group.alpha = 0;
                            bumperDisplays[newBumper].group.DOFade(1f, Beat);
                            bumperDisplays[newBumper].animationTarget.anchoredPosition = bumpAxis switch
                            {
                                Axis.X => Vector3.up * bumpStride * newBumper,
                                Axis.Y => Vector3.right * bumpStride * newBumper,
                                _ => throw new ArgumentOutOfRangeException()
                            };
                            bumperDisplays[newBumper].animationTarget
                                .DOAnchorPos3D(bumpAxis switch
                                {
                                    Axis.X => bumpAmplitude * Vector3.right + Vector3.up * bumpStride * newBumper,
                                    Axis.Y => bumpAmplitude * Vector3.up + Vector3.right * bumpStride * newBumper,
                                    _ => throw new ArgumentOutOfRangeException()
                                }, bumpDuration / 2)
                                .From()
                                .SetEase(bumpEase);

                            await UniTask.WaitUntil(() => _bumperTimeouts[newBumper] < Time.time,
                                cancellationToken: ct);

                            await bumperDisplays[newBumper].group
                                .DOFade(0f, Beat)
                                .AsyncWaitForCompletion();
                        }
                        finally
                        {
                            bumperDisplays[newBumper].ID = default;
                            bumperDisplays[newBumper].Accumulator = 0;
                            bumperDisplays[newBumper].animationTarget.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        public enum Axis
        {
            X,
            Y
        }

        private const float Beat = .35f;
    }
}