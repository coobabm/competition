using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LingGuangV05.Core.Story
{
    /// <summary>
    /// Minimal JSON reader for story content. Core has no engine references, so it cannot use
    /// JsonUtility; story data also needs maps and mixed string/number values, which JsonUtility
    /// cannot express. Produces Dictionary&lt;string,object&gt;, List&lt;object&gt;, string, double, bool or null.
    /// </summary>
    public static class StoryJson
    {
        public static object Parse(string json)
        {
            if (json == null) throw new StoryFormatException("JSON text is null.");
            var reader = new Reader(json);
            reader.SkipSpace();
            object value = reader.ReadValue();
            reader.SkipSpace();
            if (!reader.End) throw reader.Error("Unexpected text after the root value");
            return value;
        }

        private sealed class Reader
        {
            private readonly string text;
            private int pos;
            public Reader(string source)
            {
                text = source;
                // Tolerate a UTF-8 byte order mark from editors on Windows.
                if (text.Length > 0 && text[0] == '﻿') pos = 1;
            }
            public bool End { get { return pos >= text.Length; } }

            public StoryFormatException Error(string message)
            {
                int line = 1, column = 1;
                for (int i = 0; i < pos && i < text.Length; i++)
                {
                    if (text[i] == '\n') { line++; column = 1; } else column++;
                }
                return new StoryFormatException(message + " (line " + line + ", column " + column + ").");
            }

            public void SkipSpace()
            {
                while (pos < text.Length)
                {
                    char c = text[pos];
                    if (c == ' ' || c == '\t' || c == '\r' || c == '\n') { pos++; continue; }
                    // Writers may leave // comments in content files; accept them.
                    if (c == '/' && pos + 1 < text.Length && text[pos + 1] == '/')
                    {
                        while (pos < text.Length && text[pos] != '\n') pos++;
                        continue;
                    }
                    break;
                }
            }

            public object ReadValue()
            {
                if (End) throw Error("Unexpected end of JSON");
                char c = text[pos];
                switch (c)
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return ReadNumber();
                        throw Error("Unexpected character '" + c + "'");
                }
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(text, pos, word, 0, word.Length) != 0) throw Error("Expected '" + word + "'");
                pos += word.Length;
            }

            private Dictionary<string, object> ReadObject()
            {
                var result = new Dictionary<string, object>(StringComparer.Ordinal);
                pos++;
                SkipSpace();
                if (!End && text[pos] == '}') { pos++; return result; }
                while (true)
                {
                    SkipSpace();
                    if (End || text[pos] != '"') throw Error("Expected a quoted key");
                    string key = ReadString();
                    SkipSpace();
                    if (End || text[pos] != ':') throw Error("Expected ':' after key \"" + key + "\"");
                    pos++;
                    SkipSpace();
                    if (result.ContainsKey(key)) throw Error("Duplicate key \"" + key + "\"");
                    result[key] = ReadValue();
                    SkipSpace();
                    if (End) throw Error("Unterminated object");
                    if (text[pos] == ',') { pos++; continue; }
                    if (text[pos] == '}') { pos++; return result; }
                    throw Error("Expected ',' or '}'");
                }
            }

            private List<object> ReadArray()
            {
                var result = new List<object>();
                pos++;
                SkipSpace();
                if (!End && text[pos] == ']') { pos++; return result; }
                while (true)
                {
                    SkipSpace();
                    result.Add(ReadValue());
                    SkipSpace();
                    if (End) throw Error("Unterminated array");
                    if (text[pos] == ',') { pos++; continue; }
                    if (text[pos] == ']') { pos++; return result; }
                    throw Error("Expected ',' or ']'");
                }
            }

            private string ReadString()
            {
                pos++;
                var sb = new StringBuilder();
                while (true)
                {
                    if (End) throw Error("Unterminated string");
                    char c = text[pos++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (End) throw Error("Unterminated escape");
                    char e = text[pos++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (pos + 4 > text.Length) throw Error("Bad unicode escape");
                            sb.Append((char)int.Parse(text.Substring(pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            pos += 4;
                            break;
                        default: throw Error("Bad escape '\\" + e + "'");
                    }
                }
            }

            private double ReadNumber()
            {
                int start = pos;
                if (text[pos] == '-') pos++;
                while (pos < text.Length)
                {
                    char c = text[pos];
                    if ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-') pos++;
                    else break;
                }
                double value;
                if (!double.TryParse(text.Substring(start, pos - start), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    throw Error("Bad number");
                return value;
            }
        }
    }

    public sealed class StoryFormatException : Exception
    {
        public StoryFormatException(string message) : base(message) { }
    }
}
