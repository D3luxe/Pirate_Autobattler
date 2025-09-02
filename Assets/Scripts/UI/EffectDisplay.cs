using UnityEngine.UIElements;
using PirateRoguelike.Data.Abilities;
using System.Text;
using PirateRoguelike.Runtime; // Added for RuntimeAbility and RuntimeAction
using PirateRoguelike.Combat; // Added for IRuntimeContext

namespace PirateRoguelike.UI
{
    public class EffectDisplay
    {
        private Label _descriptionLabel;

        public EffectDisplay(VisualElement root)
        {
            _descriptionLabel = root.Q<Label>("TitleLabel");
        }

        public void SetData(RuntimeAbility runtimeAbility, IRuntimeContext context)
        {
            if (runtimeAbility == null)
            {
                _descriptionLabel.text = "No ability data.";
                return;
            }

            if (runtimeAbility.Actions == null || runtimeAbility.Actions.Count == 0)
            {
                _descriptionLabel.text = runtimeAbility.DisplayName; // Fallback to display name if no actions
                return;
            }

            StringBuilder descriptionBuilder = new StringBuilder();
            foreach (var runtimeAction in runtimeAbility.Actions)
            {
                if (runtimeAction != null)
                {
                    descriptionBuilder.AppendLine(runtimeAction.BuildDescription(context));
                }
            }

            if (descriptionBuilder.Length == 0)
            {
                _descriptionLabel.text = runtimeAbility.DisplayName; // Fallback if actions have no descriptions
            }
            else
            {
                _descriptionLabel.text = descriptionBuilder.ToString().Trim();
            }
        }
    }
}