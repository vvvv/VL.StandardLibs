using System;

namespace VL.Core.Diagnostics
{
    public class Message : IEquatable<Message>
    {
        public Message(MessageType type, string text)
        {
            Type = type;
            Text = text;
        }

        public MessageType Type { get; }

        public string Text { get; }

        public bool Equals(Message other)
        {
            if (other is null)
                return false;
            return Type == other.Type && Text == other.Text;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Message);
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode() ^ Text.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Type}: {Text}";
        }
    }
}
