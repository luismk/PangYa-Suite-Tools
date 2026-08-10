using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.BinaryModels;
using Xunit;

namespace PangyaAPI.Network.Tests;

public sealed class GameServerRegistrationNotificationTests
{
    [Fact]
    public void Payload_RoundTripsServerIds()
    {
        using var writer = new PangyaBinaryWriter(GameServerRegistrationNotification.PacketId);
        GameServerRegistrationNotification.WritePayload(writer, new[] { 10, 20 });
        using var packet = new packet(writer.GetBytes);

        Assert.Equal(GameServerRegistrationNotification.PacketId, (ushort)packet.Id);
        Assert.Equal(new[] { 10, 20 }, GameServerRegistrationNotification.ReadPayload(packet));
    }

    [Fact]
    public void ReadPayload_RejectsLengthMismatch()
    {
        using var writer = new PangyaBinaryWriter(GameServerRegistrationNotification.PacketId);
        writer.WriteUInt16(2);
        writer.WriteInt32(10);
        using var packet = new packet(writer.GetBytes);

        Assert.Throws<exception>(() => GameServerRegistrationNotification.ReadPayload(packet));
    }

    [Fact]
    public void ReadPayload_RejectsDuplicateIds()
    {
        using var writer = new PangyaBinaryWriter(GameServerRegistrationNotification.PacketId);
        writer.WriteUInt16(2);
        writer.WriteInt32(10);
        writer.WriteInt32(10);
        using var packet = new packet(writer.GetBytes);

        Assert.Throws<exception>(() => GameServerRegistrationNotification.ReadPayload(packet));
    }
}
