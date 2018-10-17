using Microsoft.SharePoint.Administration.Claims;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.IdentityModel {

  /// <summary>
  /// This is a non-persisted port of MS internal class 
  /// Encoding which is used by SPClaimEncodingManager.
  /// It is used as a basis for encoded character to 
  /// claim type and claim value type converation classes.
  /// </summary>
  public abstract class ClaimsEncodingBase { // : SPAutoSerializingObject

    private const int StartIndex = 500;

    // [Persisted]
    protected Hashtable m_EncodingScheme;

    /// <summary>
    /// Indicates that initialization has been performed.
    /// Typically this involves adding dictionary items
    /// using AddValueToScheme.
    /// </summary>
    // [Persisted]
    protected bool m_Initialized;

    /// <summary>
    /// This would generally track whatever is the next instance in SharePoint
    /// which has an open unicode character to use for a custom claim.
    /// </summary>
    /// <remarks>
    /// Note this is not permanently stored anyplace, and so it has
    /// to be reconstructed each time this class is instantiated.
    /// </remarks>
    //[Persisted]
    private int m_NextIndex;

    private static Dictionary<string, string> _formsReplaceStrings = null;
    public Dictionary<string, string> FormsReplaceStrings {
      get {
        if (_formsReplaceStrings == null)
          _formsReplaceStrings = CreateFormsReplaceStrings();
        return _formsReplaceStrings;
      }
    }

    /* Most likely provided to allow serialization
     * and thus not needed in our implementation
    public ClaimsEncoding() {
    }
     */
    protected internal ClaimsEncodingBase(bool initialize) {
      this.m_EncodingScheme = new Hashtable();
      //this.m_NextIndex = 500;
      if (initialize)
        this.Initialize();
    }

    #region Set-up

    public virtual void Initialize() {
      this.m_Initialized = true;
    }

    /// <summary>
    /// This method was originally intended to help add
    /// custom claim types to SP config db. In our implementation
    /// it might be used to help reconstruct those claims.
    /// </summary>
    /// <param name="value"></param>
    protected void AddValueToScheme(string value) {
      if (value == null)
        throw new ArgumentNullException("value");
      if (!this.m_EncodingScheme.ContainsValue(value)) {
        int nextSafeIndex = this.GetNextSafeIndex();
        this.m_EncodingScheme[nextSafeIndex] = value;
      }
    }

    protected void AddValueToScheme(int key, string value)
    {
      if (value == null)
        throw new ArgumentNullException("value");
      if (this.m_EncodingScheme.ContainsKey(key))
        throw new ArgumentException(null, "key");
      if (this.m_EncodingScheme.ContainsValue(value))
        throw new ArgumentException(null, "value");
      this.m_EncodingScheme[key] = value;
    }
        
    #endregion

    #region Encoding/Decoding a char/int to string/claim 

    /// <summary>
    /// Decode a value into the stored string counterpart.
    /// Throws and exception if you provide value for 
    /// encodedValue that is not in the dictionary.
    /// </summary>
    /// <param name="encodedValue"></param>
    /// <returns></returns>
    public virtual string DecodeValue(int encodedValue) {
      if (!this.m_EncodingScheme.ContainsKey(encodedValue))
        throw new ArgumentException(null, "encodedValue");
      return (string)this.m_EncodingScheme[encodedValue];
    }

    /// <summary>
    /// Try to encode a string value into the int/char.
    /// Returns -1 if value is not in the dictionary.
    /// </summary>
    /// <param name="encodedValue"></param>
    /// <returns></returns>
    public virtual int EncodeValue(string value) {
      if (this.m_EncodingScheme.ContainsValue(value)) {
        foreach (DictionaryEntry entry in this.m_EncodingScheme) {
          if (entry.Value.Equals(value)) {
            return (int)entry.Key;
          }
        }
      }
      return -1;
    }

    #endregion

    #region FormsClaims

    private static Dictionary<string, string> CreateFormsReplaceStrings() {
      Dictionary<string, string> dictionary = new Dictionary<string, string>();
      dictionary.Add("%2C", ",");
      dictionary.Add("%2c", ",");
      dictionary.Add("%3A", ":");
      dictionary.Add("%3a", ":");
      dictionary.Add("%3B", ";");
      dictionary.Add("%3b", ";");
      dictionary.Add("%0A", "\n");
      dictionary.Add("%0a", "\n");
      dictionary.Add("%0D", "\r");
      dictionary.Add("%0d", "\r");
      dictionary.Add("%7C", new string(new char[] { '|' })); // why is this done this way??
      dictionary.Add("%7c", new string(new char[] { '|' }));
      dictionary.Add("%25", "%");
      return dictionary;
    }

    /// <summary>
    /// Decode URL encoded characters in string so it can be safely 
    /// used. This is the opposite of EncodeForFormsClaimsSafety.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string DecodeForFormsClaimsSafety(string input) {
      if (_formsReplaceStrings == null)
        _formsReplaceStrings = CreateFormsReplaceStrings();
      if (input == null)
          throw new ArgumentNullException("input");
      string str = input;
      foreach (string key in _formsReplaceStrings.Keys) {
        str = str.Replace(key, _formsReplaceStrings[key]);
      }
      return str;
    }

    /// <summary>
    /// URL Encodes characters in string used for forms claims.
    /// This is the opposite of DecodeForFormsClaimsSafety.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string EncodeForFormsClaimsSafety(string input) {
      if (input == null)
        throw new ArgumentNullException("input");
      string str = input;
      foreach (string key in _formsReplaceStrings.Keys) {
        // skip any items with the lowercase variant of the URL encoding
        // yes this is not as performant, but at least it avoids duplicates
        if (key.ToUpper().Equals(key, StringComparison.InvariantCulture))
          str = str.Replace(_formsReplaceStrings[key], key);
      }
      return str;
    }

    #endregion

    #region Next Safe Character Logic

    private static char[] disallowedClaimEncodingChars = new char[]{':', ',',';','|'};

    private int GetNextSafeIndex() {
      this.m_NextIndex = this.GenerateNextSafeIndex(this.m_NextIndex);
      return this.m_NextIndex;
    }
    private int GenerateNextSafeIndex(int index) {
      int key = 0;
      if (index < 0x20) {
        // for numbers where index less than 20$(hex)
        // if less than 9$(h) make key equal 9
        // otherwise make it 20$(h)
        if (index < 9) {
          key = 9;
        } else if (9 <= index) { // this condition seems redundant
          key = 0x20;
        }
      } else if ((0x20 <= index) && (index < 0xd7ff)) {
        // For index 20$ and up but less than d7ff$
        // make key = index + 1 - this is a safe range
        key = index + 1;
      } else if ((0xd7ff <= index) && (index < 0xdfff)) {
        // For index d7ff$ and up but less than dfff$
        // make key e000$ - this range is reserved
        key = 0xe000;
      } else if ((0xe000 <= index) && (index < 0xfffd)) {
        // For index e000$ and up but less than fffd$
        // make key = index + 1 - this is a safe range
        key = index + 1;
      } else if ((0xfffd <= index) && (index < 0x10000)) {
        // For index fffd$ and up but less than 10000$
        // make key 10000$ - this range is reserved
        key = 0x10000;
      } else if ((0x10000 <= index) && (index < 0x10ffff)) {
        // For index 10000$ and up but less than 10ffff$
        // make key = index + 1 - this is a safe range
        key = index + 1;
      } else {
        key = -1;
      }
      char c = (char) key;
      if (
        !disallowedClaimEncodingChars.Contains(c)
        && !char.IsWhiteSpace(c) 
        && !char.IsUpper(c)
        && !this.m_EncodingScheme.ContainsKey(key)
      ) {
        // If not one of the reserved/disallowed characters,
        // not an uppercase character (why?), not whitespace, 
        // and not something that is already in the dictionary 
        // and thus in use for some other claim, then this 
        // key/character is OK to use.
        return key;
      }
      // Otherwise, move to the next available charcter
      return this.GenerateNextSafeIndex(key);
    }

    #endregion

    public IEnumerable<KeyValuePair<char, string>> GetEncodings() {
      // This is a fancy, performance-enhanced way of returning
      // a version of the dictionary cast as IEnumerable of some kind
      // of generic KeyValuePair, like (but not necessarily) a dictionary
      IDictionaryEnumerator enumerator = this.m_EncodingScheme.GetEnumerator();
      while (enumerator.MoveNext()) {
        DictionaryEntry current = (DictionaryEntry) enumerator.Current;
        char c = (char)(current.Key);
        KeyValuePair<char, string> iteratorVariable = new KeyValuePair<char, string>(c, (string) current.Value);
        yield return iteratorVariable;
      }
    }

    /// <summary>
    /// Determines of the requested key is in
    /// the dictionary or not
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected bool IsKeyInScheme(int key) {
      return this.m_EncodingScheme.ContainsKey(key);
    }

    /// <summary>
    /// Uses logic from GenerateNextSafeIndex
    /// and basic rule that &lt; 500(dec) is false
    /// to determine if a requested encoding key
    /// is valid or not.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected bool IsValidKey(int key) {
      bool flag = (key >= 500 && this.GenerateNextSafeIndex(key - 1) == key);
      return flag;
    }

    protected bool IsValueInScheme(string value) {
      if (value == null)
        throw new ArgumentNullException("value");
      if (string.IsNullOrEmpty(value))
        throw new ArgumentException(null, "value");
      return this.m_EncodingScheme.ContainsValue(value);
    }

    public abstract void Refresh(bool force = false);
    // TODO uses SPClaimProviderManager which is not sandbox safe
    //public abstract void Refresh(SPClaimProviderManager manager);

    /* the following methods can't be implemented in our version
     * 
     * It is also not entirely clear what the purpose or behavior of
     * d__0 is as it is compiler generated so kind of obfuscated,
     * and yet we couldn't find anything that references/uses it.
     */
    #region Sir Not-Appearing-in-this-Film
    /*
     
      // Nested Types
      [CompilerGenerated]
      private sealed class <GetEncodings>d__0 : IEnumerable<KeyValuePair<char, string>>, IEnumerable, IEnumerator<KeyValuePair<char, string>>, IEnumerator, IDisposable
      {
          // Fields
          private int <>1__state;
          private KeyValuePair<char, string> <>2__current;
          public SPClaimEncodingManager.Encoding <>4__this;
          public IDictionaryEnumerator <>7__wrap5;
          public IDisposable <>7__wrap6;
          private int <>l__initialThreadId;
          public char <charValue>5__3;
          public DictionaryEntry <entry>5__4;
          public int <intValue>5__2;
          public KeyValuePair<char, string> <result>5__1;

          // Methods
          [DebuggerHidden]
          public <GetEncodings>d__0(int <>1__state)
          {
              this.<>1__state = <>1__state;
              this.<>l__initialThreadId = Thread.CurrentThread.ManagedThreadId;
          }

          private void <>m__Finally7()
          {
              this.<>1__state = -1;
              this.<>7__wrap6 = this.<>7__wrap5 as IDisposable;
              if (this.<>7__wrap6 != null)
              {
                  this.<>7__wrap6.Dispose();
              }
          }

          private bool MoveNext()
          {
              bool flag;
              try
              {
                  switch (this.<>1__state)
                  {
                      case 0:
                          this.<>1__state = -1;
                          this.<>7__wrap5 = this.<>4__this.m_EncodingScheme.GetEnumerator();
                          this.<>1__state = 1;
                          goto Label_00C2;

                      case 2:
                          this.<>1__state = 1;
                          goto Label_00C2;

                      default:
                          goto Label_00D8;
                  }
              Label_0044:
                  this.<entry>5__4 = (DictionaryEntry) this.<>7__wrap5.Current;
                  this.<intValue>5__2 = (int) this.<entry>5__4.Key;
                  this.<charValue>5__3 = (char) this.<intValue>5__2;
                  this.<result>5__1 = new KeyValuePair<char, string>(this.<charValue>5__3, (string) this.<entry>5__4.Value);
                  this.<>2__current = this.<result>5__1;
                  this.<>1__state = 2;
                  return true;
              Label_00C2:
                  if (this.<>7__wrap5.MoveNext())
                  {
                      goto Label_0044;
                  }
                  this.<>m__Finally7();
              Label_00D8:
                  flag = false;
              }
              fault
              {
                  this.System.IDisposable.Dispose();
              }
              return flag;
          }

          [DebuggerHidden]
          IEnumerator<KeyValuePair<char, string>> IEnumerable<KeyValuePair<char, string>>.GetEnumerator()
          {
              if ((Thread.CurrentThread.ManagedThreadId == this.<>l__initialThreadId) && (this.<>1__state == -2))
              {
                  this.<>1__state = 0;
                  return this;
              }
              return new SPClaimEncodingManager.Encoding.<GetEncodings>d__0(0) { <>4__this = this.<>4__this };
          }

          [DebuggerHidden]
          IEnumerator IEnumerable.GetEnumerator()
          {
              return this.System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Char,System.String>>.GetEnumerator();
          }

          [DebuggerHidden]
          void IEnumerator.Reset()
          {
              throw new NotSupportedException();
          }

          void IDisposable.Dispose()
          {
              switch (this.<>1__state)
              {
                  case 1:
                  case 2:
                      try
                      {
                      }
                      finally
                      {
                          this.<>m__Finally7();
                      }
                      return;
              }
          }

          // Properties
          KeyValuePair<char, string> IEnumerator<KeyValuePair<char, string>>.Current
          {
              [DebuggerHidden]
              get
              {
                  return this.<>2__current;
              }
          }

          object IEnumerator.Current
          {
              [DebuggerHidden]
              get
              {
                  return this.<>2__current;
              }
          }
      }
     * 
     */
    #endregion

  } // class
} // namespace

