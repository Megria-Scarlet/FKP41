using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace FKP41
{
    /// <summary>
    /// 単一の <typeparamref name="T"/> 型のオブジェクトの変更通知をサポートするクラス。
    /// </summary>
    /// <typeparam name="T">任意の型。</typeparam>
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public class ObservableObject<T> : INotifyPropertyChanged
    {
        protected T? value;
        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => value!;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue(value);
        }
        public event PropertyChangedEventHandler? PropertyChanged;


        public ObservableObject(in T value)
        {
            this.value = value;
        }

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SetValue(in T obj)
        {
            if ((!typeof(T).IsValueType && !ReferenceEquals(obj, value)) || !EqualityComparer<T>.Default.Equals(value, obj))
            {
                value = obj;
                NotifyPropertyChanged(nameof(Value));
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValueNonNotification(in T obj)
        {
            value = obj;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T AsValue() => ref value!;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(ObservableObject<T> @object) => @object.value!;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string? ToString() => value is string s ? s : (value?.ToString());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetDebuggerDisplay() => ToString() ?? "null";
    }
}
