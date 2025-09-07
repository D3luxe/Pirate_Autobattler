namespace PirateRoguelike.Commands
{
    public interface ICommand
    {
        bool CanExecute();
        void Execute();
    }
}