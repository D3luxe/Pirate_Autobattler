namespace Pirate.MapGen
{
    public interface IRandomNumberGenerator
    {
        ulong NextULong();
        double NextDouble(); // Returns a random floating-point number between 0.0 and 1.0
        IRandomNumberGenerator CreateStream(ulong seed);
    }
}
