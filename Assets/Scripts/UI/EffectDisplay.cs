using UnityEngine.UIElements;
using PirateRoguelike.Data.Abilities;

public class EffectDisplay
{
    private Label _descriptionLabel;

    public EffectDisplay(VisualElement root)
    {
        _descriptionLabel = root.Q<Label>("TitleLabel");
    }

    public void SetData(AbilitySO ability)
    {
        if (ability != null)
        {
            // TODO: Get a proper description from the ability and its actions.
            _descriptionLabel.text = ability.displayName;
        }
    }
}