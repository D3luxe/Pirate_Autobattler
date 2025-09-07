using UnityEngine;
using PirateRoguelike.Services; // For SlotId, SlotContainerType if needed for logging/context

namespace PirateRoguelike.Commands
{
    public class UICommandProcessor
    {
        private static UICommandProcessor _instance;
        public static UICommandProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UICommandProcessor();
                }
                return _instance;
            }
        }

        private UICommandProcessor() { } // Private constructor for singleton

        public void ProcessCommand(ICommand command)
        {
            if (command == null)
            {
                Debug.LogError("Attempted to process a null command.");
                return;
            }

            if (command.CanExecute())
            {
                command.Execute();
            }
            else
            {
                Debug.LogWarning($"Command {command.GetType().Name} cannot be executed. Validation failed.");
                // TODO: Potentially dispatch a UI event for user feedback (e.g., "Invalid action!")
            }
        }
    }
}