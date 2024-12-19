using System;
using System.Security.Cryptography;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif
using Readymade.Utils.Patterns;
using Readymade.Utils.UI;
using TMPro;
using UnityEngine;
using Image = UnityEngine.UI.Image;

namespace Readymade.Machinery.Acting
{
    public class InventoryFillPresenter : MonoBehaviour
    {
        [BoxGroup("Source")]
        [SerializeField]
        [Tooltip(
            "The destination inventory selection method\n" +
            "<b>[Actor]</b> Use the actor's inventory\n" +
            "<b>[LocalReference]</b> Use a local reference\n" +
            "<b>[ServiceLocator]</b> Get the inventory from the service locator. Will request IInventory<SoProp>.\n" +
            "<b>[PropBroker]</b> Get the inventory from the prop broker.")]
        private InventorySelection selectSource;

        [BoxGroup("Source")]
        [ShowIf(nameof(selectSource), InventorySelection.LocalReference)]
        [SerializeField]
        private InventoryComponent source;


        [BoxGroup("Source")] [SerializeField] private TwoPhaseComponent component = TwoPhaseComponent.Total;
        [BoxGroup("Source")] [SerializeField] private SoProp prop;
        [BoxGroup("Canvas")] [SerializeField] private Image fill;
        [BoxGroup("Canvas")] [SerializeField] private Image icon;

        [SerializeField] private FillDisplay display;


        [BoxGroup("Material")]
        [SerializeField]
        private Renderer renderer;

        [BoxGroup("Material")]
        [SerializeField]
        private string floatProperty;

        [BoxGroup("Material")]
        [SerializeField]
        private int maxCount = 1;

        private Material _material;
        private IInventory<SoProp> _from;
        private float _aggregationTimeout;

        private void Start()
        {
            Debug.Assert(prop != null, "prop!= null", this);
            _from = selectSource switch
            {
                InventorySelection.LocalReference => source,
                InventorySelection.ServiceLocator => Services.Get<IInventory<SoProp>>(),
                InventorySelection.PropBroker => throw new NotImplementedException(),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (renderer)
            {
                _material = renderer.material;
                Debug.Assert(_material != null, "_material != null", this);
            }

            if (display)
            {
                display.Label?.SetText(prop.DisplayName);
                display.MaxValue?.SetText("{0}", maxCount);
                display.MinValue?.SetText("{0}", 0);
                display.CurrentValue?.SetText("{0}", 0);
                display.NextValue?.SetText(string.Empty);
                if (display.Icon != null)
                {
                    display.Icon.sprite = prop.IconSprite;
                }
            }

            if (_from != null)
            {
                _from.Modified += ChangedHandler;
                ChangedHandler(
                    Phase.Set,
                    new IInventory<SoProp>.InventoryEventArgs(
                        inventory: _from,
                        item: prop,
                        delta: 0,
                        claimed: _from.GetClaimedCount(prop),
                        available: _from.GetAvailableCount(prop)
                    ));
            }

            Debug.Assert(_from != null, "_from != null", this);
        }

        private void OnDestroy()
        {
            if (renderer)
            {
                Destroy(_material);
            }

            if (_from != null)
            {
                _from.Modified -= ChangedHandler;
            }
        }

        private void ChangedHandler(Phase message,
            IInventory<SoProp>.InventoryEventArgs args)
        {
            Debug.Assert(prop != null, "prop != null", this);
            if (args.Identity != prop)
            {
                return;
            }

            long x = component switch
            {
                TwoPhaseComponent.Total => args.Claimed + args.Available,
                TwoPhaseComponent.Available => args.Available,
                TwoPhaseComponent.Claimed => args.Claimed,
                _ => throw new ArgumentOutOfRangeException()
            };
            long dx = component switch
            {
                TwoPhaseComponent.Total => message switch
                {
                    Phase.Set => args.Delta,
                    Phase.Put => args.Delta,
                    Phase.Claimed => 0,
                    Phase.Released => 0,
                    Phase.Committed => -args.Delta,
                    _ => throw new ArgumentOutOfRangeException()
                },
                TwoPhaseComponent.Available => message switch
                {
                    Phase.Set => args.Delta,
                    Phase.Put => args.Delta,
                    Phase.Claimed => -args.Delta,
                    Phase.Released => args.Delta,
                    Phase.Committed => 0,
                    _ => throw new ArgumentOutOfRangeException()
                },
                TwoPhaseComponent.Claimed => message switch
                {
                    Phase.Set => 0,
                    Phase.Put => 0,
                    Phase.Claimed => args.Delta,
                    Phase.Released => -args.Delta,
                    Phase.Committed => -args.Delta,
                    _ => throw new ArgumentOutOfRangeException()
                },
                _ => throw new ArgumentOutOfRangeException()
            };
            float t = maxCount == 0 ? 1f : x / (float)maxCount;
            float dt = maxCount == 0 ? 1f : dx / (float)maxCount;

            if (display)
            {
                display.SetFill(t, dt);

                display.CurrentValue?.SetText("{0}", args.Available);
                display.NextValue?.SetText("{0}", args.Available);
            }

            if (icon)
            {
                icon.sprite = prop.IconSprite;
            }

            if (fill)
            {
                fill.fillAmount = t;
            }

            if (renderer)
            {
                _material.SetFloat(floatProperty, t);
            }
        }
    }
}