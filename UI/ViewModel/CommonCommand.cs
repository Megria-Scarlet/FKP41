using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace FKP41.WPF
{
    public class CommonCommand : System.Windows.Input.ICommand, IDisposable
    {
        protected Func<object?, bool>? canExecuteFunc;
        protected Action<object?>? executeAction;
        protected bool isDisposed;
        public bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => isDisposed;
        }

        public event EventHandler? CanExecuteChanged;

        public CommonCommand([System.Diagnostics.CodeAnalysis.AllowNull] Action executeAction)
        {
            this.canExecuteFunc = null;
            this.executeAction = sender => executeAction?.Invoke();
        }
        public CommonCommand([System.Diagnostics.CodeAnalysis.AllowNull] Action<object?> executeAction) : this(null, executeAction)
        {

        }

        public CommonCommand([System.Diagnostics.CodeAnalysis.AllowNull] Func<object?, bool> canExecuteFunc, [System.Diagnostics.CodeAnalysis.AllowNull] Action<object?> executeAction)
        {
            this.canExecuteFunc = canExecuteFunc;
            this.executeAction = executeAction;
        }

        public bool CanExecute(object? parameter)
        {
            return canExecuteFunc is null || canExecuteFunc.Invoke(parameter);
        }

        public void Execute(object? parameter)
        {
            executeAction?.Invoke(parameter);
        }

        #region Dispose

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)
                    CanExecuteChanged = null;
                    canExecuteFunc = null;
                    executeAction = null;
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                isDisposed = true;
            }
        }

        // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~CommonCommand()
        // {
        //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
