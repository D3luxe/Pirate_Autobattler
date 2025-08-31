using UnityEngine.UIElements;
using System.ComponentModel;

namespace PirateRoguelike.UI.Utilities
{
    public static class BindingUtility
    {
        public static void BindLabelText(Label label, INotifyPropertyChanged viewModel, string propertyName)
        {
            // Initial bind
            var propertyInfo = viewModel.GetType().GetProperty(propertyName);
            if (propertyInfo != null)
            {
                label.text = propertyInfo.GetValue(viewModel)?.ToString();
            }

            // Subscribe to changes
            viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == propertyName)
                {
                    label.text = propertyInfo.GetValue(viewModel)?.ToString();
                }
            };
        }
    }
}
