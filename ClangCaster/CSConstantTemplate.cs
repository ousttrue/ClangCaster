using System.Text;
using ClangAggregator;

namespace ClangCaster
{
    class CSConstantTemplate
    {
        public static string Render(string path, ConstantDefinition constant)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"        // {path}:{constant.Location.Line}");
            sb.AppendLine($"        public const int {constant.Name} = unchecked((int){constant.Value});");

            return sb.ToString();
        }
    }
}
