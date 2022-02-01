using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;

using Bgs.Protocol;
using Bgs.Protocol.GameUtilities.V1;
using Google.Protobuf;

using HermesProxy.Framework.Constants;
using HermesProxy.Framework.Logging;
using HermesProxy.Framework.Util;
using HermesProxy.Network.BattleNet.REST;

namespace HermesProxy.Network.Realm
{
    public static class RealmManager
    {
        public static List<RealmInfo> Realms { get; set; } = new();

        /// <summary>
        /// Adds a <see cref="RealmInfo"/> instance to the <see cref="Realms"/> instance.
        /// </summary>
        public static void AddRealm(RealmInfo realmInfo) 
            => Realms.Add(realmInfo);

        /// <summary>
        /// Write all Subregions to <see cref="GetAllValuesForAttributeResponse"/>
        /// </summary>
        public static void WriteSubRegions(GetAllValuesForAttributeResponse response)
        {
            foreach (var realmInfo in Realms)
            {
                response.AttributeValue.Add(new Variant
                {
                    StringValue = $"0-0-{realmInfo.ID}"                 //< Constructed from Region-Site-Realm
                });
            }
        }

        /// <summary>
        /// Compress and return the <see cref="RealmInfo"/> instance.
        /// </summary>
        public static byte[] GetRealmList(uint build)
        {
            var realmList = new RealmListUpdates();
            foreach (var realmInfo in Realms)
            {
                var flag = realmInfo.Flags;
                if (Settings.GetBuild(client: true) != build)
                    flag |= RealmFlags.VersionMisMatch;

                var realmListUpdate = new RealmListUpdate
                {
                    Update = new()
                };
                realmListUpdate.Update.WowRealmAddress = realmInfo.ID;
                realmListUpdate.Update.CfgTimezonesID = 1;
                realmListUpdate.Update.PopulationState = realmInfo.Flags.HasFlag(RealmFlags.Offline) ? 0 : Math.Max((int)realmInfo.Population, 1);
                realmListUpdate.Update.CfgCategoriesID = realmInfo.Timezone;

                realmListUpdate.Update.Version = new();
                realmListUpdate.Update.Version.Major = Settings.GetMajorPatchVersion(client: true);
                realmListUpdate.Update.Version.Minor = Settings.GetMinorPatchVersion(client: true);
                realmListUpdate.Update.Version.Revision = Settings.GetRevisionPatchVersion(client: true);
                realmListUpdate.Update.Version.Build = Settings.GetBuild(client: true);

                realmListUpdate.Update.CfgRealmsID = realmInfo.ID;
                realmListUpdate.Update.Flags = (int)flag;
                realmListUpdate.Update.Name = realmInfo.Name;
                realmListUpdate.Update.CfgConfigsID = 1;
                realmListUpdate.Update.CfgLanguagesID = 1;

                realmListUpdate.Deleting = false;

                realmList.Updates.Add(realmListUpdate);
            }
            return JSON.Deflate("JSONRealmListUpdates", realmList);
        }

        /// <summary>
        /// Handles the JoinRealm message from the client.
        /// </summary>
        public static BattlenetRpcErrorCode JoinRealm(uint realmAddress, uint build, IPAddress clientAddress, byte[] clientSecret, ClientResponse response)
        {
            var realm = Realms.Find(x => x.ID == realmAddress);
            if (realm == null)
                return BattlenetRpcErrorCode.UtilServerUnknownRealm;

            if (realm.Flags.HasFlag(RealmFlags.Offline) || realm.Flags.HasFlag(RealmFlags.VersionMisMatch))
                return BattlenetRpcErrorCode.UserServerNotPermittedOnRealm;

            var realmListServerIPAddress = new RealmListServerIPAddresses();
            var addressFamily = new AddressFamily
            {
                Id = 1
            };
            addressFamily.Addresses.Add(new()
            {
                Ip = realm.GetAddressForClient(clientAddress).Address.ToString(),
                Port = realm.Port,
            });
            realmListServerIPAddress.Families.Add(addressFamily);

            var compressed = JSON.Deflate("JSONRealmListServerIPAddresses", realmListServerIPAddress);

            var serverSecret = RandomNumberGenerator.GetBytes(32);
            var keyData = clientSecret.Combine(serverSecret);

            response.Attribute.Add(new Bgs.Protocol.Attribute
            {
                Name = "Param_RealmJoinTicket",
                Value = new Variant
                {
                    BlobValue = ByteString.CopyFrom("Wow1", Encoding.UTF8)
                }
            });

            response.Attribute.Add(new Bgs.Protocol.Attribute
            {
                Name = "Param_ServerAddresses",
                Value = new Variant
                {
                    BlobValue = ByteString.CopyFrom(compressed)
                }
            });

            response.Attribute.Add(new Bgs.Protocol.Attribute
            {
                Name = "Param_JoinSecret",
                Value = new Variant
                {
                    BlobValue = ByteString.CopyFrom(serverSecret)
                }
            });

            return BattlenetRpcErrorCode.Ok;
        }

        /// <summary>
        /// Prints the <see cref="RealmInfo"/> from the <see cref="Realms"/> instance.
        /// </summary>
        public static void PrintRealmList()
        {
            if (Realms.Count == 0)
                return;

            Log.Print(LogType.Debug, "");
            Log.Print(LogType.Debug, $"{"ID",-5} {"Type",-5} {"Locked",-8} {"Flags",-10} {"Name",-15} {"Address",-15} {"Port",-10} {"Build",-10}");

            foreach (var realm in Realms)
                Log.Print(LogType.Debug, realm.ToString());

            Log.Print(LogType.Debug,"");
        }
    }
}
