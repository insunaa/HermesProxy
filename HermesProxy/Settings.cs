using HermesProxy.Enums;

namespace HermesProxy
{
    public static class Settings
    {
        static readonly Configuration Conf = new();

        public static readonly ClientVersionBuild ClientBuild = Conf.GetEnum("ClientBuild", ClientVersionBuild.V2_5_2_40892);
        public static readonly ClientVersionBuild ServerBuild = Conf.GetEnum("ServerBuild", ClientVersionBuild.V2_4_3_8606);

        public static byte GetMajorPatchVersion(bool client = false)
        {
            var str = (client ? ClientBuild.ToString() : ServerBuild.ToString()).Replace("V", "");
            var split = str.Split('_');
            return (byte)uint.Parse(split[0]);
        }

        public static byte GetMinorPatchVersion(bool client = false)
        {
            var str = (client ? ClientBuild.ToString() : ServerBuild.ToString()).Replace("V", "");
            var split = str.Split('_');
            return (byte)uint.Parse(split[1]);
        }

        public static byte GetRevisionPatchVersion(bool client = false)
        {
            var str = (client ? ClientBuild.ToString() : ServerBuild.ToString()).Replace("V", "");
            var split = str.Split('_');
            return (byte)uint.Parse(split[2]);
        }

        public static int GetBuild(bool client = false)
        {
            return (int)(client ? ClientBuild : ServerBuild);
        }

        public static readonly string ServerAddress = Conf.GetString("ServerAddress", "127.0.0.1");
    }
}
