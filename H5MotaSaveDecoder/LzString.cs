using System.Text;

namespace H5MotaSaveDecoder;

/// <summary>LZString Base64 wire format, using UTF-16 code units just like JavaScript.</summary>
public static class LzString
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    private const int MaxDecodedChars = 64 * 1024 * 1024;

    public static string CompressToBase64(string input)
    {
        var dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
        var pending = new HashSet<string>(StringComparer.Ordinal);
        var writer = new BitWriter();
        int dictionarySize = 3, enlargeIn = 2, bitCount = 2;
        string word = "";

        void Grow()
        {
            if (--enlargeIn == 0)
            {
                enlargeIn = 1 << bitCount;
                bitCount++;
            }
        }

        void Emit(string value)
        {
            if (pending.Remove(value))
            {
                int code = value[0];
                writer.Write(bitCount, code < 256 ? 0 : 1);
                writer.Write(code < 256 ? 8 : 16, code);
                Grow();
            }
            else writer.Write(bitCount, dictionary[value]);
            Grow();
        }

        foreach (char character in input)
        {
            string next = character.ToString();
            if (!dictionary.ContainsKey(next))
            {
                dictionary[next] = dictionarySize++;
                pending.Add(next);
            }
            string combined = word + next;
            if (dictionary.ContainsKey(combined)) word = combined;
            else
            {
                Emit(word);
                dictionary[combined] = dictionarySize++;
                word = next;
            }
        }
        if (word.Length > 0) Emit(word);
        writer.Write(bitCount, 2);
        return writer.Finish();
    }

    public static string DecompressFromBase64(string input)
    {
        input = input.Trim().TrimStart('\uFEFF');
        if (input.Length == 0) throw new InvalidDataException("存档文件为空。");
        var reader = new BitReader(input);
        int initial = reader.Read(2);
        if (initial == 2) return "";
        if (initial > 1) throw InvalidFormat();
        string word = ((char)reader.Read(initial == 0 ? 8 : 16)).ToString();
        var dictionary = new List<string> { "", "", "", word };
        var result = new StringBuilder(word);
        int enlargeIn = 4, bitCount = 3;

        void GrowIfNeeded()
        {
            if (enlargeIn == 0)
            {
                if (bitCount >= 25) throw InvalidFormat();
                enlargeIn = 1 << bitCount;
                bitCount++;
            }
        }

        while (true)
        {
            int code = reader.Read(bitCount);
            if (code == 2) return result.ToString();
            if (code is 0 or 1)
            {
                dictionary.Add(((char)reader.Read(code == 0 ? 8 : 16)).ToString());
                code = dictionary.Count - 1;
                enlargeIn--;
            }
            GrowIfNeeded();
            string entry;
            if (code < dictionary.Count) entry = dictionary[code];
            else if (code == dictionary.Count) entry = word + word[0];
            else throw InvalidFormat();
            if (entry.Length == 0) throw InvalidFormat();
            if ((long)result.Length + entry.Length > MaxDecodedChars)
                throw new InvalidDataException("解码内容超过 64 MiB 字符限制。");
            result.Append(entry);
            dictionary.Add(word + entry[0]);
            enlargeIn--;
            word = entry;
            GrowIfNeeded();
        }
    }

    private static InvalidDataException InvalidFormat() => new("存档不是有效的 LZString Base64 数据，或文件已损坏。");

    private sealed class BitWriter
    {
        private readonly StringBuilder output = new();
        private int value, position;
        public void Write(int count, int bits)
        {
            for (int i = 0; i < count; i++)
            {
                value = (value << 1) | (bits & 1);
                bits >>= 1;
                if (++position == 6)
                {
                    output.Append(Alphabet[value]);
                    position = value = 0;
                }
            }
        }
        public string Finish()
        {
            // LZString always flushes one final sextet, even at an exact boundary.
            do { value <<= 1; position++; } while (position < 6);
            output.Append(Alphabet[value]);
            while (output.Length % 4 != 0) output.Append('=');
            return output.ToString();
        }
    }

    private sealed class BitReader
    {
        private readonly string input;
        private int index, mask = 32;
        public BitReader(string text)
        {
            int length = text.TrimEnd('=').Length;
            if (text.Length - length > 3) throw InvalidFormat();
            for (int i = 0; i < length; i++)
                if (Alphabet.IndexOf(text[i]) < 0) throw InvalidFormat();
            input = text[..length];
        }
        public int Read(int count)
        {
            int result = 0;
            for (int i = 0; i < count; i++)
            {
                if (index >= input.Length) throw InvalidFormat();
                if ((Alphabet.IndexOf(input[index]) & mask) != 0) result |= 1 << i;
                mask >>= 1;
                if (mask == 0) { mask = 32; index++; }
            }
            return result;
        }
    }
}
