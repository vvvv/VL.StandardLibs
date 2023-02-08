using Stride.Core.Shaders.Ast.Hlsl;
using System;
using System.Linq;
using VL.Lang;
using Stride.Core.Mathematics;
using Stride.Core.Shaders.Ast.Stride;
using Stride.Core.Shaders.Ast;
using System.Collections.Generic;
using VL.Core;

namespace VL.Stride.Rendering
{
    public static class ParserExtensions
    {
        public static ClassType GetFirstClassDecl(this Shader shader)
        {

            var result = shader.Declarations.OfType<ClassType>().FirstOrDefault();

            if (result == null)
            {
                var nameSpace = shader.Declarations.OfType<NamespaceBlock>().FirstOrDefault();
                if (nameSpace != null)
                {
                    result = nameSpace.Body.OfType<ClassType>().FirstOrDefault();
                }

            }

            return result;
        }

        public static bool TryGetAttribute(this Variable v, string attrName, out AttributeDeclaration attribute)
        {
            attribute = v.Attributes.OfType<AttributeDeclaration>().Where(a => a.Name.Text == ShaderMetadata.DefaultName).FirstOrDefault();
            return attribute != null;
        }

        public static string GetKeyName(this Variable v, ClassType shader)
            => shader.Name.Text + "." + v.Name.Text;

        public static string ParseString(this AttributeDeclaration attr)
        {
            return attr.Parameters.FirstOrDefault()?.Value as string;
        }

        public static List<string> ParseStringList(this AttributeDeclaration attr)
        {
            return attr.Parameters
                .Select(p => p?.Value as string)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        public static List<string> ParseStringAsCommaSeparatedList(this AttributeDeclaration attr)
        {
            return attr.Parameters
                .Select(p => p?.Value as string)
                .SelectMany(s => s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct()
                .ToList();
        }

        public static bool ParseBool(this AttributeDeclaration attr, int index = 0)
        {
            if (attr.Parameters.Count > index)
            {
                if (bool.TryParse(attr.Parameters[index].Text, out var value))
                {
                    return value;
                }
            }
            return default;
        }

        public static float ParseFloat(this AttributeDeclaration attr, int index = 0)
        {
            if (attr.Parameters.Count > index)
            {
                if (ParseUtils.TryParseFloat(attr.Parameters[index].Text, out var value))
                {
                    return value;
                }
            }
            return default;
        }

        public static int ParseInt(this AttributeDeclaration attr, int index = 0)
        {
            if (attr.Parameters.Count > index)
            {
                if (ParseUtils.TryParseInt(attr.Parameters[index].Text, out var value))
                {
                    return value;
                }
            }
            return default;
        }

        public static Int2 ParseInt2(this AttributeDeclaration attr)
        {
            return new Int2(attr.ParseInt(0), attr.ParseInt(1));
        }

        public static Int3 ParseInt3(this AttributeDeclaration attr)
        {
            return new Int3(attr.ParseInt(0), attr.ParseInt(1), attr.ParseInt(2));
        }

        public static Int4 ParseInt4(this AttributeDeclaration attr)
        {
            return new Int4(attr.ParseInt(0), attr.ParseInt(1), attr.ParseInt(2), attr.ParseInt(3));
        }

        public static uint ParseUInt(this AttributeDeclaration attr, int index = 0)
        {
            if (attr.Parameters.Count > index)
            {
                if (ParseUtils.TryParseUInt(attr.Parameters[index].Text, out var value))
                {
                    return value;
                }
            }
            return default;
        }

        public static Vector2 ParseVector2(this AttributeDeclaration attr)
        {
            return new Vector2(attr.ParseFloat(0), attr.ParseFloat(1));
        }

        public static Vector3 ParseVector3(this AttributeDeclaration attr)
        {
            return new Vector3(attr.ParseFloat(0), attr.ParseFloat(1), attr.ParseFloat(2));
        }

        public static Vector4 ParseVector4(this AttributeDeclaration attr)
        {
            return new Vector4(attr.ParseFloat(0), attr.ParseFloat(1), attr.ParseFloat(2), attr.ParseFloat(3));
        }

        public static object ParseBoxed(this AttributeDeclaration attr, Type type, object defaultVlaue = null)
        {
            if (type == typeof(float))
                return attr.ParseFloat();

            if (type == typeof(Vector2))
                return attr.ParseVector2();

            if (type == typeof(Vector3))
                return attr.ParseVector3();

            if (type == typeof(Vector4))
                return attr.ParseVector4();

            if (type == typeof(bool))
                return attr.ParseBool();

            if (type == typeof(int))
                return attr.ParseInt();

            if (type == typeof(Int2))
                return attr.ParseInt2();

            if (type == typeof(Int3))
                return attr.ParseInt3();

            if (type == typeof(Int4))
                return attr.ParseInt4();

            if (type == typeof(uint))
                return attr.ParseUInt();

            if (type == typeof(string))
                return attr.ParseString();

            return defaultVlaue ?? Activator.CreateInstance(type);
        }

        public static T GetDefault<T>(this Variable v)
        {
            var inital = v.InitialValue;
            if (inital != null)
                return inital.ParseDefault<T>();

            return default;
        }

        static T ParseDefault<T>(this Expression e)
        {
            if (e is LiteralExpression l)
                return (T)Convert.ChangeType(l.Literal?.Value, typeof(T));

            if (e is MethodInvocationExpression m)
                return m.ParseMethod<T>();

            return default;
        }

        static T ParseArg<T>(this MethodInvocationExpression m, int i)
        {
            if (m.Arguments.Count > i)
                return m.Arguments[i].ParseDefault<T>();

            return default;
        }

        static T ParseMethod<T>(this MethodInvocationExpression m)
        {
            var type = typeof(T);

            if (type == typeof(Vector2))
                return (T)(object)new Vector2(m.ParseArg<float>(0), m.ParseArg<float>(1));
            if (type == typeof(Vector3))
                return (T)(object)new Vector3(m.ParseArg<float>(0), m.ParseArg<float>(1), m.ParseArg<float>(2));
            if (type == typeof(Vector4))
                return (T)(object)new Vector4(m.ParseArg<float>(0), m.ParseArg<float>(1), m.ParseArg<float>(2), m.ParseArg<float>(3));

            if (type == typeof(Int2))
                return (T)(object)new Int2(m.ParseArg<int>(0), m.ParseArg<int>(1));
            if (type == typeof(Int3))
                return (T)(object)new Int3(m.ParseArg<int>(0), m.ParseArg<int>(1), m.ParseArg<int>(2));
            if (type == typeof(Int4))
                return (T)(object)new Int4(m.ParseArg<int>(0), m.ParseArg<int>(1), m.ParseArg<int>(2), m.ParseArg<int>(3));

            return default;

        }
    }
}
