using System.Threading;

using HermesProxy.Framework.Logging;
using HermesProxy.Network.BattleNet;
using HermesProxy.Network.World;

namespace HermesProxy;

class Program
{
    static void Main(string[] args)
    {
        // Start the Logger
        Log.Start();

        Log.Print(LogType.Debug, $"Client Version: {Settings.ClientBuild}");
        Log.Print(LogType.Debug, $"Server Version: {Settings.ServerBuild}");

        BattlenetServer.Start(Settings.ServerAddress);
        WorldServer.Start(Settings.ServerAddress);

        while (true)
        {
            Thread.Sleep(1);
        }
    }
}
