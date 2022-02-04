using System.Collections.Generic;

namespace HermesProxy.Framework.IO.Packet.Messages;

public interface IUniversalPacket
{
    public PacketWriter BuildOutgoing(IUniversalPacket packet);
    public void ConvertIncoming(PacketReader reader, List<IUniversalPacket> structures);
}
