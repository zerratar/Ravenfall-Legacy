using System.IO.Pipes;
using System.Text;
using System;
using System.Linq;
using System.Threading;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using static GameMath;
using System.Threading.Tasks;

namespace RavenfallDataPipe
{

    public class QueryEngineWebAPIServer : IDisposable
    {
        private readonly HttpListener listener;
        private readonly QueryEngine engine;
        private bool disposed;

        public QueryEngineWebAPIServer(QueryEngineContext context)
        {
            this.listener = new HttpListener();
            this.engine = new QueryEngine(context);

        }

        public void Start(string prefix)//string host, string apiRoute, int apiPort)
        {
            //var prefix = $"http://{host ?? "*"}:{apiPort}/" + (!string.IsNullOrEmpty(apiRoute) ? apiRoute + "/" : "");
            if (!prefix.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                prefix = "http://" + prefix;
            if (!prefix.EndsWith("/")) prefix = prefix + "/";
            try
            {
                this.listener.Prefixes.Add(prefix);
                this.listener.Start();
                Shinobytes.Debug.Log("Query Engine Web API Server Started on: " + prefix);
                this.listener.BeginGetContext(OnContextReceived, this.listener);
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("Failed to start Query Engine Web API Server (" + prefix + "): " + exc.ToString());
            }
        }
        private async void OnContextReceived(IAsyncResult ar)
        {
            if (disposed)
            {
                return;
            }

            try
            {
                var context = this.listener.EndGetContext(ar);
                if (context != null)
                {
                    await HandleRequest(context);
                }
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError(exc.ToString());
            }

            try
            {
                if (this.listener.IsListening)
                {
                    this.listener.BeginGetContext(OnContextReceived, this.listener);
                }
            }
            catch { }
        }

        private async Task HandleRequest(HttpListenerContext context)
        {
            try
            {
                using (context.Response)
                {
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/json";

                    if (TryGetContent(context, out var query))
                    {
                        Shinobytes.Debug.Log("Query Engine - Processing Query: " + query);
                        WriteResponse(await engine.ProcessAsync(query), context);
                    }
                    else
                    {
                        Shinobytes.Debug.Log("Query Engine - Bad Request: " + context.Request.Url.LocalPath);
                        WriteResponse("Empty queries, bad queries!", context);
                    }

                    //var target = context.Request.Url.LocalPath.Split('/').LastOrDefault();
                    //if (!string.IsNullOrEmpty(target) && target.IndexOf('.') > 0)
                    //{
                    //    var method = target.Split('.')[0];
                    //    switch (method.ToLower())
                    //    {
                    //        case "query":
                    //            if (TryGetContent(context, out var query))
                    //            {
                    //                WriteResponse(await engine.ProcessAsync(query), context);
                    //            }
                    //            else
                    //            {
                    //                WriteResponse("Empty queries, bad queries!", context);
                    //            }
                    //            break;
                    //        default:
                    //            Shinobytes.Debug.Log("Query Engine Request, unhandled method: " + method);
                    //            break;
                    //    }
                    //}

                    context.Response.Close();
                }

            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("Query Engine - Bad Request: " + context.Request.Url.LocalPath + ", Error: " + exc);
            }
        }


        private bool TryGetContent(HttpListenerContext context, out string content)
        {
            content = null;

            if (context.Request.HttpMethod == "GET")
            {
                content = context.Request.Url.ToString().Split('/').LastOrDefault();
                return true;
            }

            if (context.Request.ContentLength64 > 0)
            {
                try
                {
                    using (var sr = new StreamReader(context.Request.InputStream))
                    {
                        content = sr.ReadToEnd();
                        return true;
                    }
                }
                catch (Exception exc)
                {

                    Shinobytes.Debug.LogError(exc);
                }
            }
            return false;
        }

        private bool TryGetBody<T>(HttpListenerContext context, out T value, out string json)
        {
            value = default;
            if (TryGetContent(context, out json))
            {
                try
                {
                    value = JsonConvert.DeserializeObject<T>(json);
                    return true;
                }
                catch (Exception exc)
                {
                    Shinobytes.Debug.LogError(exc);
                }
            }
            return false;
        }

        private void WriteResponse<T>(T data, HttpListenerContext context, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var json = JsonConvert.SerializeObject(data);
            var bytes = UTF8Encoding.UTF8.GetBytes(json);
            context.Response.ContentLength64 = bytes.Length;
            context.Response.StatusCode = (int)statusCode;
            using (var bw = new BinaryWriter(context.Response.OutputStream))
            {
                bw.Write(bytes);
            }
        }


        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (listener == null) return;
            try { this.listener.Stop(); } catch { }
        }
    }


    public class TableNotFoundException : Exception
    {
        public TableNotFoundException() { }
        public TableNotFoundException(string message) : base(message)
        {
        }
    }
}