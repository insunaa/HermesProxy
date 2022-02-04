using System.Net;
using System.Net.Sockets;

using HermesProxy.Framework.Constants.Network;
using HermesProxy.Framework.Util;

namespace HermesProxy.Network.Realm;

public class RealmInfo
{
    public int ID;
    public byte Type;
    public byte IsLocked;
    public RealmFlags Flags;
    public string Name;
    public string Address;
    public int Port;
    public float Population;
    public byte CharacterCount;
    public byte Timezone;
    public byte VersionMajor;
    public byte VersionMinor;
    public byte VersionBugfix;
    public ushort Build;

    public IPAddress ExternalAddress { get { return IPAddress.Parse(Address); } }
    public IPAddress LocalAddress { get; } = IPAddress.Parse("127.0.0.1");
    public IPAddress LocalSubnetMask { get; } = IPAddress.Parse("255.255.255.0");

    public override string ToString()
    {
        return $"{ID,-5} {Type,-5} {IsLocked,-8} {Flags,-10} {Name,-15} {Address,-15} {Port,-10} {Build,-10}";
    }

    public IPEndPoint GetAddressForClient(IPAddress clientAddress)
    {
        IPAddress realmIp;

        // Attempt to send best address for client
        if (IPAddress.IsLoopback(clientAddress))
        {
            // Try guessing if realm is also connected locally
            if (IPAddress.IsLoopback(LocalAddress))
                realmIp = clientAddress;
            else
            {
                // Assume that user connecting from the machine that authserver is located on
                // has all realms available in his local network
                realmIp = LocalAddress;
            }
        }
        else
        {
            if (clientAddress.AddressFamily == AddressFamily.InterNetwork && clientAddress.GetNetworkAddress(LocalSubnetMask).Equals(LocalAddress.GetNetworkAddress(LocalSubnetMask)))
                realmIp = LocalAddress;
            else
                realmIp = ExternalAddress;
        }

        IPEndPoint endpoint = new(realmIp, Port);

        // Return external IP
        return endpoint;
    }
}
