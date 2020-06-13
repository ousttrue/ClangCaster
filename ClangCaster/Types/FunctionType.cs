using libclang;

namespace ClangCaster.Types
{
    public class FunctionType : UserType
    {
        public bool IsVariadic;

        public bool HasBody;

        public FunctionType((uint, ClangLocation, string) args) : base(args)
        {
        }

        public override string ToString()
        {
            return $"{Name}();";
        }
    }
}
