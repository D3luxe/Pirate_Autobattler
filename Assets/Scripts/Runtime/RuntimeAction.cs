using PirateRoguelike.Data.Actions;
using PirateRoguelike.Combat; // For IRuntimeContext

namespace PirateRoguelike.Runtime
{
    public abstract class RuntimeAction
    {
        protected readonly ActionSO BaseActionSO;

        public RuntimeAction(ActionSO baseActionSO)
        {
            BaseActionSO = baseActionSO;
        }

        public abstract string BuildDescription(IRuntimeContext context);
    }
}
