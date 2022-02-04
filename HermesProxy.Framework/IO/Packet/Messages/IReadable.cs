namespace HermesProxy.Framework.IO.Packet.Messages;

public interface IReadable
{
    public void Read(PacketReader reader);
}
