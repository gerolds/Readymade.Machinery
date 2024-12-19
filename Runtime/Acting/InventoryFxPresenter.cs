using System;
using System.Collections.Generic;
using Readymade.Utils.Feedback;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif
using Readymade.Utils.Patterns;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    public class InventoryFxPresenter : MonoBehaviour
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
        [BoxGroup("Source")] [SerializeField] private List<SoProp> include;

        [BoxGroup("Source")] [SerializeField] private List<SoProp> exclude;
#if ODIN_INSPECTOR
        [SuffixLabel("Optional")]
#endif
        [Tooltip("The floating text spawner to use. If not set, will try to get it from the service locator.")]
        [BoxGroup("Fx")]
        [SerializeField]
        private FloatingTextSpawner floatingTextSpawner;

#if ODIN_INSPECTOR
        [InlineButton(nameof(CreateSettingsAsset), "Create")]
#endif
        [BoxGroup("Fx")]
        [SerializeField]
        private FloatingTextSettings overrideSettings;

        private IInventory<SoProp> _from;
        private float _aggregationTimeout;


        private void CreateSettingsAsset()
        {
#if UNITY_EDITOR && ODIN_INSPECTOR
            FloatingTextSettings asset = ScriptableObject.CreateInstance<FloatingTextSettings>();

            UnityEditor.AssetDatabase.CreateAsset(asset, $"Assets/New {nameof(FloatingTextSettings)}.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.EditorUtility.FocusProjectWindow();
            overrideSettings = asset;
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private void Start()
        {
            if (!floatingTextSpawner)
            {
                Services.TryGet(out floatingTextSpawner);
            }

            _from = selectSource switch
            {
                InventorySelection.LocalReference => source,
                InventorySelection.ServiceLocator => Services.Get<IInventory<SoProp>>(),
                InventorySelection.PropBroker => throw new NotImplementedException(),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (_from != null)
            {
                _from.Modified += ChangedHandler;
            }

            Debug.Assert(_from != null, "ASSERTION FAILED: Inventory is selected.", this);
        }

        private void OnDestroy()
        {
            if (_from != null)
            {
                _from.Modified -= ChangedHandler;
            }
        }

        private void ChangedHandler(Phase message,
            IInventory<SoProp>.InventoryEventArgs args)
        {
            if (!floatingTextSpawner)
            {
                return;
            }

            if (!args.Identity)
            {
                return;
            }

            if (include.Count > 0 && !include.Contains(args.Identity))
            {
                return;
            }

            if (exclude.Count > 0 && exclude.Contains(args.Identity))
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

            floatingTextSpawner.SpawnText(dx, transform.position, overrideSettings, args.Identity.IconSymbol);
        }
    }
}