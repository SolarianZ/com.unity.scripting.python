using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using UObject = UnityEngine.Object;

namespace UnityEditor.Scripting.Python
{
    // 从C# API生成Python类型存根以支持类型检查和自动补全
    public static class PythonStubsGenerator
    {
        public const string OutputDirectory = "Library/PythonScripting/stubs";

        // C#到Python的类型映射
        private static readonly Dictionary<Type, string> _typeMapping = new Dictionary<Type, string>
        {
            { typeof(void), "None" },
            { typeof(bool), "bool" },
            { typeof(byte), "int" },
            { typeof(sbyte), "int" },
            { typeof(short), "int" },
            { typeof(ushort), "int" },
            { typeof(int), "int" },
            { typeof(uint), "int" },
            { typeof(long), "int" },
            { typeof(ulong), "int" },
            { typeof(float), "float" },
            { typeof(double), "float" },
            { typeof(decimal), "float" },
            { typeof(string), "str" },
            { typeof(char), "str" },
            { typeof(object), "Any" },
            { typeof(DateTime), "datetime" },
        };

        // C#的运算符重载应该映射到Python的魔术方法
        private static readonly Dictionary<string, string> _operatorMapping = new Dictionary<string, string>
        {
            { "op_Addition", "__add__" },
            { "op_Subtraction", "__sub__" },
            { "op_Multiply", "__mul__" },
            { "op_Division", "__truediv__" },
            { "op_Modulus", "__mod__" },
            { "op_Equality", "__eq__" },
            { "op_Inequality", "__ne__" },
            { "op_LessThan", "__lt__" },
            { "op_GreaterThan", "__gt__" },
            { "op_LessThanOrEqual", "__le__" },
            { "op_GreaterThanOrEqual", "__ge__" },
            { "op_UnaryNegation", "__neg__" },
            { "op_UnaryPlus", "__pos__" },
            { "op_LogicalNot", "__invert__" },
            { "op_BitwiseAnd", "__and__" },
            { "op_BitwiseOr", "__or__" },
            { "op_ExclusiveOr", "__xor__" },
            { "op_LeftShift", "__lshift__" },
            { "op_RightShift", "__rshift__" },
            { "op_Implicit", "__init__" }, // 隐式转换
            { "op_Explicit", "__init__" } // 显式转换
        };

        // Python关键字列表
        private static readonly HashSet<string> _pythonKeywords = new HashSet<string>
        {
            "and", "as", "assert", "async", "await", "break", "class", "continue",
            "def", "del", "elif", "else", "except", "finally", "for", "from",
            "global", "if", "import", "in", "is", "lambda", "nonlocal", "not",
            "or", "pass", "raise", "return", "try", "while", "with", "yield",
            "None", "True", "False", "__import__", "__name__", "__doc__"
        };


        [MenuItem("Tools/Python Scripting/Re-Generate Stubs")]
        public static void GenerateStubs()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Generating Python Stubs", "Preparing...", 0);

                if (Directory.Exists(OutputDirectory))
                    Directory.Delete(OutputDirectory, true);
                Directory.CreateDirectory(OutputDirectory);

                // 获取所有需要生成存根的程序集
                HashSet<Assembly> assemblies = GetTargetAssemblies();
                int current = 0;
                foreach (Assembly assembly in assemblies)
                {
                    EditorUtility.DisplayProgressBar("Generating Python Stubs",
                        assembly.GetName().Name, (float)current / assemblies.Count);

                    GenerateAssemblyStub(assembly, OutputDirectory);
                    current++;
                }

                // 生成__init__.py文件
                GenerateInitFiles(OutputDirectory);

                // 生成readme.md文件
                GenerateReadmeFile(OutputDirectory);

                Debug.Log($"Python stubs generated successfully at: {Path.GetFullPath(OutputDirectory).Replace("\\", "/")}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Python stub generation failed: {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Tools/Python Scripting/Open Stubs Directory")]
        public static void OpenStubsDirectory()
        {
            if (Directory.Exists(OutputDirectory))
                EditorUtility.OpenWithDefaultApp(OutputDirectory);
            else
                EditorUtility.DisplayDialog("Notice", "Stubs directory does not exist.", "OK");
        }

        private static HashSet<Assembly> GetTargetAssemblies()
        {
            // AppDomain.CurrentDomain.GetAssemblies()
            HashSet<Assembly> assemblies = new HashSet<Assembly>
            {
                // C# 核心程序集
                typeof(object).Assembly, // mscorlib or System.Private.CoreLib
                // Unity核心程序集
                typeof(Editor).Assembly, // UnityEditor.dll
                typeof(UObject).Assembly, // UnityEngine.CoreModule.dll
                typeof(Animation).Assembly, // UnityEngine.AnimationModule.dll
                typeof(AssetBundle).Assembly, // UnityEngine.AssetBundleModule.dll
                // typeof(GUI).Assembly,         // UnityEngine.IMGUIModule.dll // IMGUI通常不建议在Python中使用
            };

            // 用户程序集
            string[] userAssemblies = Directory.GetFiles("Library/ScriptAssemblies", "*.dll");
            foreach (string path in userAssemblies)
            {
                try
                {
                    Assembly assembly = Assembly.LoadFile(Path.GetFullPath(path));
                    assemblies.Add(assembly);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Load assembly failed: {path}, Exception: {e}");
                }
            }

            return assemblies;
        }

        private static void GenerateAssemblyStub(Assembly assembly, string outputPath)
        {
            try
            {
                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                // 按命名空间分组类型
                Dictionary<string, List<Type>> ns2Types = new Dictionary<string, List<Type>>();
                Type[] exportedTypes = assembly.GetExportedTypes();
                foreach (Type type in exportedTypes)
                {
                    if (ShouldSkipType(type))
                        continue;

                    string ns = type.Namespace ?? "global";
                    if (!ns2Types.ContainsKey(ns))
                        ns2Types[ns] = new List<Type>();

                    ns2Types[ns].Add(type);
                }

                // 为每个命名空间生成存根文件
                foreach (KeyValuePair<string, List<Type>> kvp in ns2Types)
                {
                    GenerateNamespaceStub(kvp.Key, kvp.Value, outputPath, assembly.FullName);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to process assembly {assembly.FullName}: {e}");
            }
        }

        private static void GenerateNamespaceStub(string nameSpace, List<Type> types, string outputPath, string assemblyName)
        {
            string nsDir = Path.Combine(outputPath, nameSpace.Replace('.', '/'));
            if (!Directory.Exists(nsDir))
                Directory.CreateDirectory(nsDir);

            string stubPath = Path.Combine(nsDir, "__init__.pyi");
            StringBuilder sb = new StringBuilder();

            // 文件头（不同程序集中可能有相同命名空间，避免重复写入）
            if (!File.Exists(stubPath))
            {
                sb.AppendLine("# Auto-generated Python type stubs for C# APIs");
                sb.AppendLine("# Generated by PythonStubGenerator");
                sb.AppendLine();
                sb.AppendLine("from __future__ import annotations"); // 使类型注解延迟求值（注意，基类、装饰器等不受影响）
                sb.AppendLine("from typing import Any, List, Set, Dict, Tuple, Optional, Union, Callable, ClassVar, overload");
                sb.AppendLine("from enum import Enum");
                sb.AppendLine("import datetime");
                sb.AppendLine();
            }

            // 写入程序集标记
            sb.AppendLine($"# From Assembly: {assemblyName}");
            sb.AppendLine();

            // 处理每个类型
            foreach (Type type in types.OrderBy(t => t.Name))
            {
                GenerateTypeStub(type, sb, 0);
                sb.AppendLine();
            }

            // 使用Append，避免覆盖同名命名空间的内容
            File.AppendAllText(stubPath, sb.ToString());
        }

        private static void GenerateTypeStub(Type type, StringBuilder sb, int indent)
        {
            if (type.IsEnum)
            {
                GenerateEnumStub(type, sb, indent);
            }
            else if (type.IsClass || type.IsValueType || type.IsInterface)
            {
                GenerateClassStub(type, sb, indent);
            }
            else
            {
                Debug.LogError($"Unsupported type: {type.FullName} ({type.MemberType})");
            }
        }

        private static void GenerateEnumStub(Type type, StringBuilder sb, int indent)
        {
            string indentStr = new string(' ', indent * 4);
            sb.AppendLine($"{indentStr}class {type.Name}(Enum):");

            string[] names = Enum.GetNames(type);
            if (names.Length == 0)
            {
                sb.AppendLine($"{indentStr}    pass");
                return;
            }

            Array values = Enum.GetValues(type);
            Assert.IsTrue(values.Length == names.Length);
            for (int i = 0; i < values.Length; i++)
            {
                string name = names[i];
                if (_pythonKeywords.Contains(name))
                    name += "_";

                object value = Convert.ToInt64(values.GetValue(i));
                sb.AppendLine($"{indentStr}    {name} = {value}");
            }

            sb.AppendLine();
        }

        private static void GenerateClassStub(Type type, StringBuilder sb, int indent)
        {
            string indentStr = new string(' ', indent * 4);

            // 类声明
            string baseClass = GetBaseClass(type);
            if (!string.IsNullOrEmpty(baseClass))
            {
                // 仅在注释中标记继承关系即可，因为并不依靠这里生成的代码实现具体运行时功能
                sb.AppendLine($"{indentStr}class {type.Name}: # Inherits from {baseClass}");
            }
            else
            {
                sb.AppendLine($"{indentStr}class {type.Name}:");
            }

            bool hasMembers = false;

            // 构造函数
            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            hasMembers = constructors.Length > 0;
            if (constructors.Length == 1)
            {
                sb.AppendLine($"{indentStr}    # Constructors");
                GenerateConstructorStub(constructors[0], sb, indent + 1);
            }
            else if (constructors.Length > 1)
            {
                sb.AppendLine($"{indentStr}    # Constructors");
                foreach (ConstructorInfo ctor in constructors)
                {
                    sb.AppendLine($"{indentStr}    @overload");
                    GenerateConstructorStub(ctor, sb, indent + 1);
                }
            }

            // 获取所有公共成员，包括继承的成员，因为生成类型存根时没有生成继承关系，无法体现继承的成员
            BindingFlags flattenHierarchyPublicMemberFlags = BindingFlags.FlattenHierarchy | BindingFlags.Public |
                BindingFlags.Instance | BindingFlags.Static;

            // 属性
            PropertyInfo[] properties = type.GetProperties(flattenHierarchyPublicMemberFlags);
            if (properties.Length != 0)
                sb.AppendLine($"{indentStr}    # Properties");
            foreach (PropertyInfo prop in properties)
            {
                if (ShouldSkipMember(prop))
                    continue;

                GeneratePropertyStub(prop, sb, indent + 1);
                hasMembers = true;
            }

            // 字段
            FieldInfo[] fields = type.GetFields(flattenHierarchyPublicMemberFlags);
            if (fields.Length != 0)
                sb.AppendLine($"{indentStr}    # Fields");
            foreach (FieldInfo field in fields)
            {
                if (field.IsSpecialName || field.IsLiteral)
                    continue;

                if (field.IsDefined(typeof(ObsoleteAttribute), true))
                    continue;

                GenerateFieldStub(field, sb, indent + 1);
                hasMembers = true;
            }

            // 方法（按名字分组以处理重载）
            MethodInfo[] methods = type.GetMethods(flattenHierarchyPublicMemberFlags);
            if (methods.Length != 0)
                sb.AppendLine($"{indentStr}    # Methods");
            IEnumerable<IGrouping<string, MethodInfo>> methodGroups = methods
                .GroupBy(m => m.Name);
            List<MethodInfo> tempMethodList = new List<MethodInfo>();
            foreach (IGrouping<string, MethodInfo> group in methodGroups)
            {
                tempMethodList.Clear();
                foreach (MethodInfo method in group)
                {
                    if (method.IsSpecialName || ShouldSkipMember(method))
                        continue;

                    tempMethodList.Add(method);
                }

                if (tempMethodList.Count == 0)
                    continue;

                hasMembers = true;

                if (tempMethodList.Count == 1)
                {
                    GenerateMethodStub(tempMethodList[0], sb, indent + 1);
                    continue;
                }

                // 处理重载
                foreach (MethodInfo method in tempMethodList)
                {
                    sb.AppendLine($"{indentStr}    @overload");
                    GenerateMethodStub(method, sb, indent + 1);
                }
            }

            // 嵌套类型
            Type[] nestedTypes = type.GetNestedTypes(BindingFlags.Public);
            if (nestedTypes.Length != 0)
                sb.AppendLine($"{indentStr}    # Nested Types");
            foreach (Type nestedType in nestedTypes)
            {
                sb.AppendLine();
                GenerateTypeStub(nestedType, sb, indent + 1);
                hasMembers = true;
            }

            if (!hasMembers)
                sb.AppendLine($"{indentStr}    pass");
        }

        private static void GenerateConstructorStub(ConstructorInfo ctor, StringBuilder sb, int indent)
        {
            string indentStr = new string(' ', indent * 4);
            string parameters = GenerateParams(ctor.GetParameters());
            sb.AppendLine($"{indentStr}def __init__(self{parameters}) -> None: ...");
        }

        private static void GenerateMethodStub(MethodInfo method, StringBuilder sb, int indent)
        {
            string indentStr = new string(' ', indent * 4);

            // 检查是否是运算符重载
            if (method.IsSpecialName && _operatorMapping.TryGetValue(method.Name, out string pythonMethod))
            {
                string opParameters = GenerateParams(method.GetParameters());
                string opReturnType = GetPythonType(method.ReturnType);
                sb.AppendLine($"{indentStr}def {pythonMethod}(self{opParameters}) -> {opReturnType}: ...");
                return;
            }

            if (method.IsStatic)
            {
                sb.AppendLine($"{indentStr}@staticmethod");
            }

            string parameters = method.IsStatic
                ? GenerateParams(method.GetParameters(), includeLeadingComma: false)
                : GenerateParams(method.GetParameters());

            string returnType = GetPythonType(method.ReturnType);
            string selfParam = method.IsStatic ? "" : "self";

            sb.AppendLine($"{indentStr}def {method.Name}({selfParam}{parameters}) -> {returnType}: ...");
        }

        private static void GeneratePropertyStub(PropertyInfo prop, StringBuilder sb, int indent)
        {
            // 索引器
            if (prop.GetIndexParameters().Length > 0)
            {
                GenerateIndexerStub(prop, sb, indent);
                return;
            }

            string indentStr = new string(' ', indent * 4);
            string propType = GetPythonType(prop.PropertyType);

            if (prop.GetMethod?.IsStatic == true)
            {
                // 静态属性作为类变量
                sb.AppendLine($"{indentStr}{prop.Name}: {propType}");
            }
            else
            {
                // 实例属性
                sb.AppendLine($"{indentStr}@property");
                sb.AppendLine($"{indentStr}def {prop.Name}(self) -> {propType}: ...");

                if (prop.CanWrite && prop.SetMethod?.IsPublic == true)
                {
                    sb.AppendLine($"{indentStr}@{prop.Name}.setter");
                    sb.AppendLine($"{indentStr}def {prop.Name}(self, value: {propType}) -> None: ...");
                }
            }
        }

        private static void GenerateIndexerStub(PropertyInfo indexer, StringBuilder sb, int indent)
        {
            string indentStr = new string(' ', indent * 4);
            ParameterInfo[] indexParams = indexer.GetIndexParameters();
            string indexType = GetPythonType(indexParams[0].ParameterType);
            string returnType = GetPythonType(indexer.PropertyType);

            // __getitem__
            if (indexer.CanRead)
                sb.AppendLine($"{indentStr}def __getitem__(self, key: {indexType}) -> {returnType}: ...");

            // __setitem__
            if (indexer.CanWrite)
                sb.AppendLine($"{indentStr}def __setitem__(self, key: {indexType}, value: {returnType}) -> None: ...");
        }

        private static void GenerateFieldStub(FieldInfo field, StringBuilder sb, int indent)
        {
            string indentStr = new string(' ', indent * 4);
            string fieldType = GetPythonType(field.FieldType);
            if (field.IsStatic)
                // 静态字段作为类变量，使用ClassVar标记
                sb.AppendLine($"{indentStr}{field.Name}: ClassVar[{fieldType}]");
            else
                // 实例字段
                sb.AppendLine($"{indentStr}{field.Name}: {fieldType}");
        }

        private static string GenerateParams(ParameterInfo[] parameters, bool includeLeadingComma = true)
        {
            if (parameters.Length == 0)
                return "";

            IEnumerable<string> paramList = parameters.Select(p =>
            {
                string paramType = GetPythonType(p.ParameterType);
                string paramName = GetSafePythonParamName(p.Name);

                // 处理可变参数
                if (p.IsDefined(typeof(ParamArrayAttribute), false))
                {
                    // 可变参数在Python中使用*args
                    Type elementType = p.ParameterType.GetElementType();
                    if (elementType != null)
                    {
                        string elementTypeName = GetPythonType(elementType);
                        return $"*{paramName}: {elementTypeName}";
                    }
                }

                // 处理ref/out参数
                if (p.IsOut)
                {
                    // out参数在Python中通常作为返回值的一部分
                    return $"{paramName}: Any = None"; // 提供默认值
                }

                if (p.ParameterType.IsByRef)
                {
                    // ref参数需要特殊标记
                    Type elementType = p.ParameterType.GetElementType();
                    paramType = GetPythonType(elementType);
                }

                if (p.HasDefaultValue)
                {
                    if (p.DefaultValue == null)
                    {
                        return $"{paramName}: Optional[{paramType}] = None";
                    }

                    if (p.ParameterType == typeof(bool))
                    {
                        string value = (bool)p.DefaultValue ? "True" : "False";
                        return $"{paramName}: {paramType} = {value}";
                    }

                    if (p.ParameterType.IsEnum)
                    {
                        return $"{paramName}: {paramType} = ...";
                    }

                    if (p.ParameterType.IsPrimitive)
                    {
                        return $"{paramName}: {paramType} = {p.DefaultValue}";
                    }

                    return $"{paramName}: {paramType} = ...";
                }

                return $"{paramName}: {paramType}";
            });

            string result = string.Join(", ", paramList);
            return includeLeadingComma && result.Length > 0 ? ", " + result : result;
        }

        private static string GetSafePythonParamName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "arg";

            if (_pythonKeywords.Contains(name))
                return name + "_";

            return name;
        }

        private static string GetPythonType(Type type)
        {
            // 处理可空类型
            Type underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
                return $"Optional[{GetPythonType(underlyingType)}]";

            // 基础类型映射
            if (_typeMapping.TryGetValue(type, out string mappedType))
                return mappedType;

            // 数组类型
            if (type.IsArray)
            {
                string elementType = GetPythonType(type.GetElementType());
                return $"List[{elementType}]";
            }

            // 泛型类型
            if (type.IsGenericType)
            {
                Type genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>))
                {
                    string argType = GetPythonType(type.GetGenericArguments()[0]);
                    return $"List[{argType}]";
                }

                if (genericDef == typeof(Dictionary<,>))
                {
                    Type[] args = type.GetGenericArguments();
                    return $"Dict[{GetPythonType(args[0])}, {GetPythonType(args[1])}]";
                }

                if (genericDef == typeof(HashSet<>))
                {
                    string argType = GetPythonType(type.GetGenericArguments()[0]);
                    return $"Set[{argType}]";
                }

                if (genericDef == typeof(IEnumerable<>) || genericDef == typeof(IList<>))
                {
                    string argType = GetPythonType(type.GetGenericArguments()[0]);
                    return $"List[{argType}]";
                }

                // 其他泛型类型
                string typeName = type.Name.Split('`')[0];
                string typeArgs = string.Join(", ", type.GetGenericArguments().Select(GetPythonType));
                return $"{typeName}[{typeArgs}]";
            }

            // 委托类型
            if (typeof(Delegate).IsAssignableFrom(type))
                return "Callable[..., Any]";

            // 其他类型直接使用类型名
            return $"\"{type.Name}\"";
        }

        private static string GetBaseClass(Type type)
        {
            Type baseType = type.BaseType;
            if (baseType == null || baseType == typeof(object) || baseType == typeof(ValueType))
                return "";

            return baseType.FullName;
        }

        private static bool ShouldSkipType(Type type)
        {
            if (type.IsSpecialName || type.IsNestedPrivate || type.IsNotPublic || type.IsGenericTypeDefinition)
                return true;

            if (type.Name.StartsWith("<") || type.Name.Contains("__"))
                return true;

            if (type.IsDefined(typeof(ObsoleteAttribute), true))
                return true;

            return false;
        }

        private static bool ShouldSkipMember(MemberInfo member)
        {
            if (member.Name.StartsWith("get_") || member.Name.StartsWith("set_"))
                return true;

            if (member is MethodInfo method && method.IsGenericMethodDefinition)
                return true;

            if (member.IsDefined(typeof(ObsoleteAttribute), true))
                return true;

            return false;
        }

        private static void GenerateInitFiles(string rootPath)
        {
            // 为每个目录生成__init__.py
            string[] directories = Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories);
            foreach (string directory in directories)
            {
                string initPath = Path.Combine(directory, "__init__.py");
                if (!File.Exists(initPath))
                    File.WriteAllText(initPath, "# Auto-generated package marker\n");
            }

            // 生成根目录的__init__.py
            string rootInitPath = Path.Combine(rootPath, "__init__.py");
            if (File.Exists(rootInitPath))
                return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Auto-generated Python stubs root");
            sb.AppendLine("# Import all namespaces");

            foreach (string dir in Directory.GetDirectories(rootPath))
            {
                string dirName = Path.GetFileName(dir);
                sb.AppendLine($"from . import {dirName}");
            }

            File.WriteAllText(rootInitPath, sb.ToString());
        }

        private static void GenerateReadmeFile(string rootPath)
        {
            const string ReadmeContent = "Add the following configuration to your VSCode `settings.json` file, " +
                "replacing `<YOUR_PROJECT_ROOT>` with the root directory of your Unity project:\n" +
                "```json\n" +
                "{\n" +
                "    \"python.analysis.extraPaths\": [\n" +
                "        \"<YOUR_PROJECT_ROOT>/Library/PythonScripting/stubs\"\n" +
                "    ],\n" +
                "    \"python.analysis.stubPath\": \"<YOUR_PROJECT_ROOT>/Library/PythonScripting/stubs\",\n" +
                "    \"python.analysis.autoSearchPaths\": true,\n" +
                "    \"python.analysis.useLibraryCodeForTypes\": true,\n" +
                "    \"python.analysis.typeCheckingMode\": \"basic\",\n" +
                "    \"python.languageServer\": \"Pylance\"\n}\n" +
                "```\n";
            string readmePath = Path.Combine(rootPath, "readme.md");
            File.WriteAllText(readmePath, ReadmeContent);
        }
    }
}