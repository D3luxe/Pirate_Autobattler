using System.Collections.Generic;
using PirateRoguelike.Data; // Changed from PirateRoguelike.Data.Items

namespace PirateRoguelike.Runtime
{
    public class RuntimeItem
    {
        protected readonly ItemSO BaseItemSO;
        public IReadOnlyList<RuntimeAbility> Abilities { get; private set; }

        public RuntimeItem(ItemSO baseItemSO)
        {
            BaseItemSO = baseItemSO;
            var runtimeAbilities = new List<RuntimeAbility>();
            foreach (var abilitySO in baseItemSO.abilities)
            {
                runtimeAbilities.Add(new RuntimeAbility(abilitySO));
            }
            Abilities = runtimeAbilities;
        }

        public string DisplayName => BaseItemSO.displayName;
        public int CooldownSec => (int)BaseItemSO.cooldownSec; // Example of passing through static data
        public bool IsActive => BaseItemSO.isActive;
    }
}
