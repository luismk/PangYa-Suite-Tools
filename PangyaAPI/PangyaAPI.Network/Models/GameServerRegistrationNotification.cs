using System;
using System.Collections.Generic;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.BinaryModels;

namespace PangyaAPI.Network.Models
{
    public static class GameServerRegistrationNotification
    {
        public const byte PacketId = 0x0F;
        public const int MaximumServerCount = 1024;

        public static void WritePayload(PangyaBinaryWriter writer, IReadOnlyList<int> serverIds)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(serverIds);

            if (serverIds.Count == 0 || serverIds.Count > MaximumServerCount)
                throw new ArgumentOutOfRangeException(nameof(serverIds));

            writer.WriteUInt16(checked((ushort)serverIds.Count));
            for (var index = 0; index < serverIds.Count; index++)
            {
                if (serverIds[index] <= 0)
                    throw new ArgumentOutOfRangeException(nameof(serverIds));

                writer.WriteInt32(serverIds[index]);
            }
        }

        public static IReadOnlyList<int> ReadPayload(packet packet)
        {
            ArgumentNullException.ThrowIfNull(packet);

            if (packet.BytesRemaining < sizeof(ushort))
                throw InvalidPayload("missing server count");

            var count = packet.ReadUInt16();
            if (count == 0 || count > MaximumServerCount)
                throw InvalidPayload("invalid server count");

            if (packet.BytesRemaining != count * sizeof(int))
                throw InvalidPayload("payload length does not match server count");

            var serverIds = new int[count];
            var uniqueIds = new HashSet<int>();
            for (var index = 0; index < count; index++)
            {
                var serverId = packet.ReadInt32();
                if (serverId <= 0 || !uniqueIds.Add(serverId))
                    throw InvalidPayload("server IDs must be positive and unique");

                serverIds[index] = serverId;
            }

            return serverIds;
        }

        private static exception InvalidPayload(string reason)
            => new exception(
                "[GameServerRegistrationNotification][Error] " + reason + ".",
                ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.UNIT_AUTH_SERVER_CONNECT, 6, 0));
    }
}
