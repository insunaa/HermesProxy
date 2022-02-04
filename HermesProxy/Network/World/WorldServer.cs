using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using HermesProxy.Framework.Constants.Network;
using HermesProxy.Framework.Logging;
using HermesProxy.Network.World.Server;

namespace HermesProxy.Network.World;

public static class WorldServer
{
    public static WorldSocket RealmSocket { get; private set; }
    public static WorldSocket InstanceSocket { get; private set; }

    static bool _requestDisconnect;

    public static void Start(string ip)
    {
        var realmListener = new TcpListener(IPAddress.Parse(ip), 9085);
        realmListener.Start();
        Log.Print(LogType.Server, $"Started Realm Server on {ip}:9085");

        var instanceListener = new TcpListener(IPAddress.Parse(ip), 9086);
        instanceListener.Start();
        Log.Print(LogType.Server, $"Started Instance Server on {ip}:9086");

        var realmThread = new Thread(() =>
        {
            while (true)
            {
                if (realmListener.Pending())
                {
                    RealmSocket = new(realmListener.AcceptSocket(), ConnectionType.Realm);
                    RealmSocket.StartConnection();
                }

                Thread.Sleep(1);
            }
        });
        realmThread.Start();

        var instanceThread = new Thread(() =>
        {
            while (true)
            {
                if (instanceListener.Pending())
                {
                    InstanceSocket = new(instanceListener.AcceptSocket(), ConnectionType.Instance);
                    InstanceSocket.StartConnection();
                }

                Thread.Sleep(1);
            }
        });
        instanceThread.Start();

        var updateThread = new Thread(() =>
        {
            var lastTick = Environment.TickCount;
            var currentTick = 0;

            while (!_requestDisconnect)
            {
                currentTick = Environment.TickCount;

                RealmSocket?.Update();
                InstanceSocket?.Update();

                lastTick = currentTick;
                Thread.Sleep(1);
            }

            RealmSocket?.RequestDisconnect();
            InstanceSocket?.RequestDisconnect();
        });
        updateThread.IsBackground = true;
        updateThread.Start();
    }

    public static void Disconnect() => _requestDisconnect = true;
}
