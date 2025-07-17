//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using System;
using System.Runtime.InteropServices;

namespace ETWAnalyzer.TraceProcessorHelpers
{
    /// <summary>
    /// When going though an byte buffer keep track of source and current offset.
    /// </summary>
    ref struct ParseContext
    {
        public ReadOnlySpan<byte> Data;
        public int Offset;
    }

    /// <summary>
    /// Extensions to parse a byte span sequentially 
    /// </summary>
    static class ParserExtensions
    {
        /// <summary>
        /// Check if there is still data to parse in the byte span.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns>true if data is left or false.</returns>
        public static bool HasData(this ref ParseContext ctx)
        {
            return ctx.Offset < ctx.Data.Length;
        }

        /// <summary>
        /// Parse a byte from the byte span and advance the offset.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static UInt64 ParseUInt64(this ref ParseContext ctx)
        {
            if (ctx.Data.Length < ctx.Offset + sizeof(ulong))
            {
                throw new InvalidOperationException("Not enough data to read UInt64.");
            }

            return ReadUnmanaged<UInt64>(ref ctx);
        }

        /// <summary>
        /// Parse a UInt32 from the byte span and advance the offset.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static UInt32 ParseUInt32(this ref ParseContext ctx)
        {
            if (ctx.Data.Length < ctx.Offset + sizeof(uint))
            {
                throw new InvalidOperationException("Not enough data to read UInt32.");
            }
            return ReadUnmanaged<UInt32>(ref ctx);
        }

        /// <summary>
        /// Read a null-terminated UTF-16 string from the byte span.
        /// </summary>
        /// <param name="ctx">Parser Context</param>
        /// <returns>Parsed string or empty string if nothing was present.</returns>
        public static string ReadNullTerminatedUTF16String(this ref ParseContext ctx)
        {
            // Find the null terminator (two consecutive zero bytes) in the byte span
            int i = ctx.Offset;
            int end = ctx.Data.Length;
            int start = ctx.Offset;
            while (i + 1 < end)
            {
                if (ctx.Data[i] == 0 && ctx.Data[i + 1] == 0)
                {
                    // Found null terminator
                    int byteLength = i - start;
                    if (byteLength == 0)
                    {
                        ctx.Offset = i + 2;
                        return string.Empty;
                    }
                    // Convert bytes to string (UTF-16LE)
                    string result = System.Text.Encoding.Unicode.GetString(ctx.Data.Slice(start, byteLength).ToArray());
                    ctx.Offset = i + 2;
                    return result;
                }
                i += 2;
            }
            // No null terminator found, return empty string and move offset to end
            ctx.Offset = end;
            return string.Empty;
        }


        unsafe static T ReadUnmanaged<T>(ref ParseContext ctx) where T : unmanaged
        {
            checked
            {
                T result = MemoryMarshal.Read<T>(ctx.Data.Slice(ctx.Offset, sizeof(T)));
                ctx.Offset += sizeof(T);
                return result;
            }
        }
    }
}
