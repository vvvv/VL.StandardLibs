using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;

namespace VL.Lang
{
    public enum MessageSeverity
    {
        None,
        Debug,
        Info,
        Warning,
        Error
    }

    public enum MessageSource
    {
        Compiler, 
        Runtime
    }

    public class Message : IEquatable<Message>
    {
        public readonly UniqueId Location;
        public readonly MessageSeverity Severity;
        public readonly string What;
        public readonly string Why;
        public readonly string How;
        public readonly string Ignore;
        public readonly bool IsFollowUp;
        public readonly MessageSource Source;
        public readonly DateTime Time = DateTime.Now;
        private bool? flowToParent;

        public Message(MessageSeverity severity, string what, string why = "", string how = "", string ignore = "")
            : this(default(UniqueId), severity, what, why, how, ignore)
        {
        }

        public Message(UniqueId location, MessageSeverity severity, string what, string why = "", string how = "", string ignore = "", bool? flowToParent = default, bool isFollowUp = false, MessageSource source = MessageSource.Compiler)
        {
            Location = location;
            Severity = severity;
            What = what ?? throw new ArgumentNullException(nameof(what));
            Why = why;
            How = how;
            Ignore = ignore;
            this.flowToParent = flowToParent;
            IsFollowUp = isFollowUp;
            Source = source;
        }

        public bool FlowToParent => flowToParent.HasValue ? flowToParent.Value : Severity == MessageSeverity.Error;

        public override string ToString()
        {
            return string.Format("{0}: {1}", Severity, What);
        }

        public override bool Equals(object obj) => Equals(obj as Message);

        public bool Equals(Message other)
        {
            if (ReferenceEquals(other, null)) return false;
            return other.Location.Equals(Location) && other.Severity == Severity && other.What == What;
        }

        public override int GetHashCode()
        {
            return Location.GetHashCode() ^ Severity.GetHashCode() ^ What.GetHashCode();
        }

        public static bool operator ==(Message x, Message y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            return x.Equals(y);
        }

        public static bool operator !=(Message x, Message y)
        {
            return !(x == y);
        }

        public Message WithElementId(UniqueId id)
        {
            return new Message(id, Severity, What, Why, How, Ignore, FlowToParent, IsFollowUp, Source);
        }
    }

    public class Messages
    {
        public static readonly Messages Empty = new Messages(ImmutableHashSet<Message>.Empty);

        private readonly ImmutableHashSet<Message> FMessages;

        private Messages(ImmutableHashSet<Message> messages)
        {
            FMessages = messages;
        }

        public IEnumerable<Message> All 
        { 
            get { return FMessages; } 
        }

        public IEnumerable<Message> Errors
        {
            get { return FMessages.Where(m => m.Severity == MessageSeverity.Error); }
        }

        public IEnumerable<Message> For(UniqueId location)
        {
            return FMessages.Where(m => m.Location.Equals(location));
        }

        public Messages Add(Message message)
        {
            var updated = FMessages.Add(message);
            if (updated != FMessages)
                return new Messages(updated);
            else
                return this;
        }

        public Messages AddRange(IEnumerable<Message> messages)
        {
            var that = this;
            foreach (var message in messages)
                that = that.Add(message);
            return that;
        }

        public Messages AddRange(Messages messages)
        {
            return AddRange(messages.All);
        }
    }

    public static class MessageHelpers
    {
        public static MessageSeverity MaxSeverity(this MessageSeverity a, MessageSeverity b) => (MessageSeverity)Math.Max((sbyte)a, (sbyte)b);
        public static MessageSeverity MaxSeverity(this IEnumerable<Message> messages) => messages.Aggregate(MessageSeverity.None, (acc, m) => acc.MaxSeverity(m.Severity));
        public static MessageSeverity MaxSeverity(this ImmutableArray<Message> messages) => messages.Aggregate(MessageSeverity.None, (acc, m) => acc.MaxSeverity(m.Severity));
    }
}
