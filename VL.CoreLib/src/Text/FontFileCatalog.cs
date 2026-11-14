using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VL.Lib.Text
{
    /// <summary>
    /// Finds the fonts in a set of folders by reading the family and style from the tables of
    /// .ttf, .otf and .ttc files. Used where the system offers no font enumeration we can call (macOS, Linux).
    /// </summary>
    internal sealed class FontFileCatalog
    {
        internal readonly record struct Face(string Family, string Path, int Index, bool IsBold, bool IsItalic);

        static readonly Lazy<FontFileCatalog> system = new(() => new FontFileCatalog(GetSystemFontFolders()));

        /// <summary>
        /// The catalog of the fonts installed on this system. Scanned once, on first use.
        /// </summary>
        public static FontFileCatalog System => system.Value;

        public static IReadOnlyList<string> GetSystemFontFolders()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (OperatingSystem.IsMacOS())
                return new[]
                {
                    "/System/Library/Fonts",
                    "/Library/Fonts",
                    string.IsNullOrEmpty(home) ? null : Path.Combine(home, "Library", "Fonts"),
                }.Where(f => f != null).ToArray();

            return new[]
            {
                "/usr/share/fonts",
                "/usr/local/share/fonts",
                string.IsNullOrEmpty(home) ? null : Path.Combine(home, ".fonts"),
                string.IsNullOrEmpty(home) ? null : Path.Combine(home, ".local", "share", "fonts"),
            }.Where(f => f != null).ToArray();
        }

        readonly Face[] faces;
        readonly string[] families;

        /// <param name="folders">The folders to scan, recursively. Missing or unreadable folders are skipped.</param>
        public FontFileCatalog(IEnumerable<string> folders)
        {
            var found = new List<Face>();
            var seenFiles = new HashSet<string>(StringComparer.Ordinal);
            foreach (var folder in folders)
            {
                IEnumerable<string> files;
                try
                {
                    if (!Directory.Exists(folder))
                        continue;
                    files = Directory.EnumerateFiles(folder, "*", new EnumerationOptions
                    {
                        RecurseSubdirectories = true,
                        IgnoreInaccessible = true,
                        MatchCasing = MatchCasing.CaseInsensitive,
                    });
                }
                catch (Exception)
                {
                    continue;
                }

                try
                {
                    foreach (var file in files.OrderBy(f => f, StringComparer.Ordinal))
                    {
                        if (!IsFontFile(file) || !seenFiles.Add(file))
                            continue;
                        ReadFaces(file, found);
                    }
                }
                catch (Exception)
                {
                    // Folder vanished or became unreadable while enumerating, keep what we have
                }
            }

            faces = found.ToArray();
            families = faces
                .Select(f => f.Family)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(f => f, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
        }

        public IReadOnlyList<string> Families => families;

        public IReadOnlyList<Face> Faces => faces;

        /// <summary>
        /// The face of the family which fits the style best, or null if the family is unknown.
        /// </summary>
        public Face? Find(string familyName, FontStyle style)
        {
            if (string.IsNullOrEmpty(familyName))
                return null;

            var wantBold = (style & FontStyle.Bold) != 0;
            var wantItalic = (style & FontStyle.Italic) != 0;

            Face? best = null;
            var bestScore = int.MaxValue;
            foreach (var face in faces)
            {
                if (!string.Equals(face.Family, familyName, StringComparison.OrdinalIgnoreCase))
                    continue;
                var score = (face.IsBold != wantBold ? 1 : 0) + (face.IsItalic != wantItalic ? 1 : 0);
                if (score < bestScore)
                {
                    best = face;
                    bestScore = score;
                    if (score == 0)
                        break;
                }
            }
            return best;
        }

        static bool IsFontFile(string file)
        {
            var ext = Path.GetExtension(file);
            return ext.Equals(".ttf", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".otf", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".ttc", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".otc", StringComparison.OrdinalIgnoreCase);
        }

        static void ReadFaces(string file, List<Face> result)
        {
            try
            {
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                ReadFaces(stream, file, result);
            }
            catch (Exception)
            {
                // Broken, truncated or unreadable file - not a font we can offer
            }
        }

        static void ReadFaces(FileStream stream, string file, List<Face> result)
        {
            Span<byte> header = stackalloc byte[12];
            ReadExactly(stream, 0, header);
            var tag = BinaryPrimitives.ReadUInt32BigEndian(header);

            var offsets = new List<uint>();
            if (tag == 0x74746366) // 'ttcf'
            {
                var count = BinaryPrimitives.ReadUInt32BigEndian(header.Slice(8));
                if (count == 0 || count > 1024)
                    return;
                Span<byte> offsetBytes = stackalloc byte[4];
                for (int i = 0; i < count; i++)
                {
                    ReadExactly(stream, 12 + 4L * i, offsetBytes);
                    offsets.Add(BinaryPrimitives.ReadUInt32BigEndian(offsetBytes));
                }
            }
            else
                offsets.Add(0);

            for (int i = 0; i < offsets.Count; i++)
            {
                try
                {
                    var face = ReadFace(stream, file, i, offsets[i]);
                    if (face.HasValue)
                        result.Add(face.Value);
                }
                catch (Exception)
                {
                    // Skip this face, the others in the collection may be fine
                }
            }
        }

        static Face? ReadFace(FileStream stream, string file, int index, long offset)
        {
            Span<byte> header = stackalloc byte[12];
            ReadExactly(stream, offset, header);
            var version = BinaryPrimitives.ReadUInt32BigEndian(header);
            // 0x00010000, 'OTTO', 'true', 'typ1'
            if (version != 0x00010000 && version != 0x4F54544F && version != 0x74727565 && version != 0x74797031)
                return null;

            var numTables = BinaryPrimitives.ReadUInt16BigEndian(header.Slice(4));
            var directory = new byte[numTables * 16];
            ReadExactly(stream, offset + 12, directory);

            long nameOffset = 0, os2Offset = 0, headOffset = 0;
            uint nameLength = 0;
            for (int i = 0; i < numTables; i++)
            {
                var record = directory.AsSpan(i * 16, 16);
                var recordTag = Encoding.ASCII.GetString(record.Slice(0, 4));
                var tableOffset = BinaryPrimitives.ReadUInt32BigEndian(record.Slice(8));
                var tableLength = BinaryPrimitives.ReadUInt32BigEndian(record.Slice(12));
                switch (recordTag)
                {
                    case "name":
                        nameOffset = tableOffset;
                        nameLength = tableLength;
                        break;
                    case "OS/2":
                        os2Offset = tableOffset;
                        break;
                    case "head":
                        headOffset = tableOffset;
                        break;
                }
            }

            if (nameOffset == 0 || nameLength < 6 || nameLength > 4 * 1024 * 1024)
                return null;

            var nameTable = new byte[nameLength];
            ReadExactly(stream, nameOffset, nameTable);
            var family = ReadName(nameTable, 1);
            if (string.IsNullOrWhiteSpace(family))
                return null;
            var subFamily = ReadName(nameTable, 2);

            bool? bold = null, italic = null;
            if (os2Offset != 0)
            {
                // fsSelection at 62: bit 0 italic, bit 5 bold. Version 0 tables are 78 bytes, fsSelection is there too.
                Span<byte> fsSelection = stackalloc byte[2];
                if (TryReadExactly(stream, os2Offset + 62, fsSelection))
                {
                    var bits = BinaryPrimitives.ReadUInt16BigEndian(fsSelection);
                    italic = (bits & 1) != 0;
                    bold = (bits & (1 << 5)) != 0;
                }
            }
            if (bold is null && headOffset != 0)
            {
                // macStyle at 44: bit 0 bold, bit 1 italic
                Span<byte> macStyle = stackalloc byte[2];
                if (TryReadExactly(stream, headOffset + 44, macStyle))
                {
                    var bits = BinaryPrimitives.ReadUInt16BigEndian(macStyle);
                    bold = (bits & 1) != 0;
                    italic = (bits & 2) != 0;
                }
            }
            if (bold is null)
            {
                bold = subFamily != null && subFamily.Contains("Bold", StringComparison.OrdinalIgnoreCase);
                italic = subFamily != null && (subFamily.Contains("Italic", StringComparison.OrdinalIgnoreCase) || subFamily.Contains("Oblique", StringComparison.OrdinalIgnoreCase));
            }

            return new Face(family.Trim(), file, index, bold.Value, italic.Value);
        }

        static bool TryReadExactly(Stream stream, long position, Span<byte> buffer)
        {
            try
            {
                ReadExactly(stream, position, buffer);
                return true;
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        static void ReadExactly(Stream stream, long position, Span<byte> buffer)
        {
            stream.Position = position;
            stream.ReadExactly(buffer);
        }

        /// <summary>
        /// Reads a string of the name table, preferring English names in Unicode encodings over Mac Roman.
        /// </summary>
        static string ReadName(byte[] table, int nameId)
        {
            var count = BinaryPrimitives.ReadUInt16BigEndian(table.AsSpan(2));
            var stringOffset = BinaryPrimitives.ReadUInt16BigEndian(table.AsSpan(4));

            string best = null;
            var bestRank = int.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var pos = 6 + i * 12;
                if (pos + 12 > table.Length)
                    break;
                var record = table.AsSpan(pos, 12);
                if (BinaryPrimitives.ReadUInt16BigEndian(record.Slice(6)) != nameId)
                    continue;

                var platform = BinaryPrimitives.ReadUInt16BigEndian(record);
                var language = BinaryPrimitives.ReadUInt16BigEndian(record.Slice(4));
                var length = BinaryPrimitives.ReadUInt16BigEndian(record.Slice(8));
                var offset = BinaryPrimitives.ReadUInt16BigEndian(record.Slice(10));
                var start = stringOffset + offset;
                if (start + length > table.Length)
                    continue;

                int rank;
                if (platform == 3 && language == 0x0409) rank = 0;
                else if (platform == 3) rank = 2;
                else if (platform == 0) rank = 3;
                else if (platform == 1 && language == 0) rank = 1;
                else continue;

                if (rank >= bestRank)
                    continue;

                var bytes = table.AsSpan(start, length);
                best = platform == 1 ? Encoding.Latin1.GetString(bytes) : Encoding.BigEndianUnicode.GetString(bytes);
                bestRank = rank;
            }
            return best;
        }
    }
}
