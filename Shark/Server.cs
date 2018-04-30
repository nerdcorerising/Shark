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
    public delegate Response ErrorHandler(Uri requestUri);
    public delegate string FastPathCall(object[] args);

    public class Server<T> : IDisposable
    {
        private readonly int DefaultThreads = 8;
        private Dictionary<SharkPath, MethodInfo> mMethods;
        private ErrorHandler m404Handler;
        private CancellationTokenSource mCancellationSource;
        private object mTarget;
        private HttpListener mListener;

        private BlockingCollection<HttpListenerContext> mWorkQueue;

        public Server(SharkOptions options = null)
        {
            mMethods = new Dictionary<SharkPath, MethodInfo>();

            mWorkQueue = new BlockingCollection<HttpListenerContext>();

            BuildRouteMap();

            mTarget = Activator.CreateInstance<T>();

            mCancellationSource = new CancellationTokenSource();
            int numThreads = options?.WorkerThreadCount ?? DefaultThreads;
            for (int i = 0; i < DefaultThreads; ++i)
            {
                Thread temp = new Thread(WorkerThread);
                temp.Start(mCancellationSource.Token);
            }

            string listenUrl = options?.Url ?? "http://localhost:3500/";
            mListener = new HttpListener();
            mListener.Prefixes.Add(listenUrl);

            m404Handler = options?.Handler ?? Default404Handler;
        }

        public void Run(CancellationToken? token = null)
        {
            Console.WriteLine($"Starting listening at {mListener.Prefixes.FirstOrDefault()}");

            mListener.Start();

            while (mListener.IsListening)
            {
                if ((token?.IsCancellationRequested) == true)
                {
                    mListener.Stop();
                    break;
                }

                HttpListenerContext context = mListener.GetContext();
                mWorkQueue.Add(context);
            }
        }

        public void Dispose()
        {
            mCancellationSource.Cancel();
            mCancellationSource.Dispose();
            if (mListener.IsListening)
            {
                mListener.Close();
            }
        }

        private void WorkerThread(object tokenObj)
        {
            CancellationToken token = (CancellationToken)tokenObj;
            while (true)
            {
                HttpListenerContext context = mWorkQueue.Take(token);

                if (token.IsCancellationRequested == true)
                {
                    return;
                }

                // TODO: logging
                //Console.WriteLine($"Incoming request Method={context.Request.HttpMethod} LocalPath={context.Request.Url.LocalPath} RawUrl={context.Request.RawUrl}");

                Uri requestUrl = context.Request.Url;
                string route = requestUrl.LocalPath;
                string httpMethod = context.Request.HttpMethod;

                MethodInfo info;
                Dictionary<string, object> values;
                bool success = FindRequestHandlerForRoute(httpMethod, route, out info, out values);
                Response response;
                if (!success || !CallMethodHandler(info, values, context.Request, out response))
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

        private bool CallMethodHandler(MethodInfo handler, Dictionary<string, object> values, HttpListenerRequest request, out Response response)
        {
            response = String.Empty;

            if (handler.ReturnType != typeof(Response))
            {
                throw new InvalidOperationException("Method to invoke doesn't return a Response.");
            }

            // TODO: something with the query... should make it available to the user somehow
            NameValueCollection query = request.QueryString;

            ParameterInfo[] parameters = handler.GetParameters();
            int paramCount = parameters.Length;
            object[] realArgs = new object[paramCount];

            if (parameters.Length != values.Keys.Count)
            {
                return false;
            }

            for (int i = 0; i < parameters.Length; ++i)
            {
                string paramName = parameters[i].Name;
                object arg;
                if (!values.TryGetValue(paramName, out arg))
                {
                    return false;
                }

                realArgs[i] = arg;
            }

            try
            {
                response = (Response)handler.Invoke(mTarget, realArgs);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool FindRequestHandlerForRoute(string httpMethod, string route, out MethodInfo info, out Dictionary<string, object> values)
        {
            info = null;
            values = null;

            httpMethod = httpMethod.ToLower();
            HttpMethod method;
            if (httpMethod == "get")
            {
                method = HttpMethod.GET;
            }
            else if (httpMethod == "post")
            {
                method = HttpMethod.POST;
            }
            else if (httpMethod == "put")
            {
                method = HttpMethod.PUT;
            }
            else if (httpMethod == "patch")
            {
                method = HttpMethod.PATCH;
            }
            else if (httpMethod == "delete")
            {
                method = HttpMethod.DELETE;
            }
            else
            {
                throw new ArgumentException($"Invalid method {httpMethod} in FindRequestHandlerForRoute");
            }

            foreach (SharkPath path in mMethods.Keys)
            {
                if (path.EnabledFor(method) && (values = path.ParseRoute(route)) != null)
                {
                    info = mMethods[path];
                    return true;
                }
            }

            return false;
        }

        private Response Default404Handler(Uri requestUrl)
        {
            return $"<b>{requestUrl.PathAndQuery}: 404 not found</b>";
        }

        private void BuildRouteMap()
        {
            foreach (MethodInfo info in typeof(T).GetMethods())
            {
                IEnumerable<PathAttribute> getAttributes = info.GetCustomAttributes<PathAttribute>();
                if (getAttributes.Count() > 0)
                {
                    ValidateParameters(info);
                }

                foreach (PathAttribute attribute in getAttributes)
                {
                    string route = attribute.Path;
                    HttpMethod httpMethods = 0;
                    foreach (string method in attribute.Methods)
                    {
                        if (method.Equals("get", StringComparison.OrdinalIgnoreCase))
                        {
                            httpMethods |= HttpMethod.GET;
                        }
                        else if (method.Equals("post", StringComparison.OrdinalIgnoreCase))
						{
							httpMethods |= HttpMethod.POST;
						}
                        else if (method.Equals("put", StringComparison.OrdinalIgnoreCase))
                        {
                            httpMethods |= HttpMethod.PUT;
                        }
                        else if (method.Equals("patch", StringComparison.OrdinalIgnoreCase))
                        {
                            httpMethods |= HttpMethod.PATCH;
                        }
                        else if (method.Equals("delete", StringComparison.OrdinalIgnoreCase))
                        {
                            httpMethods |= HttpMethod.DELETE;
                        }
                        else
                        {
                            throw new ArgumentException($"Invalid http method {method} in path attribute for method {info.Name}.");
                        }
                    }

                    SharkPath path = new SharkPath(route, httpMethods);
                    Dictionary<string, Type> variables = path.GetVariableTypes();
                    ParameterInfo[] paramInfos = info.GetParameters();

                    if (variables.Count != paramInfos.Length)
                    {
                        throw new ArgumentException($"Method {info.Name} has a mismatched number of parameters in the method signature and in the path.");
                    }

                    foreach (ParameterInfo param in paramInfos)
                    {
                        Type varType;
                        if (!variables.TryGetValue(param.Name, out varType))
                        {
                            throw new ArgumentException($"Parameter {param.Name} to method {info.Name} does not exist in the path.");
                        }

                        if (varType != param.ParameterType)
                        {
                            throw new ArgumentException($"Parameter {param.Name} of method {info.Name} has mismatched types with the path.");
                        }
                    }

                    mMethods.Add(path, info);
                }
            }
        }

        private void ValidateParameters(MethodInfo info)
        {
            ParameterInfo[] parameters = info.GetParameters();
            foreach (ParameterInfo param in parameters)
            {
                if (!IsSupportedType(param.ParameterType))
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
