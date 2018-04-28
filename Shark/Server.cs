using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Collections.Concurrent;

namespace Shark
{
    public delegate string RequestHandler();
    public delegate string ErrorHandler(Uri requestUri);
    public delegate string FastPathCall(object[] args);

    public class Server
    {
        private readonly int NumThreads = 8;
//#error Need to have list of methodinfo, in case multiple handlers exist for the same route
        private Dictionary<string, MethodInfo> mGetMethods;
        private Dictionary<string, MethodInfo> mPostMethods;
        private Dictionary<string, MethodInfo> mPutMethods;
        private Dictionary<string, MethodInfo> mPatchMethods;
        private Dictionary<string, MethodInfo> mDeleteMethods;
        private ErrorHandler m404Handler;
        private object mTarget;

        private BlockingCollection<HttpListenerContext> mWorkQueue;

        public Server()
        {
            mGetMethods = new Dictionary<string, MethodInfo>();
            mPostMethods = new Dictionary<string, MethodInfo>();
            mPutMethods = new Dictionary<string, MethodInfo>();
            mPatchMethods = new Dictionary<string, MethodInfo>();
            mDeleteMethods = new Dictionary<string, MethodInfo>();

            m404Handler = Default404Handler;
            mWorkQueue = new BlockingCollection<HttpListenerContext>();
        }

        public void RunServer<T>()
        {
            RunServer<T>(null as SharkOptions);
        }

        public void RunServer<T>(SharkOptions options)
        {
            BuildRouteMap<T>();

            mTarget = Activator.CreateInstance<T>();

            CancellationTokenSource tokenSource = new CancellationTokenSource();

            for (int i = 0; i < NumThreads; ++i)
            {
                Thread temp = new Thread(WorkerThread);
                temp.Start(tokenSource.Token);
            }

            string listenUrl = options?.Url ?? "http://localhost:3500/";
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(listenUrl);

            Console.WriteLine($"Starting listening at {listenUrl}");

            listener.Start();

            while (listener.IsListening)
            {
                if ((options?.CancellationToken.IsCancellationRequested) == true)
                {
                    listener.Stop();
                    break;
                }

                HttpListenerContext context = listener.GetContext();
                mWorkQueue.Add(context);
            }
        }

        private void WorkerThread(object tokenObj)
        {
            CancellationToken token = (CancellationToken)tokenObj;
            while (true)
            {
                HttpListenerContext context = mWorkQueue.Take(token);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                // TODO: logging
                //Console.WriteLine($"Incoming request Method={context.Request.HttpMethod} LocalPath={context.Request.Url.LocalPath} RawUrl={context.Request.RawUrl}");

                Uri requestUrl = context.Request.Url;
                string route = requestUrl.LocalPath;
                string httpMethod = context.Request.HttpMethod;

                MethodInfo info = FindRequestHandlerForRoute(httpMethod, route);
                Response response;
                if (info == null || !CallMethodHandler(info, context.Request, out response))
                {
                    response = m404Handler(requestUrl);
                }

                SendResponse(context, response);
            }
        }

        private void SendResponse(HttpListenerContext context, Response response)
        {
            try
            {
                context.Response.StatusCode = response.ResponseCode;
                using (StreamWriter output = new StreamWriter(context.Response.OutputStream))
                {
                    output.Write(response.Body);
                }
            }
            catch (HttpListenerException e)
            {
                Console.WriteLine($"Error writing to response stream: {e.Message}");
            }
        }

        private bool CallMethodHandler(MethodInfo handler, HttpListenerRequest request, out Response response)
        {
            if (handler.ReturnType != typeof(Response))
            {
                throw new InvalidOperationException("Method to invoke doesn't return a Response.");
            }

            NameValueCollection query = request.QueryString;

            ParameterInfo[] parameters = handler.GetParameters();
            int paramCount = parameters.Length;
            object[] realArgs = new object[paramCount];
            if (!MakeArgs(query, parameters, paramCount, realArgs))
            {
                response = String.Empty;
                return false;
            }

            try
            {
                response = (Response)handler.Invoke(mTarget, realArgs);
            }
            catch (Exception)
            {
                response = String.Empty;
                return false;
            }

            return true;
        }

        private bool MakeArgs(NameValueCollection query, ParameterInfo[] parameters, int paramCount, object[] realArgs)
        {
            bool allArgs = parameters.Length == 1 && parameters[0].ParameterType == typeof(NameValueCollection);
            if (!allArgs && parameters.Length != query.AllKeys.Length)
            {
                return false;
            }

            bool success = true;
            for (int i = 0; i < paramCount; ++i)
            {
                ParameterInfo param = parameters[i];
                Type paramType = param.ParameterType;
                string name = param.Name;

                string value = null;
                if (paramType == typeof(NameValueCollection))
                {
                    realArgs[i] = query;
                    continue;
                }
                else if (query.AllKeys.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    value = query[name];
                }
                else
                {
                    return false;
                }

                object arg = null;
                success &= MakeArgument(value, paramType, out arg);
                realArgs[i] = arg;
            }

            return success;
        }

        private bool MakeArgument(string value, Type paramType, out object arg)
        {
            if (paramType == typeof(string))
            {
                arg = value;
                return true;
            }
            else if (paramType == typeof(char))
            {
                if (value.Length != 1)
                {
                    arg = default(char);
                    return false;
                }

                arg = value[0];
                return true;
            }
            else if (paramType == typeof(bool))
            {
                return ParseIntegralType(value, out arg, Convert.ToBoolean);
            }
            else if (paramType == typeof(byte))
            {
                return ParseIntegralType(value, out arg, Convert.ToByte);
            }
            else if (paramType == typeof(short))
            {
                return ParseIntegralType(value, out arg, Convert.ToInt16);
            }
            else if (paramType == typeof(int))
            {
                return ParseIntegralType(value, out arg, Convert.ToInt32);
            }
            else if (paramType == typeof(long))
            {
                return ParseIntegralType(value, out arg, Convert.ToInt64);
            }
            else if (paramType == typeof(float))
            {
                return ParseIntegralType(value, out arg, Convert.ToSingle);
            }
            else if (paramType == typeof(double))
            {
                return ParseIntegralType(value, out arg, Convert.ToDouble);
            }
            else if (paramType == typeof(ushort))
            {
                return ParseIntegralType(value, out arg, Convert.ToUInt16);
            }
            else if (paramType == typeof(uint))
            {
                return ParseIntegralType(value, out arg, Convert.ToUInt32);
            }
            else if (paramType == typeof(ulong))
            {
                return ParseIntegralType(value, out arg, Convert.ToUInt64);
            }
            else
            {
                arg = null;
                return false;
            }
        }

        private static bool ParseIntegralType<T>(string value, out object arg, Func<string, T> converter)
        {
            // TODO: this makes coding easier but the exception based Convert methods are much slower
            // than TryParse
            try
            {
                arg = converter(value);
                return true;
            }
            catch (Exception)
            {
                arg = default(T);
                return false;
            }
        }

        private MethodInfo FindRequestHandlerForRoute(string httpMethod, string route)
        {
            httpMethod = httpMethod.ToLower();

            if (httpMethod == "get")
            {
                return FindRequestHandlerForKnownHttpMethod(mGetMethods, route);
            }
            else if (httpMethod == "post")
            {
                return FindRequestHandlerForKnownHttpMethod(mPostMethods, route);
            }
            else if (httpMethod == "put")
            {
                return FindRequestHandlerForKnownHttpMethod(mPutMethods, route);
            }
            else if (httpMethod == "patch")
            {
                return FindRequestHandlerForKnownHttpMethod(mPatchMethods, route);
            }
            else if (httpMethod == "delete")
            {
                return FindRequestHandlerForKnownHttpMethod(mDeleteMethods, route);
            }
            else
            {
                return null;
            }

        }

        private MethodInfo FindRequestHandlerForKnownHttpMethod(Dictionary<string, MethodInfo> methods, string route)
        {
            MethodInfo target;
            if (methods.TryGetValue(route, out target))
            {
                return target;
            }

            return null;
        }

        private bool RouteMatches(string route, string candidate)
        {
            // TODO: wildcards
            return route.Equals(candidate, StringComparison.OrdinalIgnoreCase);
        }

        private string Default404Handler(Uri requestUrl)
        {
            return $"<b>{requestUrl.PathAndQuery}: 404 not found</b>";
        }

        private void BuildRouteMap<T>()
        {
            foreach (MethodInfo info in typeof(T).GetMethods())
            {
                ParseGetAttributes(info);
                ParsePostAttributes(info);
                ParsePutAttributes(info);
                ParsePatchAttributes(info);
                ParseDeleteAttributes(info);
            }
        }

        private void ParseGetAttributes(MethodInfo info)
        {
            IEnumerable<GetAttribute> getAttributes = info.GetCustomAttributes<GetAttribute>();
            if (getAttributes.Count() > 0)
            {
                ValidateParameters(info);
            }

            foreach (GetAttribute attribute in getAttributes)
            {
                string route = attribute.Path;
                mGetMethods.Add(route, info);
            }
        }

        private void ParsePostAttributes(MethodInfo info)
        {
            IEnumerable<PostAttribute> getAttributes = info.GetCustomAttributes<PostAttribute>();
            if (getAttributes.Count() > 0)
            {
                ValidateParameters(info);
            }

            foreach (PostAttribute attribute in getAttributes)
            {
                string route = attribute.Path;
                mPostMethods.Add(route, info);
            }
        }

        private void ParsePutAttributes(MethodInfo info)
        {
            IEnumerable<PutAttribute> getAttributes = info.GetCustomAttributes<PutAttribute>();
            if (getAttributes.Count() > 0)
            {
                ValidateParameters(info);
            }

            foreach (PutAttribute attribute in getAttributes)
            {
                string route = attribute.Path;
                mPutMethods.Add(route, info);
            }
        }

        private void ParsePatchAttributes(MethodInfo info)
        {
            IEnumerable<PatchAttribute> getAttributes = info.GetCustomAttributes<PatchAttribute>();
            if (getAttributes.Count() > 0)
            {
                ValidateParameters(info);
            }

            foreach (PatchAttribute attribute in getAttributes)
            {
                string route = attribute.Path;
                mPatchMethods.Add(route, info);
            }
        }

        private void ParseDeleteAttributes(MethodInfo info)
        {
            IEnumerable<DeleteAttribute> getAttributes = info.GetCustomAttributes<DeleteAttribute>();
            if (getAttributes.Count() > 0)
            {
                ValidateParameters(info);
            }

            foreach (DeleteAttribute attribute in getAttributes)
            {
                string route = attribute.Path;
                mDeleteMethods.Add(route, info);
            }
        }

        private void ValidateParameters(MethodInfo info)
        {
            ParameterInfo[] parameters = info.GetParameters();
            foreach (ParameterInfo param in parameters)
            {
                if (param.ParameterType == typeof(NameValueCollection))
                {
                    if (parameters.Length != 1)
                    {
                        throw new ArgumentException($"NameValueCollection arguments must be the only argument for method {info.Name}");
                    }
                }
                else if (!IsSupportedType(param.ParameterType))
                {
                    throw new ArgumentException($"Unsupported type {param.ParameterType} as argument to method for method {info.Name}");
                }
            }
        }

        private bool IsSupportedType(Type parameterType)
        {
            switch (Type.GetTypeCode(parameterType))
            {
                case TypeCode.String:
                case TypeCode.Char:
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }
    }
}
