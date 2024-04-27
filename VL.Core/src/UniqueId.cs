using MessagePack;
using System;

namespace VL.Core
{
    /// <summary>
    /// A unique and persistent identifier for a patch element. 
    /// It consists of the persistent/serialized id of the element as well as the persistent/serialized id of the document where the element resides in.
    /// </summary>
    [MessagePackObject]
    public readonly struct UniqueId : IEquatable<UniqueId>
    {

        public UniqueId(string documentId, string elementId, uint volatileId = 0)
        {
            DocumentId = documentId ?? throw new ArgumentNullException(nameof(documentId));
            ElementId = elementId ?? throw new ArgumentNullException(nameof(elementId));
            VolatileId = volatileId;
        }

        public static bool TryParse(string value, out UniqueId result)
        {
            var i = value.IndexOf(' ');
            if (i > 0)
            {
                var x = value.Split(' ');
                result = new UniqueId(x[0], x[1]);
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        [Key(0)]
        public string DocumentId { get; }

        [Key(1)]
        public string ElementId { get; }

        /// <summary>
        /// Only valid while the session is running. Used by some obsolete APIs.
        /// </summary>
        [Key(2)]
        public uint VolatileId { get; }

        [IgnoreMember]
        public bool IsDefault => DocumentId is null;

        public bool Equals(UniqueId other)
        {
            return DocumentId == other.DocumentId && ElementId == other.ElementId;
        }

        public override int GetHashCode()
        {
            if (IsDefault)
                return 0;

            return DocumentId.GetHashCode() ^ ElementId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is UniqueId id && Equals(id);
        }

        public override string ToString()
        {
            return $"{DocumentId} {ElementId}";
        }

        public static bool operator ==(UniqueId left, UniqueId right) => left.Equals(right);

        public static bool operator !=(UniqueId left, UniqueId right) => !left.Equals(right);
    }
}
