using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Security.Cryptography;

using HermesProxy.Framework.Constants.Network;
using HermesProxy.Framework.Crypto;
using HermesProxy.Framework.IO.Packet;
using HermesProxy.Framework.Logging;

namespace HermesProxy.Network.World.Server;

public class WorldSocket
{
    const string ClientConnInitialize = "WORLD OF WARCRAFT CONNECTION - CLIENT TO SERVER - V2";
    const string ServerConnInitialize = "WORLD OF WARCRAFT CONNECTION - SERVER TO CLIENT - V2";

    readonly ConnectionType _connectionType;
    readonly byte[] _buffer = new byte[4096];

    readonly ConcurrentQueue<ModernServerPacket> _outgoingPackets = new();
    readonly ConcurrentQueue<ModernClientPacket> _incomingPackets = new();

    Socket _socket;
    bool _firstTime = true;
    bool _requestDisconnect = false;
    NewWorldCrypt _worldCrypt;

    public byte[] SessionKey { get; private set; }

    public WorldSocket(Socket socket, ConnectionType connectionType)
    {
        if (_socket != null)
            throw new InvalidOperationException("There already is a WorldSocket instance");

        _worldCrypt = new();
        _socket = socket;
        _connectionType = connectionType;

        SessionKey = RandomNumberGenerator.GetBytes(16);

        Log.Print(LogType.Network, $"Received new connection on {_socket.RemoteEndPoint}");
    }

    /// <summary>
    /// Starts the server -> client connection
    /// </summary>
    public void StartConnection()
    {
        using (var writer = new PacketWriter())
        {
            writer.WriteString(ServerConnInitialize);
            writer.WriteString("\n");

            SendRaw(writer.GetData());
        }

        _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveDataCallback, null);
    }

    private void ReceiveDataCallback(IAsyncResult ar)
    {
        if (_socket == null)
            return;

        try
        {
            var packetLength = _socket.EndReceive(ar);
            if (packetLength == 0)
                return;

            var data = new byte[packetLength];
            Buffer.BlockCopy(_buffer, 0, data, 0, packetLength);

            if (_firstTime)
            {
                using (var reader = new PacketReader(data))
                {
                    var initializer = reader.ReadString(ClientConnInitialize.Length);
                    if (initializer != ClientConnInitialize)
                    {
                        Log.Print(LogType.Error, $"Incorrect initializer! {initializer} != {ClientConnInitialize}");
                        RequestDisconnect();

                        return;
                    }
                }

                using (var writer = new PacketWriter())
                {
                    writer.WriteUInt32(51u);                    //< PacketSize
                    writer.WriteBytes(new byte[12]);            //< Tag
                    writer.WriteUInt16(0x3048);                 //< OpcodeID

                    writer.WriteBytes(new byte[32], 32);        //< DosChallenge
                    writer.WriteBytes(SessionKey, 16);          //< Challenge
                    writer.WriteUInt8(0);                       //< DosZeroBits

                    SendRaw(writer.GetData());
                }

                _firstTime = false;
            }
            else if (packetLength > 5)
            {
                HandleData(data);
            }

            _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveDataCallback, null);
        }
        catch (Exception ex)
        {
            Log.Print(LogType.Error, ex);
            RequestDisconnect();
        }
    }

    private void HandleData(byte[] data) => _incomingPackets.Enqueue(new(data));

    public void Update()
    {
        while (_incomingPackets.TryDequeue(out var clientPacket))
            HandleClientPacket(clientPacket);

        while (_outgoingPackets.TryDequeue(out var serverPacket))
            FlushPacket(serverPacket);

        if (_requestDisconnect)
            CloseSocket();
    }

    private void HandleClientPacket(ModernClientPacket packet)
    {
        Log.Print(LogType.Debug, $"[{_connectionType}] Received packet {packet.Opcode} (0x{packet.Opcode:X} Size: {packet.Header.PacketSize})");

        if (!_worldCrypt.Decrypt(ref packet.Data, ref packet.Header.Tag))
        {
            Log.Print(LogType.Error, $"WorldSocket::HandleClientPacket: Client {_socket.RemoteEndPoint} failed to decrypt packet (size: {packet.Header.PacketSize})");
            RequestDisconnect();

            return;
        }

        // Invoke some method to retrieve the IUniversalPacket structure.
    }

    private void FlushPacket(ModernServerPacket packet)
    {
        Log.Print(LogType.Debug, $"[{_connectionType}] Sent packet {packet.Opcode} (0x{packet.Opcode:X})");

        var data = packet.Data;

        using var dataWriter = new PacketWriter();
        dataWriter.WriteUInt16(packet.Opcode);
        dataWriter.WriteBytes(data);

        data = dataWriter.GetData();
        _worldCrypt.Encrypt(ref data, ref packet.Header.Tag);

        using var writer = new PacketWriter();
        packet.Header.Write(writer);
        writer.WriteBytes(data);

        SendRaw(writer.GetData());
    }

    private void SendRaw(byte[] data) => _socket.Send(data);

    /// <summary>
    /// Disconnect the <see cref="WorldSocket"/>
    /// </summary>
    public void RequestDisconnect() => _requestDisconnect = true;

    private void CloseSocket()
    {
        _socket.Close();
        _socket = null;
    }
}
