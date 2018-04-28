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
        private Dictionary<string, MethodInfo> mRouteMap;
        private ErrorHandler m404Handler;
        private object mTarget;

        private BlockingCollection<HttpListenerContext> mWorkQueue;

        public Server()
        {
            mRouteMap = new Dictionary<string, MethodInfo>();
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

                //Console.WriteLine($"Incoming request Method={context.Request.HttpMethod} LocalPath={context.Request.Url.LocalPath} RawUrl={context.Request.RawUrl}");

                Uri requestUrl = context.Request.Url;
                string route = requestUrl.LocalPath;

                MethodInfo info = FindRequestHandlerForRoute(route);
                string response;
                if (info == null || !CallMethodHandler(info, context.Request, out response))
                {
                    response = m404Handler(requestUrl);
                }

                SendResponse(context, response);
            }
        }

        private void SendResponse(HttpListenerContext context, string response)
        {
            try
            {
                using (StreamWriter output = new StreamWriter(context.Response.OutputStream))
                {
                    output.Write(response);
                }
            }
            catch (HttpListenerException e)
            {
                Console.WriteLine($"Error writing to response stream: {e.Message}");
            }
        }

        private bool CallMethodHandler(MethodInfo handler, HttpListenerRequest request, out string response)
        {
            if (handler.ReturnType != typeof(string))
            {
                throw new ArgumentException("Method to invoke doesn't return string.");
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
                response = (string)handler.Invoke(mTarget, realArgs);
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

        private MethodInfo FindRequestHandlerForRoute(string route)
        {
            foreach (string candidate in mRouteMap.Keys)
            {
                if (RouteMatches(route, candidate))
                {
                    return mRouteMap[candidate];
                }
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
                IEnumerable<GetAttribute> getAttributes = info.GetCustomAttributes<GetAttribute>();
                if (getAttributes.Count() > 0)
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

                foreach (GetAttribute attribute in getAttributes)
                {
                    string route = attribute.Route;
                    mRouteMap.Add(route, info);
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
