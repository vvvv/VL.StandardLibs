using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using VL.Core;

namespace VL.Lib.Collections
{
    public interface IDynamicEnum
    {
        /// <summary>
        /// Gets the current enum value as string
        /// </summary>
        string Value { get; }

        /// <summary>
        /// Gets the associated tag if the enum definition has registered one.
        /// </summary>
        object Tag { get; }

        /// <summary>
        /// Creates a new enum value with the same type as the input instance
        /// </summary>
        IDynamicEnum CreateValue(string value);

        /// <summary>
        /// Creates the default enum value with the same type as the input instance
        /// </summary>
        IDynamicEnum Default { get; }

        /// <summary>
        /// Gets the definition of this enum with all entries
        /// </summary>
        IDynamicEnumDefinition Definition { get; }
    }

    public interface IDynamicEnumDefinition
    {
        /// <summary>
        /// Fires when the definition changes, i.e. entries get added or removed
        /// </summary>
        IObservable<IReadOnlyList<string>> OnChange { get; }

        /// <summary>
        /// Gets the current list of valid entries
        /// </summary>
        IReadOnlyList<string> Entries { get; }

        /// <summary>
        /// Returns true if the string is a valid entry of this enum type
        /// </summary>
        bool IsValid(string entry);

        /// <summary>
        /// Gets the empty enum fallback string for cases when no entries are in the enum definition.
        /// </summary>
        string EmptyEnumFallbackMessage { get; }
    }

    public static class DynamicEnumExtensions
    {
        /// <summary>
        /// Returns true if the value is in the current entry list of the definition.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the specified input is valid, not null and its value is not a null or empty string; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValid(this IDynamicEnum input)
        {
            if (input == null || string.IsNullOrEmpty(input.Value))
                return false;

            return input.Definition.IsValid(input.Value);
        }

        public static bool IsEmptyFallback(this IDynamicEnum input)
        {
            if (input == null || string.IsNullOrEmpty(input.Value))
                return false;

            return input.Definition.EmptyEnumFallbackMessage == input.Value;
        }

        /// <summary>
        /// Gets the index of the selected item in the entries list of its definition.
        /// Can return -1 if the string is not in the current list of entries.
        /// </summary>
        public static int SelectedIndex(this IDynamicEnum input)
        {
            var entries = input.Definition.Entries;

            var list = entries as IList<string>;
            if(list != null)
            {
                return list.IndexOf(input.Value);
            }

            var index = -1;
            var i = 0;
            foreach (var item in entries)
            {
                if(item == input.Value)
                {
                    index = i;
                    break;
                }
                i++;
            }

            return index;
        }

        /// <summary>
        /// Creates a new enum value of given type
        /// </summary>
        public static TEnum CreateValue<TEnum>(this TEnum input, string value) where TEnum : IDynamicEnum
        {
            return (TEnum)input.CreateValue(value);
        }

        /// <summary>
        /// Sets the selected item to the value at the index in the entries list of its definition.
        /// If the index is out of range, returns false and the input value.
        /// </summary>
        public static void TrySelectIndex<TEnum>(this TEnum input, int index, out bool success, out TEnum result) where TEnum : IDynamicEnum
        {
            var entryCount = input.Definition.Entries.Count;
            success = entryCount > 0 && index >= 0 && index < entryCount;
            if (success)
            {
                result = (TEnum)input.CreateValue(input.Definition.Entries[index]);
            }
            else
            {
                result = input;
            }

        }

        public static IDynamicEnumDefinition GetDynamicEnumDefinition(Type type)
        {
            if (!typeof(IDynamicEnum).IsAssignableFrom(type))
                return null;

            var baseType = type.BaseType;
            if (baseType is null || !baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(DynamicEnumBase<,>))
                return null;

            var definitionType = baseType.GenericTypeArguments[1];
            var property = definitionType.GetProperty(nameof(DynamicEnumDefinitionBase<DummyDynamicEnumDefinition>.Instance), BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            return (IDynamicEnumDefinition)property.GetValue(null);
        }

        public static IDynamicEnum CreateEnumValue(Type type, string value)
        {
            var ctor = type.GetConstructor(new[] { typeof(string) });
            if (ctor != null)
                return (IDynamicEnum)ctor.Invoke(new[] { value } );
            return null;
        }
    }

    /// <summary>
    /// Base class for easy dynamic enum implementaion. Use like this:
    /// MyEnumClass : DynamicEnumBase&lt;MyEnumClass&gt; and override 
    /// IDynamicEnumDefinition Definition { get; } and define a default value:
    /// public static MidiInputDevice Default => new MyEnumClass("Default Entry");
    /// <typeparam name="TSubclass">The type of the actual dynamic enum class.</typeparam>
    /// <typeparam name="TDefinitionClass">The type of the enum definition.</typeparam>
    /// <seealso cref="VL.Lib.Collections.IDynamicEnum" />
    /// </summary>
    [Serializable]
    public abstract class DynamicEnumBase<TSubclass, TDefinitionClass> : IDynamicEnum
        where TSubclass : DynamicEnumBase<TSubclass, TDefinitionClass>
        where TDefinitionClass : DynamicEnumDefinitionBase<TDefinitionClass>, new()
    {
        public DynamicEnumBase(string value)
        {
            FValue = value;
        }

        static DynamicEnumDefinitionBase<TDefinitionClass> DefinitionInstance => DynamicEnumDefinitionBase<TDefinitionClass>.Instance;
        private readonly string FValue;

        //IDynamicEnum interface implementation
        public string Value => FValue;
        public object Tag => DefinitionInstance.GetTag(FValue);
        public IDynamicEnum CreateValue(string value) => Create(value);

        public IDynamicEnum Default
        {
            get
            {
                if (CreateDefaultInfo != null)
                    return (IDynamicEnum)CreateDefaultInfo.Invoke(null, null);
                else
                    return CreateDefaultBase();
            }
        }

        public IDynamicEnumDefinition Definition => DefinitionInstance;

        public static TSubclass Create(string value)
        {
            var obj = FormatterServices.GetUninitializedObject(typeof(TSubclass));
            FormatterServices.PopulateObjectMembers(obj, MembersToInit, new object[] { value });
            return (TSubclass)obj;
        }

        /// <summary>
        /// Can be used in subclass to create the default, selects the first entry.
        /// </summary>
        public static TSubclass CreateDefaultBase(string emptyEnumFallbackMessage = "")
        {
            TSubclass result;
            var definition = DefinitionInstance;
            if (definition.Entries.Count > 0)
            {
                result = Create(definition.Entries[0]);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(emptyEnumFallbackMessage))
                    DefinitionInstance.EmptyEnumFallbackMessage = "No entries found for " + typeof(TSubclass).Name;
                else
                    DefinitionInstance.EmptyEnumFallbackMessage = emptyEnumFallbackMessage;

                result = Create(DefinitionInstance.EmptyEnumFallbackMessage);
            }

            return result;
        }

        static MemberInfo[] FMembersToInit;
        static MemberInfo[] MembersToInit
        {
            get
            {
                if(FMembersToInit == null)
                {
                    var subClassFieldInfo = FormatterServices.GetSerializableMembers(typeof(TSubclass))
                        .OfType<FieldInfo>().FirstOrDefault(fi => fi.Name.Contains("FValue"));
                    FMembersToInit = new MemberInfo[] { subClassFieldInfo };
                }

                return FMembersToInit;
            }
        }

        static bool FCreateDefaultInfoInit;
        static MethodInfo FCreateDefaultInfo;
        static MethodInfo CreateDefaultInfo
        {
            get
            {
                if (!FCreateDefaultInfoInit)
                {
                    FCreateDefaultInfo = typeof(TSubclass).GetMethod("CreateDefault", BindingFlags.Public | BindingFlags.Static);
                    FCreateDefaultInfoInit = true;
                }

                return FCreateDefaultInfo;
            }
        }

        // override object.Equals
        public override bool Equals(object obj)
        {         
            if (obj == null)
                return false;

            var asEnum = obj as DynamicEnumBase<TSubclass, TDefinitionClass>;
            if(asEnum == null)            
                return false;

            //only FValue is relevant
            if (FValue.Equals(asEnum.FValue))
            {
                if (Tag != null)
                    return Tag.Equals(asEnum.Tag);
                else
                    return asEnum.Tag == null;
            }
            return false;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            int hashCode = 0;
            unchecked
            {
                hashCode += 1000000007 * FValue.GetHashCode();
                hashCode += 1000000009 * Tag?.GetHashCode() ?? 1;
            }
            return hashCode;
        }

        public override string ToString() => Value;

    }

    public struct DynamicEnumDefinitionChangedInfo
    {
        public Type EnumType;
    }

    /// <summary>
    /// Static class used to inform the compiler that a dynamic enum definition has changed
    /// </summary>
    public static class AnyDynamicEnumDefinitionChanged
    {
        static Subject<DynamicEnumDefinitionChangedInfo> Subject = new Subject<DynamicEnumDefinitionChangedInfo>();
        public static IObservable<DynamicEnumDefinitionChangedInfo> EnumDefinitionChanged => Subject;
        public static void SendEnumDefinitionChanged(Type t)
            => Subject.OnNext(new DynamicEnumDefinitionChangedInfo() { EnumType = t });
    }

    /// <summary>
    /// Base class for dynamic enum definitions.
    /// Takes care of the singleton pattern and the update of the entries. Use like this:
    /// MyEnumDefinitionClass : DynamicEnumBase&lt;MyEnumDefinitionClass&gt; and override the two abstract methods.
    /// <typeparam name="TDefinitionSubclass">The type of the actual definition class.</typeparam>
    /// <seealso cref="VL.Lib.Collections.IDynamicEnumDefinition" />
    /// </summary>
    public abstract class DynamicEnumDefinitionBase<TDefinitionSubclass> : IDynamicEnumDefinition
       where TDefinitionSubclass : DynamicEnumDefinitionBase<TDefinitionSubclass>, new()
    {
        //singleton pattern

        public static TDefinitionSubclass Instance { get; } = new TDefinitionSubclass();

        public DynamicEnumDefinitionBase()
        {
            InternalInitialize();
        }
             
        //IDynamicEnumDefinition interface implementation
        public IReadOnlyList<string> Entries { get; private set; }
        public IObservable<IReadOnlyList<string>> OnChange { get; private set; }
        public bool IsValid(string entry) => FEntriesLookup.ContainsKey(entry);
        public object GetTag(string entry)
        {
            object result;
            FEntriesLookup.TryGetValue(entry, out result);
            return result;
        }

        protected virtual bool AutoSortAlphabetically
        {
            get;
            set;
        }

        IReadOnlyDictionary<string, object> FEntriesLookup;

        protected virtual void Initialize() { }
        private void InternalInitialize()
        {
            AutoSortAlphabetically = true;
            Initialize();

            OnChange = GetEntriesChangedObservable()
                .Select(_ => SetNewEntries())
                .Publish()
                .RefCount();

            //make sure there is at least one subscription to make sure 
            //the side effect "SetEntries" gets called and inform the compiler about a change to add or remove errors
            OnChange
                .Subscribe(_ =>AnyDynamicEnumDefinitionChanged.SendEnumDefinitionChanged(this.GetType()))
                .DisposeBy(AppHost.Global);

            SetNewEntries();
        }

        public string EmptyEnumFallbackMessage
        {
            get;
            internal set;
        }

        //needs to be provided by the derived class
        protected abstract IReadOnlyDictionary<string, object> GetEntries();
        protected abstract IObservable<object> GetEntriesChangedObservable();

        //actual work
        private IReadOnlyList<string> SetNewEntries()
        {
            var map = GetEntriesSafe();
            IReadOnlyList<string> entries;

            //set entries and return result
            if (AutoSortAlphabetically)
                entries = map.Keys.OrderBy(e => e, StringComparer.OrdinalIgnoreCase).ToList();
            else
                entries = map.Keys.ToList();

            FEntriesLookup = map;
            return Entries = entries;

            IReadOnlyDictionary<string, object> GetEntriesSafe()
            {
                try
                {
                    return GetEntries();
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                    return new Dictionary<string, object>();
                }
            }
        }        
    }

    public abstract class ManualDynamicEnumDefinitionBase<TDefinitionSubclass> : DynamicEnumDefinitionBase<TDefinitionSubclass>
       where TDefinitionSubclass : ManualDynamicEnumDefinitionBase<TDefinitionSubclass>, new()
    {
        Subject<object> FEntriesChanged = new Subject<object>();
        Dictionary<string, object> FEntries = new Dictionary<string, object>();
        bool FBatchUpdate;

        //return the current enum entries
        protected override IReadOnlyDictionary<string, object> GetEntries()
        {
            return FEntries;
        }

        //inform the system that the enum has changed
        protected override IObservable<object> GetEntriesChangedObservable()
        {
            return FEntriesChanged;
        }

        public void BeginUpdate()
        {
            FBatchUpdate = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SendIfNotBatchUpdate()
        {
            if (!FBatchUpdate)
                FEntriesChanged.OnNext(new object());
        }

        public void EndUpdate()
        {
            if (FBatchUpdate)
                FEntriesChanged.OnNext(new object());

            FBatchUpdate = false;
        }

        public void AddEntry(string entry, object tag)
        {
            FEntries.Add(entry, tag);

            SendIfNotBatchUpdate();
        }

        public bool RemoveEntry(string entry)
        {
            var result = FEntries.Remove(entry);

            SendIfNotBatchUpdate();

            return result;
        }

        public bool ContainsEntry(string entry)
        {
            return FEntries.ContainsKey(entry);
        }

        public void Clear()
        {
            FEntries.Clear();

            SendIfNotBatchUpdate();
        }     
    }

    public class DummyDynamicEnumDefinition : ManualDynamicEnumDefinitionBase<DummyDynamicEnumDefinition>
    {
        public static new DummyDynamicEnumDefinition Instance => ManualDynamicEnumDefinitionBase<DummyDynamicEnumDefinition>.Instance;
    }

    [Serializable]
    public class DummyDynamicEnumEnum : DynamicEnumBase<DummyDynamicEnumEnum, DummyDynamicEnumDefinition>
    {
        public DummyDynamicEnumEnum(string value) : base(value) { }

        public static DummyDynamicEnumEnum CreateDefault()
        {
            return CreateDefaultBase("Dummy Dynamic Enum");
        }
    }
}
