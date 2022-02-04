using System;

using HermesProxy.Framework.IO.Packet.Messages;

namespace HermesProxy.Framework.IO.Packet
{
    public abstract class ModernGamePacket
    {
        /// <summary>
        /// Total size of <see cref="ModernPacketHeader"/> + <see cref="ushort"/> opcode.
        /// </summary>
        public const int HeaderSize = 16;

        public ModernPacketHeader Header { get; set; } = new();

        public ushort Opcode { get; set; }
        public byte[] Data = Array.Empty<byte>();
    }

    public class ModernPacketHeader
    {
        public int PacketSize { get; set; }
        public byte[] Tag = new byte[12];

        public void Read(PacketReader reader)
        {
            PacketSize = reader.ReadInt32();
            Tag = reader.ReadBytes(12);
        }

        public void Write(PacketWriter writer)
        {
            writer.WriteInt32(PacketSize);
            writer.WriteBytes(Tag);
        }
    }

    public class ModernClientPacket : ModernGamePacket
    {
        public ModernClientPacket(byte[] data)
        {
            using var reader = new PacketReader(data);
            Header.Read(reader);
            Opcode = reader.ReadUInt16();

            if (Header.PacketSize == 0)
                Data = reader.ReadBytes(Header.PacketSize);
            else
                Data = reader.ReadBytes(Header.PacketSize - 2);
        }
    }

    public class ModernServerPacket : ModernGamePacket
    {
        public ModernServerPacket(ModernPacketHeader header, IUniversalPacket message)
        {
            using (var writer = new PacketWriter())
            {
                var packet = message.BuildOutgoing(message);
                writer.WriteBytes(packet.GetData());

                Data = writer.GetData();
            }

            Header = header;
            Header.PacketSize = Data.Length + 2;
        }
    }
}
