using NetCoreServer;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

class HttpGameSession : HttpSession
{
    public HttpGameSession(NetCoreServer.HttpServer server) : base(server) { }

    protected override void OnReceivedRequest(HttpRequest request)
    {
        if (Engine.Settings.ShowHttpRequest)
            Engine.Log("DEBUG - Request", request.ToString());

        if (request.Method == "GET")
        {
            string route = request.Url.TrimStart('/');
            if (Loader.CachedURLs.Contains(route))
            {
                var (content, mime) = Loader.GetContentAndMime(route);
                SendResponseAsync(Response.MakeGetResponse(content, mime));
            }
            else
                SendResponseAsync(Response.MakeErrorResponse(404, "Requested resource not found: " + route));

        }
        else if ((request.Method == "POST") || (request.Method == "PUT"))
        {
            SendResponseAsync(Response.MakeErrorResponse(404, "This shit don't work yet, chill."));
        }
        else if (request.Method == "HEAD")
            SendResponseAsync(Response.MakeHeadResponse());
        else if (request.Method == "OPTIONS")
            SendResponseAsync(Response.MakeOptionsResponse());
        else if (request.Method == "TRACE")
            SendResponseAsync(Response.MakeTraceResponse(request.Cache.Data));
        else
            SendResponseAsync(Response.MakeErrorResponse("Unsupported HTTP method: " + request.Method));
    }

    protected override void OnReceivedRequestError(HttpRequest request, string error)
    {
        Console.WriteLine($"Request error: {error}");
    }

    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"HTTP session caught an error: {error}");
    }
}

class HttpGameServer : NetCoreServer.HttpServer
{
    public HttpGameServer(IPAddress address, int port) : base(address, port) { }

    protected override TcpSession CreateSession() { return new HttpGameSession(this); }

    protected override void OnError(SocketError error)
    {
        Console.WriteLine($"HTTP session caught an error: {error}");
    }
}