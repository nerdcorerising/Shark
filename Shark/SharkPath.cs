
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Shark
{
    [Flags]
    public enum HttpMethod
    {
        GET = 1,
        POST = 2,
        PUT = 4,
        PATCH = 8,
        DELETE = 16
    }

    public class SharkPath
    {
        private enum PathPartKind
        {
            StaticPath,
            VariablePath,
            UserItem
        }

        private class PathPart
        {
            private readonly PathPartKind mKind;
            private readonly string mName;
            private readonly Type mType;

            public PathPart(PathPartKind kind, string name, Type type)
            {
                mKind = kind;
                mName = name;
                mType = type;
            }

            public PathPartKind Kind => mKind;
            public string Name => mName;
            public Type Type => mType;

			public override bool Equals(object obj)
			{
                if (obj == null || obj.GetType() != this.GetType())
                {
                    return false;
                }

                PathPart other = (PathPart)obj;

                return mKind == other.mKind 
                        && mName == other.mName
                        && mType == other.mType;
                    
			}

			public override int GetHashCode()
			{
                int constant = 7;
                int hashcode = constant * mKind.GetHashCode();
                hashcode += constant * mName.GetHashCode();
                if (mType != null)
                {
                    hashcode += constant * mType.GetHashCode();
                }

                return hashcode;
			}
		}

        readonly List<PathPart> mParts;
        private readonly HttpMethod mMethods;

        public SharkPath(string route, HttpMethod methods)
        {
            mParts = new List<PathPart>();
            mMethods = methods;

            BuildPath(route);
        }

        public bool EnabledFor(HttpMethod method)
        {
            return mMethods.HasFlag(method);
        }

        public bool MatchesRoute(string route)
        {
            return ParseRouteHelper(route, null);
        }

        public Dictionary<string, object> ParseRoute(string route)
        {
            Dictionary<string, object> variables = new Dictionary<string, object>();
            if (ParseRouteHelper(route, variables))
            {
                return variables;
            }

            return null;
        }

        public Dictionary<string, Type> GetVariableTypes()
        {
            Dictionary<string, Type> variables = new Dictionary<string, Type>();

            foreach (PathPart part in mParts)
            {
                if (part.Kind == PathPartKind.UserItem)
                {
                    variables.Add(part.Name, part.Type);
                }
            }

            return variables;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != this.GetType())
            {
                return false;
            }

            SharkPath other = (SharkPath)obj;
            if (mMethods != other.mMethods)
            {
                return false;
            }

            List<PathPart> otherParts = other.mParts;
            if (otherParts.Count != mParts.Count)
            {
                return false;
            }

            for (int i = 0; i < mParts.Count; ++i)
            {
                if (!mParts[i].Equals(otherParts[i]))
                {
                    return false;
                }
            }

            return true;
		}

		public override int GetHashCode()
		{
            int constant = 7;
            int hashCode = mMethods.GetHashCode() * constant;

            for (int i = 0; i < mParts.Count; ++i)
            {
                hashCode += mParts[i].GetHashCode() * constant;
            }

            return hashCode;
		}

        private bool ParseRouteHelper(string route, Dictionary<string, object> variables)
        {
            int pos = 0;
            for (int i = 0; i < mParts.Count; ++i)
            {
                PathPart pp = mParts[i];
                switch (pp.Kind)
                {
                    case PathPartKind.StaticPath:
                        {
                            string path = pp.Name;
                            for (int subPos = 0; subPos < path.Length; ++subPos)
                            {
                                if (pos + subPos >= route.Length || route[pos + subPos] != path[subPos])
                                {
                                    return false;
                                }
                            }

                            pos += path.Length;
                        }
                        break;
                    case PathPartKind.UserItem:
                        {
                            StringBuilder value = new StringBuilder();
                            while (pos < route.Length && route[pos] != '/')
                            {
                                value.Append(route[pos]);
                                ++pos;
                            }

                            Object arg = CreateArgForType(value.ToString(), pp.Type);
                            if (arg == null)
                            {
                                return false;
                            }

                            // TODO: what about duplicately named variables
                            variables?.Add(pp.Name, arg);
                        }
                        break;
                    // TODO: implement this
                    case PathPartKind.VariablePath:
                    default:
                        throw new InvalidOperationException("Unknown type in ParseRouteHelper.");
                }
            }

            return pos == route.Length;
        }

        private object CreateArgForType(string value, Type paramType)
        {
            if (paramType == typeof(string))
            {
                return value;
            }
            else if (paramType == typeof(char))
            {
                if (value.Length != 1)
                {
                    return null;
                }

                return value[0];
            }
            else if (paramType == typeof(bool))
            {
                return CreateIntegralType(value, Convert.ToBoolean);
            }
            else if (paramType == typeof(byte))
            {
                return CreateIntegralType(value, Convert.ToByte);
            }
            else if (paramType == typeof(short))
            {
                return CreateIntegralType(value, Convert.ToInt16);
            }
            else if (paramType == typeof(int))
            {
                return CreateIntegralType(value, Convert.ToInt32);
            }
            else if (paramType == typeof(long))
            {
                return CreateIntegralType(value, Convert.ToInt64);
            }
            else if (paramType == typeof(float))
            {
                return CreateIntegralType(value, Convert.ToSingle);
            }
            else if (paramType == typeof(double))
            {
                return CreateIntegralType(value, Convert.ToDouble);
            }
            else if (paramType == typeof(ushort))
            {
                return CreateIntegralType(value, Convert.ToUInt16);
            }
            else if (paramType == typeof(uint))
            {
                return CreateIntegralType(value, Convert.ToUInt32);
            }
            else if (paramType == typeof(ulong))
            {
                return CreateIntegralType(value, Convert.ToUInt64);
            }
            else
            {
                return null;
            }
        }

        private object CreateIntegralType<I>(string value, Func<string, I> converter)
        {
            // TODO: this makes coding easier but the exception based Convert methods are much slower
            // than TryParse
            try
            {
                return converter(value);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void BuildPath(string route)
        {
            if (route == "")
            {
                PathPart pp = new PathPart(PathPartKind.StaticPath, "/", null);
                mParts.Add(pp);
                return;
            }

            StringBuilder builder = new StringBuilder();
            int pos = 0;
            bool inVariable = false;
            while (true)
            {
                if (!inVariable)
                {
                    if (pos >= route.Length || route[pos] == '{')
                    {
                        inVariable = true;
                        string path = builder.ToString();
                        builder.Clear();
                        if (path.Length > 0)
                        {
                            PathPart temp = new PathPart(PathPartKind.StaticPath, path, null);
                            mParts.Add(temp);
                        }
                    }
                    else if (route[pos] == '}')
                    {
                        throw new ArgumentException($"Mismatched }} in path {route}.");
                    }
                    else
                    {
                        builder.Append(route[pos]);
                    }
                }
                else
                {
                    if (pos >= route.Length)
                    {
                        throw new ArgumentException($"Path {route} has no terminating }}.");
                    }
                    else if (route[pos] == '{')
                    {
                        throw new ArgumentException($"Path {route} has nested variable declaration.");
                    }
                    else if (route[pos] == '}')
                    {
                        inVariable = false;
                        string variableDecl = builder.ToString();
                        builder.Clear();
                        PathPart temp = ParseVariable(variableDecl);
                        mParts.Add(temp);
                    }
                    else
                    {
                        builder.Append(route[pos]);
                    }
                }

                if (pos >= route.Length)
                {
                    break;
                }

                ++pos;
            }

        }

        private PathPart ParseVariable(string variable)
        {
            string[] parts = variable.Split(':');
            if (parts.Length > 2)
            {
                throw new ArgumentException("More than one ':' in variable declaration.");
            }

            string variableName = parts[0];
            PathPartKind kind = PathPartKind.UserItem;
            Type variableType = null;
            if (parts.Length == 2)
            {
                if (parts[1].Equals("path", StringComparison.OrdinalIgnoreCase))
                {
                    kind = PathPartKind.VariablePath;
                    variableType = typeof(string);
                }
                else
                {
                    variableType = ParseType(parts[1]);
                }
            }
            else
            {
                variableType = typeof(String);
            }

            PathPart pp = new PathPart(kind, variableName, variableType);
            return pp;
        }

        private Type ParseType(string typeName)
        {
            typeName = typeName.ToLower();

            if (typeName == "string")
            {
                return typeof(string);
            }
            else if (typeName == "char")
            {
                return typeof(char);
            }
            else if (typeName == "bool")
            {
                return typeof(bool);
            }
            else if (typeName == "byte")
            {
                return typeof(byte);
            }
            else if (typeName == "short")
            {
                return typeof(short);
            }
            else if (typeName == "int")
            {
                return typeof(int);
            }
            else if (typeName == "long")
            {
                return typeof(long);
            }
            else if (typeName == "float")
            {
                return typeof(float);
            }
            else if (typeName == "double")
            {
                return typeof(double);
            }
            else if (typeName == "ushort")
            {
                return typeof(ushort);
            }
            else if (typeName == "uint")
            {
                return typeof(uint);
            }
            else if (typeName == "ulong")
            {
                return typeof(ulong);
            }
            else 
            {
                throw new ArgumentException($"Unsupported type {typeName}");
            }
        }
	}
}
