namespace ClangAggregator
{
    public struct HeaderWithDll
    {
        public readonly string Header;
        public readonly string Dll;

        public HeaderWithDll(string src)
        {
            var pos = src.LastIndexOf(',');
            if (pos == -1)
            {
                Header = src;
                Dll = default;
            }
            else
            {
                Header = src.Substring(0, pos);
                Dll = src.Substring(pos + 1);
            }
        }
    }
}
