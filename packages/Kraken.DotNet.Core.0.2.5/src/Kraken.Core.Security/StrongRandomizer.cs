using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Kraken.Core.Security {

  public sealed class StrongRandomizer {

    private StrongRandomizer() { }

    private static int GenerateStrongSeed() {
      // We will make up an integer seed from 4 bytes of this array.
      byte[] randomBytes = new byte[4];

      // Generate 4 random bytes.
      RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
      rng.GetBytes(randomBytes);

      // Convert four random bytes into a positive integer value.
      int seed = ((randomBytes[0] & 0x7f) << 24) |
                  (randomBytes[1] << 16) |
                  (randomBytes[2] << 8) |
                  (randomBytes[3]);
      return seed;
    }

    /// <summary>
    /// Generates random integer.
    /// </summary>
    /// <param name="minValue">
    /// Min value (inclusive).
    /// </param>
    /// <param name="maxValue">
    /// Max value (inclusive).
    /// </param>
    /// <returns>
    /// Random integer value between the min and max values (inclusive).
    /// </returns>
    /// <remarks>
    /// This methods overcomes the limitations of .NET Framework's Random
    /// class, which - when initialized multiple times within a very short
    /// period of time - can generate the same "random" number.
    /// </remarks>
    public static int Generate(int minValue, int maxValue) {
      int seed = GenerateStrongSeed();
      // Now, this looks more like real randomization.
      Random random = new Random(seed);

      // Calculate a random number.
      return random.Next(minValue, maxValue + 1);
    }

    public static string GetRandomHexNumber(int digits) {
      int seed = GenerateStrongSeed();
      // Now, this looks more like real randomization.
      Random random = new Random(seed);

      byte[] buffer = new byte[digits / 2];
      random.NextBytes(buffer);
      string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
      if (digits % 2 == 0)
        return result;
      return result + random.Next(16).ToString("X");
    }

  }

}
