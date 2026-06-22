using NetCoreServer;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

//Docs for this lib:  https://github.com/chronoxor/NetCoreServer#example-https-server

var serverRoot = "/home/malf/z";

Engine.InitDirectories(serverRoot);

Engine.Run(4676);


