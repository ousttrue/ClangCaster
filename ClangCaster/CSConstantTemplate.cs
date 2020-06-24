using System.Text;
using ClangAggregator;

namespace ClangCaster
{
    class CSConstantTemplate
    {
        public static string Render(ConstantDefinition constant)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"        // {constant.Location.Path.Path}:{constant.Location.Line}");
            sb.AppendLine($"        public const int {constant.Name} = unchecked((int){constant.Value});");

            return sb.ToString();
        }
    }
}
