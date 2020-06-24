using System;
using System.Collections.Generic;
using System.Linq;
using ClangAggregator.Types;

namespace ClangAggregator
{
    /// <summary>
    /// ひとつのソースに属するエクスポート対象の型を保持する
    /// </summary>
    public class ExportSource
    {
        readonly NormalizedFilePath m_path;

        public readonly string Dll;

        readonly List<TypeReference> m_enumTypes = new List<TypeReference>();
        public IEnumerable<TypeReference> EnumTypes => m_enumTypes;

        readonly List<TypeReference> m_structTypes = new List<TypeReference>();
        public IEnumerable<TypeReference> StructTypes => m_structTypes;

        readonly List<TypeReference> m_functionTypes = new List<TypeReference>();
        public IEnumerable<TypeReference> FunctionTypes => m_functionTypes;

        readonly List<TypeReference> m_typedefTypes = new List<TypeReference>();
        public IEnumerable<TypeReference> TypedefTypes => m_typedefTypes;

        readonly List<TypeReference> m_interfaces = new List<TypeReference>();
        public IEnumerable<TypeReference> Interfaces => m_interfaces;

        public readonly Dictionary<string, List<ConstantDefinition>> ConstantMap = new Dictionary<string, List<ConstantDefinition>>();

        public ExportSource(NormalizedFilePath path, string dll)
        {
            m_path = path;
            if (!string.IsNullOrEmpty(dll) && dll.ToLower().EndsWith(".dll"))
            {
                // remove extension
                dll = dll.Substring(0, dll.Length - 4);
            }
            Dll = dll;
        }

        public ExportSource(string path, string dll) : this(new NormalizedFilePath(path), dll)
        {
        }

        public override string ToString()
        {
            return $"{m_path} ({m_enumTypes.Count}types)";
        }

        public void PushConstant(string prefix, ConstantDefinition constant)
        {
            if (!ConstantMap.TryGetValue(prefix, out List<ConstantDefinition> list))
            {
                list = new List<ConstantDefinition>();
                ConstantMap.Add(prefix, list);
            }
            list.Add(constant);
        }

        static int s_anonymous;

        static bool IsExportFunction(FunctionType functionType)
        {
            if (functionType.DllExport)
            {
                return true;
            }

            if (!functionType.HasBody)
            {
                // とりあえず
                return true;
            }

            return false;
        }

        public void Push(TypeReference reference)
        {
            var type = reference.Type;
            if (type is EnumType enumType)
            {
                if (m_enumTypes.Any(x => x.Hash == reference.Hash))
                {
                    return;
                }

                // enum値名の重複する部分を除去する
                enumType.PreparePrefix();

                m_enumTypes.Add(reference);
            }
            else if (type is StructType structType)
            {
                if (structType.IID != default)
                {
                    // COM interface
                    if (m_interfaces.Any(x => x.Hash == reference.Hash))
                    {
                        return;
                    }

                    m_interfaces.Add(reference);
                }
                else
                {
                    // struct
                    if (m_structTypes.Any(x => x.Hash == reference.Hash))
                    {
                        return;
                    }

                    if (string.IsNullOrEmpty(structType.Name))
                    {
                        // 無名型に名前を付ける(unionによくある)
                        structType.Name = $"__Anonymous__{s_anonymous++}";
                    }

                    m_structTypes.Add(reference);
                }
            }
            else if (type is FunctionType functionType)
            {
                if (!IsExportFunction(functionType))
                {
                    return;
                }
                if (m_functionTypes.Any(x => x.Hash == reference.Hash))
                {
                    return;
                }
                m_functionTypes.Add(reference);
            }
            else if (type is TypedefType typedefType)
            {
                if (m_typedefTypes.Any(x => x.Hash == reference.Hash))
                {
                    return;
                }
                m_typedefTypes.Add(reference);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
