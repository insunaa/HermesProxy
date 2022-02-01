namespace HermesProxy.Framework.IO.Packet.Messages
{
    public interface IWritable
    {
        public void Write(PacketWriter writer);
    }
}
