namespace Pirate.MapGen
{
    public static class SeedUtility
    {
        /// <summary>
        /// Generates a new seed by hashing the parent seed and a unique salt.
        /// This ensures deterministic sub-seeds for different generation phases.
        /// </summary>
        /// <param name="parentSeed">The parent seed.</param>
        /// <param name="salt">A unique salt for this sub-seed (e.g., a string representing the phase name).</param>
        /// <returns>A new 64-bit unsigned integer seed.</returns>
        public static ulong CreateSubSeed(ulong parentSeed, string salt)
        {
            // Simple hashing function for demonstration. 
            // For production, consider a more robust hashing algorithm if collision resistance is critical.
            unchecked
            {
                ulong hash = parentSeed;
                foreach (char c in salt)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }
    }
}
