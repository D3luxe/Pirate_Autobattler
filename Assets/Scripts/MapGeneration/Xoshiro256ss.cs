using System;

namespace Pirate.MapGen
{
    /// <summary>
    /// An implementation of the Xoshiro256** random number generator.
    /// This is a fast, high-quality PRNG suitable for most purposes.
    /// </summary>
    public class Xoshiro256ss : IRandomNumberGenerator
    {
        private ulong s0, s1, s2, s3;

        /// <summary>
        /// Initializes a new instance of the Xoshiro256ss class with a given seed.
        /// The seed is used to initialize the internal state of the generator.
        /// </summary>
        /// <param name="seed">The 64-bit seed value.</param>
        public Xoshiro256ss(ulong seed)
        {
            // Initialize state using SplitMix64 to ensure good dispersion from a single seed
            s0 = SplitMix64(seed);
            s1 = SplitMix64(s0);
            s2 = SplitMix64(s1);
            s3 = SplitMix64(s2);

            // Ensure at least one state variable is non-zero
            if (s0 == 0 && s1 == 0 && s2 == 0 && s3 == 0)
            {
                s0 = 0x8000000000000000UL; // Set a non-zero default if all are zero
            }
        }

        /// <summary>
        /// Generates the next 64-bit unsigned integer.
        /// </summary>
        /// <returns>A random 64-bit unsigned integer.</returns>
        public ulong NextULong()
        {
            ulong result = Rotl(s1 * 5, 7) * 9;

            ulong t = s1 << 17;

            s2 ^= s0;
            s3 ^= s1;
            s1 ^= s2;
            s0 ^= s3;

            s2 ^= t;

            s3 = Rotl(s3, 45);

            return result;
        }

        /// <summary>
        /// Generates a random floating-point number between 0.0 (inclusive) and 1.0 (exclusive).
        /// </summary>
        /// <returns>A random double between 0.0 and 1.0.</returns>
        public double NextDouble()
        {
            // Convert 64-bit unsigned integer to a double in [0.0, 1.0)
            // Uses 53 bits of randomness for IEEE 754 double precision
            return (NextULong() >> 11) * (1.0 / (1UL << 53));
        }

        /// <summary>
        /// Creates a new RNG stream with a derived seed.
        /// </summary>
        /// <param name="seed">The seed for the new stream.</param>
        /// <returns>A new IRandomNumberGenerator instance.</returns>
        public IRandomNumberGenerator CreateStream(ulong seed)
        {
            return new Xoshiro256ss(seed);
        }

        private static ulong Rotl(ulong x, int k)
        {
            return (x << k) | (x >> (64 - k));
        }

        /// <summary>
        /// A simple 64-bit SplitMix PRNG for seeding the Xoshiro256ss generator.
        /// </summary>
        /// <param name="seed">The initial seed.</param>
        /// <returns>A dispersed 64-bit unsigned integer.</returns>
        private static ulong SplitMix64(ulong seed)
        {
            ulong z = (seed + 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
