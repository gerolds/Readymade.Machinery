using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Readymade.Machinery.Acting
{
    public class PropConsumerComponent : MonoBehaviour, IPropConsumer
    {
        [SerializeField] private InventoryComponent inventoryComponent;

        [Tooltip(
            "The list of consumable props and their associated effect. Effect triggering props are assumed to be unique in this list.")]
        [SerializeField]
        private List<SoEffect> consumables;

        public bool CanConsume(SoProp prop) => consumables.Any(it => it.UnlockedBy == prop);

        public bool TryConsume(SoProp prop, IActor actor)
        {
            bool hasProp = inventoryComponent.TryTakeImmediately(prop, 1);
            if (!hasProp)
            {
                return false;
            }

            SoEffect effect = consumables.FirstOrDefault(it => it.UnlockedBy == prop);
            if (effect)
            {
                effect.InvokeFor(actor);
            }

            return true;
        }

        public bool TryGetEffect(SoProp prop, out SoEffect soEffect)
        {
            soEffect = consumables.FirstOrDefault(it => it.UnlockedBy == prop);
            return soEffect;
        }
    }
}