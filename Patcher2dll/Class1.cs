﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Patcher2
{
  [Serializable()]
  public class WtfException : Exception
  {
    public WtfException()
      : base("(" + new StackTrace().GetFrame(1).GetMethod().Name + ") This should not have happened.")
    {
    }

    public WtfException(string message)
      : base("(" + new StackTrace().GetFrame(1).GetMethod().Name + ") " + message)
    {
      // Add any type-specific logic.
    }

    public WtfException(string message, Exception innerException) :
      base(message, innerException)
    {
      // Add any type-specific logic for inner exceptions.
    }

    protected WtfException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
      // Implement type-specific serialization constructor logic.
    }
  }

  public static class Patcher
  {
    private const string _q = "?";
    private const string nl = "\r\n";
    private const string _h = "X2";

    public static int[] BinaryPatternSearch(ref byte[] bytes, string[] searchBytes, string wildcard, bool onlyOne)
    {
      int sl = searchBytes.Length;
      if (sl < 1 || bytes.Length < sl)
        return new int[] { };
      byte[] sbytes = new byte[sl];
      for (int i = 0; i < sl; i++)
        if (searchBytes[i] != wildcard)
          if (!Byte.TryParse(searchBytes[i], System.Globalization.NumberStyles.HexNumber, null, out sbytes[i]))
            return new int[] { };

      List<int> locs = new List<int>();
      int n = bytes.Length - sl;
      int found = 0;
      for (var i = 0; i <= n; i++)
      {
        for (int j = 0; j < sl; j++)
          if (searchBytes[j] != wildcard)
            if (bytes[i + j] != sbytes[j])
              goto nope;
        locs.Add(i);
        found++;
        if (onlyOne && found > 1)
          break;
      nope: ;
      }
      return locs.ToArray();
    }

    // (without wildcard)
    public static int[] BinaryPatternSearch(ref byte[] bytes, string[] searchBytes, bool onlyOne)
    {
      return BinaryPatternSearch(ref bytes, searchBytes, _q, onlyOne);
    }

    // (return only 1)
    public static int[] BinaryPatternSearch(ref byte[] bytes, string[] searchBytes, string wildcard)
    {
      return BinaryPatternSearch(ref bytes, searchBytes, wildcard, true);
    }

    // (return only 1 and no wildcard)
    public static int[] BinaryPatternSearch(ref byte[] bytes, string[] searchBytes)
    {
      return BinaryPatternSearch(ref bytes, searchBytes, _q, true);
    }

    private static int largest(ref string[][] list)
    {
      int size = 0;
      for (int i = 0; i < list.Length; i++)
        size = (list[i].Length > size) ? list[i].Length : size;
      return size;
    }

    public static int BinaryPatternReplace(ref byte[] bytes, int[] locs, string[][] replaceBytes, int[] offsets, string wildcard)
    {
      int ls = locs.Length;
      if (ls < 1 || ls != replaceBytes.Length || ls != offsets.Length || bytes.Length < largest(ref replaceBytes))
        return -1;
      int replaced = 0;
      for (int i2 = 0; i2 < ls; i2++)
        for (var i = 0; i < replaceBytes[i2].Length; i++)
          if (replaceBytes[i2][i] != wildcard)
            if (Byte.TryParse(replaceBytes[i2][i], System.Globalization.NumberStyles.HexNumber, null, out bytes[locs[i2] + i + offsets[i2]]))
              replaced++;
            else
              return -1;
      // To get hex back from bytes:
      // string hex = BitConverter.ToString(bytes).Replace("-", " ");
      return replaced;
    }

    // Multiple replaces (without wildcard)
    public static int BinaryPatternReplace(ref byte[] bytes, int[] locs, string[][] replaceBytes, int[] offsets)
    {
      return BinaryPatternReplace(ref bytes, locs, replaceBytes, offsets, _q);
    }

    // Single replace
    public static int BinaryPatternReplace(ref byte[] bytes, int loc, string[] replaceBytes, int offset, string wildcard)
    {
      return BinaryPatternReplace(ref bytes, new int[] { loc }, new string[][] { replaceBytes }, new int[] { offset }, wildcard);
    }

    // (without wildcard)
    public static int BinaryPatternReplace(ref byte[] bytes, int loc, string[] replaceBytes, int offset)
    {
      return BinaryPatternReplace(ref bytes, new int[] { loc }, new string[][] { replaceBytes }, new int[] { offset }, _q);
    }

    // (without offset)
    public static int BinaryPatternReplace(ref byte[] bytes, int loc, string[] replaceBytes, string wildcard)
    {
      return BinaryPatternReplace(ref bytes, new int[] { loc }, new string[][] { replaceBytes }, new int[] { 0 }, wildcard);
    }

    // (without offset or wildcard)
    public static int BinaryPatternReplace(ref byte[] bytes, int loc, string[] replaceBytes)
    {
      return BinaryPatternReplace(ref bytes, new int[] { loc }, new string[][] { replaceBytes }, new int[] { 0 }, _q);
    }

    // Warning, this is not as smart as the Cheat Engine version, as we don't do disassembly,
    // so we don't know which bytes are addresses, that should be masked.
    public static string[][][] FindDiffs(ref byte[] bytes1, ref byte[] bytes2)
    {
      List<string[][]> finds = new List<string[][]>();
      List<string[]> diffs = new List<string[]>();
      if (bytes1.Length != bytes2.Length)
        return new string[][][] { };
      int diffCounter = 0;
      int matchCounter = 0;
      int state; // We only need 3 bits.
      int loc = 0; // Funny, how the compiler thinks this is necessary, but actually isn't.
      List<int> locs = new List<int>();
      List<int> diffLocs = new List<int>();
      for (int i = 0; i < bytes1.Length; i++)
      {
        state = Convert.ToInt32(diffCounter == 0);            // If true, we are not in block.
        state += Convert.ToInt32(bytes1[i] == bytes2[i]) * 2; // If true, no difference, so equals.
        state += Convert.ToInt32(matchCounter < 7) * 4;       // If true, has not matched 8.

        switch (state)
        {
          case 0: // In block, Does not equal, Matched 8. (Block resumes.)
            matchCounter = 0;
            diffLocs.Add(i);
            diffs.Add(new string[] { bytes1[i].ToString(_h), bytes2[i].ToString(_h) });
            break;

          //case 1: // Not in block, Does not equal, Matched 8?
          //  goto default;

          case 2: // In block, Equals, Matched 8. (End of block.)
            diffCounter = 0;
            matchCounter = 0;
            locs.Add(loc);
            finds.Add(MakeUnique(ref bytes1, diffs.ToArray(), loc, diffLocs.ToArray()));
            diffs = new List<string[]>();
            break;

          //case 3: // Not in block, Equals, Matched 8?
          //  goto default;

          case 4: // In block, Does not equal, Has not matched 8 yet. (Adding to block.)
            for (int i2 = 0; i2 < matchCounter; i2++)
              diffs.Add(new string[] { bytes1[i - matchCounter + i2].ToString(_h), _q });
            matchCounter = 0;
            diffLocs.Add(i);
            diffs.Add(new string[] { bytes1[i].ToString(_h), bytes2[i].ToString(_h) });
            break;

          case 5: // Not in block, Does not equal, Has not matched 8 yet. (Beginning of block.)
            loc = i;
            diffLocs.Add(i);
            diffs.Add(new string[] { "0" });
            diffs.Add(new string[] { bytes1[i].ToString(_h), bytes2[i].ToString(_h) });
            diffCounter++;
            break;

          case 6: // In block, Equals, Has not matched 8 yet.
            matchCounter++;
            break;

          case 7: // Not in block, Equals, Has not matched 8 yet.
            continue;

          default:
            throw new WtfException();
        }
      }
      if (diffCounter > 0)
      {
        locs.Add(loc);
        finds.Add(MakeUnique(ref bytes1, diffs.ToArray(), loc, diffLocs.ToArray()));
      }
      return finds.ToArray();
    }

    private static string[][] MakeUnique(ref byte[] bytes, string[][] diffs, int loc, int[] diffLocs)
    {
      string[] relevantBytes = new string[diffs.Length - 1];
      for (int i = 1; i < diffs.Length; i++)
        relevantBytes[i - 1] = diffs[i][0];
      int[] locs = BinaryPatternSearch(ref bytes, relevantBytes);
      if (locs.Length < 1) // Should not happen.
        throw new WtfException("Pattern: " + String.Join(" ", relevantBytes) + " not found?");
      if (locs.Length == 1)
        return diffs;
      string[] extendLeft = ExtendBlock(ref bytes, ref diffLocs, loc, relevantBytes);
      string[] extendRight = ExtendBlock(ref bytes, ref diffLocs, loc, relevantBytes, 1);
      if (extendLeft.Length == 0 && extendRight.Length == 0)
      {
        Console.WriteLine("No unique pattern found for: " + String.Join(" ", relevantBytes));
        return diffs;
      }
      List<string[]> newDiffs = new List<string[]>();
      if (extendLeft.Length <= extendRight.Length)
      {
        if (extendLeft.Length == 0)
          goto right;
        goto left;
      }
      else
      {
        if (extendRight.Length == 0)
          goto left;
        goto right;
      }
    left:
      newDiffs.Add(new string[] { (extendLeft.Length - relevantBytes.Length).ToString() });
      relevantBytes = extendLeft;
      goto finish;
    right:
      newDiffs.Add(new string[] { "0" });
      relevantBytes = extendRight;
    finish:
      for (int i = 0; i < relevantBytes.Length; i++)
        newDiffs.Add(new string[] { relevantBytes[i], (diffs.Length > i + 1) ? diffs[i + 1][1] : "" });
      return newDiffs.ToArray();
    }

    private static string[] ExtendBlock(ref byte[] bytes, ref int[] diffLocs, int loc, string[] block, int direction = -1)
    {
      if (direction != 1)
        direction = -1;
      else
        loc += block.Length - 1;
      List<string> newBlock = new List<string>(block);
      string currByte;
      while (true)
      {
        if ((direction < 0 && loc < 1) || (direction > 0 && loc > bytes.Length - 2)) // Reached beginning or end of data.
          return new string[] { };
        loc += direction;
        currByte = bytes[loc].ToString(_h);
        if (direction == -1)
        {
          if (Array.IndexOf(diffLocs, loc) != -1) // Overlap detection
            currByte = _q;
          newBlock.Insert(0, currByte);
        }
        else
          newBlock.Add(currByte);
        if (currByte == _q)
          continue;
        int[] locs = BinaryPatternSearch(ref bytes, newBlock.ToArray());
        if (locs.Length == 1)
          return newBlock.ToArray();
        if (locs.Length < 1)
          throw new WtfException("Pattern: " + String.Join(" ", newBlock) + " not found?");
      }
    }

    private static string JaggedArray3ToString(ref string[][][] bytes)
    {
      string ret = "";
      string[] line;
      for (int i = 0; i < bytes.Length; i++)
      {
        //line = new string[2] { "Search:   ", "Replace: " };
        line = new string[2] { "", "" };
        for (int i2 = 1; i2 < bytes[i].Length; i2++)
        {
          line[0] += bytes[i][i2][0] + " ";
          if (bytes[i][i2].Length > 1)
            line[1] += bytes[i][i2][1] + " ";
        }
        //ret += line[0].Trim() + nl + "Offset:     " + bytes[i][0][0] + nl + line[1].Trim() + nl;
        ret += line[0].Trim() + nl + bytes[i][0][0] + nl + line[1].Trim() + nl;
      }
      return ret;
    }

    public static string Format4Ini(string[][][] bytes, string file, string name, string postfix)
    {
      string[] origLines = JaggedArray3ToString(ref bytes).Split(new string[] { nl }, StringSplitOptions.RemoveEmptyEntries);
      string ret = "";
      for (int i = 0; i < origLines.Length; i += 3)
        ret += name + String.Format(postfix, i / 3 + 1) + nl + file + nl + origLines[i] + nl + origLines[i + 1] + nl + origLines[i + 2] + nl + nl;
      return ret.Substring(0, ret.Length - nl.Length);
    }

    public static string Format4Ini(string[][][] bytes, string file, string name)
    {
      return Format4Ini(bytes, file, name, " {0}");
    }
  }
}