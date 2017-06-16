using System;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text;

namespace Kraken.Core.Security.Unsafe {

  public class SecureStringTools {

    #region Unmanaged String Code

    // Thanks to Wesner Moise who published his article "Strings UNDOCUMENTED"
    // on TheCodeProject.com; it was a great help in finding a way to obfuscate the
    // data held inside a string and set best practices for limited how often strings
    // are copied. http://www.codeproject.com/dotnet/strings.asp?df=100&forumid=13838&exp=0&select=773966

    /// <summary>
    /// Determines the memory capacity of a managed string.
    /// </summary>
    /// <param name="stringData"></param>
    /// <returns></returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
    public static unsafe int GetCapacity(ref string text) {
      int capacity;
      fixed (char* p = text) {
        capacity = GetCapacity(p);
      }
      return capacity;
    }
    internal static unsafe int GetCapacity(char* fixedText) {
      int* pcapacity = (int*)fixedText - 2;
      return *pcapacity;
    }

    /// <summary>
    /// This function is redundant, because it accomplishes the same
    /// role as s.Length, but it does demonstrate some of the precautions
    /// that must be taken to recover the length variable.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    internal static unsafe int GetLength(ref string text) {
      int length;
      fixed (char* p = text) {
        length = GetLength(p);
      }
      return length;
    }
    internal static unsafe int GetLength(char* fixedText) {
      int* plength = (int*)fixedText - 1;
      int length = *plength & 0x3fffffff;
      return length;
    }

    /// <summary>
    /// Hacks the internal structure of System.String in order to manaully reset the Lgenth property
    /// </summary>
    /// <param name="s"></param>
    /// <param name="length"></param>
    internal static unsafe void SetLength(ref string s, int length) {
      fixed (char* p = s) {
        SetLength(p, length);
      }
    }
    internal static unsafe void SetLength(char* p, int length) {
      int* pi = (int*)p;
      if (length < 0 || length > pi[-2])
        throw (new ArgumentOutOfRangeException("length"));
      pi[-1] = length;
      p[length] = '\0';
    }

    #endregion

    #region Security

    /// <summary>
    /// Compare a managed string against an unmanaged string to see if they have equal values.
    /// </summary>
    /// <param name="strTest"></param>
    /// <param name="strCompareTo"></param>
    /// <returns></returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals")]
    [CLSCompliant(false)]
    public static unsafe bool IsStringContentEqualCharArray(string text, char* compareText) {
      fixed (char* strFixed = text) { // don't have a really great reason for fixed, but what the hey, right!
        for (int i = 0; i < text.Length; i++) {
          if (compareText[i] != text[i])
            return false;
        }
        return (compareText[text.Length] == 0); // also checks for terminating zero
      }
    }

    /// <summary>
    /// Replaces each character in a string with a random byte, so that (at best)
    /// an evesropper will only be able to determine the original capcity of the
    /// string, not its contents. When finished, the routine zeroes out all the
    /// characters, leaving an empty string. This method is slightly better than
    /// the version for unmanaged strings, since it can clear out space beyond the
    /// terminating null character ('\0').
    /// </summary>
    /// <param name="strData">A managed string you want to scramble</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
    public static unsafe void RandomizeAndZero(ref string text) {
      fixed (char* stringFixed = text) {
        // Determine capacity
        int capacity = GetCapacity(stringFixed);
        RandomizeAndZero(stringFixed, capacity);
      } // fixed
    }
    /// <summary>
    /// Scrambles an unmanaged string. WARNING Be very careful with this method!
    /// For truly unmanaged strings, RandomizeAndZerUnmanagedCharArray instead.
    /// </summary>
    /// <param name="stringFixed"></param>
    // HACK internal class marked public for SecureStringExample
    [CLSCompliant(false)]
    public static unsafe void RandomizeAndZero(char* fixedText) {
      int capacity = GetCapacity(fixedText); // 0;
      // do {} while (stringFixed[capacity++] != '\0');
      RandomizeAndZero(fixedText, capacity);
    }
    /// <summary>
    /// With unmanaged string, it is pretty much impossible to tell their
    /// true "capacity", but we can determine the current string's length.
    /// </summary>
    /// <param name="strFixed"></param>
    //[CLSCompliant(false)]
    internal static unsafe void RandomizeAndZeroUnmanagedCString(char* fixedText) {
      int capacity = 0;
      do { } while (fixedText[capacity++] != '\0');
      RandomizeAndZero(fixedText, capacity);
    }
    /// <summary>
    /// Scrambles an unmanaged string. WARNING Be very careful with this method!
    /// 
    /// Note that this will produce weird results for untermined strings.
    /// since providing an incorrect length can result in overwriting of data.
    /// Such an attach/bug could crash the computer, or result in a buffer overrun.
    /// </summary>
    /// <param name="stringFixed"></param>
    internal static unsafe void RandomizeAndZero(char* fixedText, int capacity) {
      int capacityCheck = GetCapacity(fixedText);
      if (capacityCheck < capacity) {
        Debug.Write(string.Format("Skipped string randomizer because passed capacity exceeded best guess from point value. c={0}; pcheck={1}", capacity, capacityCheck));
        return;
      }

#if !ALLOW_UNTESTED_STRING_RANDOMIZER
      Debug.Write("Skipped string randomizer because this unsafe code has not been fully tested.");
      return;
#else
      // Scramble the heck out of it!!! 
      // we need a truly random seed at least, because successive 
      // calls to Random may not make a random number without it.
      Random randGen = new Random(StrongRandomizer.Generate(int.MinValue, int.MaxValue - 1));

      // first pass
      for (int i = capacity - 1; i >= 0; i--)
        fixedText[i] = Convert.ToChar(randGen.Next(0, 256));
      // second pass
      for (int i = capacity - 1; i >= 0; i--)
        fixedText[i] = Convert.ToChar(randGen.Next(0, 256));
      // third pass
      for (int i = capacity - 1; i >= 0; i--)
        fixedText[i] = Convert.ToChar(randGen.Next(0, 256));
      // zero me
      for (int i = capacity - 1; i >= 0; i--) {
        fixedText[i] = '\0';
#if DEBUG
        if (i == capacity)
          Debug.WriteLine(new string(fixedText));
#endif
      } // for
      SetLength(fixedText, 0);
#endif
    }

    public static string RandomText(int length) {
      int min = System.Convert.ToInt32('A');
      int max = System.Convert.ToInt32('Z');
      Random randGen = new Random(StrongRandomizer.Generate(int.MinValue, int.MaxValue - 1));
      StringBuilder sb = new StringBuilder();
      for (int i = length - 1; i >= 0; i--)
        sb.Append(Convert.ToChar(randGen.Next(min, max)));
      return sb.ToString();
    }

    #endregion

    #region Arrays

    internal static unsafe void RandomizeAndZero(ref byte[] bytes) {
      fixed (byte* fixedBytes = bytes) {
        RandomizeAndZero(fixedBytes, bytes.Length);
      }
    }

    /// <summary>
    /// Scrambles and zeroes out a byte array of a known length
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="length"></param>
    internal static unsafe void RandomizeAndZero(byte* bytes, int length) {
      // Scramble the heck out of it!!!
      Random randGen = new Random();
      // first pass - zag
      for (int i = length - 1; i >= 0; i--)
        bytes[i] = Convert.ToByte(randGen.Next(0, 256));
      // second pass - zig
      for (int i = 0; i < length; i++)
        bytes[i] = Convert.ToByte(randGen.Next(0, 256));
      // third pass - zag
      for (int i = length - 1; i >= 0; i--)
        bytes[i] = Convert.ToByte(randGen.Next(0, 256));
      // zero me - zig
      for (int i = 0; i < length; i++)
        bytes[i] = 0;
    }
    
    #endregion

    #region StrongRandomizer

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

      // Now, this looks more like real randomization.
      Random random = new Random(seed);

      // Calculate a random number.
      return random.Next(minValue, maxValue + 1);
    }

    #endregion

  }

}