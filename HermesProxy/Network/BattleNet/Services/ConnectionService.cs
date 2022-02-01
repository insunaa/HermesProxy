using System;
using System.Threading.Tasks;

using Bgs.Protocol;
using Bgs.Protocol.Connection.V1;

using HermesProxy.Framework.Constants;
using HermesProxy.Framework.Util;
using HermesProxy.Network.BattleNet.Services;

namespace HermesProxy.Network.BattleNet.Session
{
    public partial class BattlenetSession
    {
        [BattlenetService(ServiceHash.ConnectionService, 1)]
        public BattlenetRpcErrorCode HandleConnectRequest(ConnectRequest request, ConnectResponse response)
        {
            if (request.ClientId != null)
                response.ClientId.MergeFrom(request.ClientId);

            response.ServerId = new()
            {
                Label = (uint)Environment.ProcessId,
                Epoch = (uint)Time.UnixTime
            };
            response.ServerTime = (ulong)Time.UnixTimeMilliseconds;
            response.UseBindlessRpc = request.UseBindlessRpc;

            return BattlenetRpcErrorCode.Ok;
        }

        [BattlenetService(ServiceHash.ConnectionService, 5)]
        public BattlenetRpcErrorCode HandleKeepAlive(NoData request) => BattlenetRpcErrorCode.Ok;

        [BattlenetService(ServiceHash.ConnectionService, 7)]
        public async Task<BattlenetRpcErrorCode> HandleDisconnectRequest(DisconnectRequest request)
        {
            await SendRequest(ServiceHash.ConnectionService, 4, new DisconnectNotification
            {
                ErrorCode = request.ErrorCode,
            });

            await CloseSocket();
            return BattlenetRpcErrorCode.Ok;
        }
    }
}
