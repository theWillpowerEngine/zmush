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

        //Web server with memory cache
        if (request.Method == "GET")
        {
            string route = request.Url.TrimStart('/');
            if (Loader.CachedURLs.Contains(route))
            {
                var (content, mime) = Loader.GetContentAndMime(route);
                SendResponseAsync(Response.MakeGetResponse(content, mime));
            }
            else
                SendResponseAsync(Response.MakeErrorResponse(404, "Requested resource not found: /" + route));
        }

        //Endpoints
        else if ((request.Method == "POST") || (request.Method == "PUT"))
        {
            var route = request.Url.TrimStart('/');
            var sessionId = "";

            try
            {
                switch (route)
                {
                    case "auth":
                        var userYaml = request.Body;
                        var user = AuthModel.FromYaml(userYaml);
                        if (user == null)
                            SendResponseAsync(Response.MakeErrorResponse(400, "Invalid user data."));
                        else
                        {
                            var dbUser = User.Load(user.u);
                            if (dbUser == null)
                                SendResponseAsync(Response.MakeErrorResponse(401, "User not found."));
                            else if (!dbUser.IsPasswordValid(user.p))
                                SendResponseAsync(Response.MakeErrorResponse(401, "Login failed."));

                            else
                            {
                                var session = Engine.MakeSessionFor(dbUser);
                                SendResponseAsync(Response.MakeGetResponse(session.Key.ToString(), "text/plain"));
                            }
                        }
                        break;

                    case "reauth":
                        sessionId = request.Body;
                        if (!Engine.Sessions.TryGetValue(sessionId, out var existingSession))
                            SendResponseAsync(Response.MakeErrorResponse(403, "Session not found or expired."));

                        else
                        {
                            existingSession.LastActivity = DateTime.Now;
                            SendResponseAsync(Response.MakeGetResponse(existingSession.Key.ToString(), "text/plain"));
                        }
                        break;

                    case "frame":
                        sessionId = request.Body;
                        if (!Engine.Sessions.TryGetValue(sessionId, out var frameSession))
                            SendResponseAsync(Response.MakeErrorResponse(403, "Session not found or expired."));

                        else
                        {
                            frameSession.LastActivity = DateTime.Now;
                            var text = Engine.RenderFrame(frameSession.UserId);

                            SendResponseAsync(Response.MakeGetResponse(text, "text/plain"));
                        }
                        break;

                    default:
                        SendResponseAsync(Response.MakeErrorResponse(404, "Requested resource not found: /" + route));
                        break;
                }

                SendResponseAsync(Response.MakeErrorResponse(404, "This shit don't work yet, chill."));
            }
            catch (Exception ex)
            {
                Guid corrId = Guid.NewGuid();
                Engine.Log("ERROR", $"Error in API route {route}: {ex.Message} (Correlation ID: {corrId})");
                SendResponseAsync(Response.MakeErrorResponse(500, "Correlation ID: " + corrId));
            }
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