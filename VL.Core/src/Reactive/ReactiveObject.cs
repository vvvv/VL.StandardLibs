using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VL.Lib.Reactive
{
    // Keep it internal as long as it's not used
    internal abstract class ReactiveObject : INotifyPropertyChanged, INotifyPropertyChanging
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public event PropertyChangingEventHandler PropertyChanging;

        protected void SetProperty<T>(ref T backingField, T value, [CallerMemberName] string name = null)
        {
            if (!EqualityComparer<T>.Default.Equals(backingField, value))
            {
                OnPropertyChanging(name);
                backingField = value;
                OnPropertyChanged(name);
            }
        }

        protected virtual void OnPropertyChanging(string propertyName)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
