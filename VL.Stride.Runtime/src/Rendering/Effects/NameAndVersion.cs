namespace VL.Stride.Rendering
{
    public record struct NameAndVersion(string NamePart, string VersionPart = null)
    {
        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(VersionPart))
                return $"{NamePart} ({VersionPart})";
            return NamePart;
        }

        public static implicit operator string(NameAndVersion nameAndVersion) => nameAndVersion.ToString();
    }
}
