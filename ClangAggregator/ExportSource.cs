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

        public readonly List<ConstantDefinition> Constants = new List<ConstantDefinition>();

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

        public void PushConstant(ConstantDefinition constant)
        {
            if (Constants.Any(x => x.Name == constant.Name))
            {
                Console.WriteLine($"duplicated: {constant.Location} => {constant}");
                return;
            }

            Constants.Add(constant);
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

        public bool Push(TypeReference reference)
        {
            var type = reference.Type;
            if (type is EnumType enumType)
            {
                if (m_enumTypes.Any(x => x.Hash == reference.Hash))
                {
                    return false;
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
                        return false;
                    }

                    m_interfaces.Add(reference);
                }
                else
                {
                    // struct
                    if (m_structTypes.Any(x => x.Hash == reference.Hash))
                    {
                        return false;
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
                    return false;
                }
                if (m_functionTypes.Any(x => x.Hash == reference.Hash))
                {
                    return false;
                }
                m_functionTypes.Add(reference);
            }
            else if (type is TypedefType typedefType)
            {
                if (m_typedefTypes.Any(x => x.Hash == reference.Hash))
                {
                    return false;
                }
                if (typedefType.Ref.Type is PointerType pointerType)
                {
                    if (pointerType.Pointee.Type is FunctionType ft)
                    {
                        ft.IsDelegate = true;
                    }
                }
                m_typedefTypes.Add(reference);
            }
            else
            {
                throw new NotImplementedException();
            }
            return true;
        }
    }
}
