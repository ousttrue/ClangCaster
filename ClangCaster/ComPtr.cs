namespace ClangCaster
{
    static class ComPtr
    {
        public static string[] Using = new string[]
        {
            "System",
            "System.Runtime.InteropServices",
        };

        public const string Source = @"
    /// <summary>
    /// COMの virtual function table を自前で呼び出すヘルパークラス。
    /// RCW は、うまくいかなかった。
    /// </summary>
    public abstract class ComPtr : IDisposable
    {
        static Guid s_uuid;
        public static ref Guid IID => ref s_uuid;
        public virtual ref Guid GetIID(){ return ref s_uuid; }
 
        /// <summay>
        /// IUnknown を継承した interface(ID3D11Deviceなど) に対するポインター。
        /// このポインターの指す領域の先頭に virtual function table へのポインタが格納されている。
        /// <summay>
        protected IntPtr m_ptr;

        /// <summary>
        /// 初期化に、 void** が要求された場合に使う
        /// </summary>
        /// <value></value>
        public ref IntPtr NewPtr
        {
            get
            {
                if (m_ptr != IntPtr.Zero)
                {
                    Marshal.Release(m_ptr);
                }
                return ref m_ptr;
            }
        }

        public ref IntPtr Ptr => ref m_ptr;

        public static implicit operator bool(ComPtr i)
        {
            if (i is null) return false;
            return i.m_ptr != IntPtr.Zero;
        }

        IntPtr VTable => Marshal.ReadIntPtr(m_ptr);

        static readonly int IntPtrSize = Marshal.SizeOf(typeof(IntPtr));

        protected IntPtr GetFunctionPointer(int index)
        {
            return Marshal.ReadIntPtr(VTable, index * IntPtrSize);
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。
                if (m_ptr != IntPtr.Zero)
                {
                    Marshal.Release(m_ptr);
                    m_ptr = IntPtr.Zero;
                }

                disposedValue = true;
            }
        }

        ~ComPtr()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(false);
        }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
";

    }
}
