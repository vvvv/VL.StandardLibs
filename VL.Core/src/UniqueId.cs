using System;
using System.ComponentModel;

namespace VL.Core
{
    /// <summary>
    /// A unique and persistent identifier for a patch element. 
    /// It consists of the persistent/serialized id of the element as well as the persistent/serialized id of the document where the element resides in.
    /// </summary>
    public readonly struct UniqueId : IEquatable<UniqueId>
    {
        readonly string documentId;
        readonly string elementId;
        readonly uint volatileId;

        public UniqueId(string documentId, string elementId, uint volatileId = 0)
        {
            this.documentId = documentId ?? throw new ArgumentNullException(nameof(documentId));
            this.elementId = elementId ?? throw new ArgumentNullException(nameof(elementId));
            this.volatileId = volatileId;
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

        public string DocumentId => documentId;

        public string ElementId => elementId;

        /// <summary>
        /// Only valid while the session is running. Used by some obsolete APIs.
        /// </summary>
        internal uint VolatileId => volatileId;

        public bool IsDefault => documentId is null;

        public bool Equals(UniqueId other)
        {
            return documentId == other.documentId && elementId == other.elementId;
        }

        public override int GetHashCode()
        {
            if (IsDefault)
                return 0;

            return documentId.GetHashCode() ^ elementId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is UniqueId id && Equals(id);
        }

        public override string ToString()
        {
            return $"{documentId} {elementId}";
        }

        public static bool operator ==(UniqueId left, UniqueId right) => left.Equals(right);

        public static bool operator !=(UniqueId left, UniqueId right) => !left.Equals(right);
    }
}
