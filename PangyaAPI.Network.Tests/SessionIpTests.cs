using System.Net;
using PangyaAPI.Network.PangyaSession;
using Xunit;

namespace PangyaAPI.Network.Tests;

public sealed class SessionIpTests
{
    [Fact]
    public void GetIp_WithoutEndpoint_ReturnsUnspecifiedAddress()
    {
        var session = new TestSession();

        Assert.Equal("0.0.0.0", session.getIP());
    }

    [Fact]
    public void Clear_PreservesLastResolvedAddressForLateDisconnectLogging()
    {
        var session = new TestSession
        {
            m_addr = new IPEndPoint(IPAddress.Parse("192.0.2.10"), 20201)
        };

        Assert.Equal("192.0.2.10", session.getIP());

        session.clear();

        Assert.Equal("192.0.2.10", session.getIP());
    }

    [Fact]
    public void GetIp_MapsIpv4MappedIpv6AddressToDottedIpv4()
    {
        var session = new TestSession
        {
            m_addr = new IPEndPoint(IPAddress.Parse("::ffff:192.0.2.11"), 20201)
        };

        Assert.Equal("192.0.2.11", session.getIP());
    }

    private sealed class TestSession : Session
    {
        public override byte getStateLogged() => 0;
        public override uint getUID() => 0;
        public override uint getCapability() => 0;
        public override string getNickname() => string.Empty;
        public override string getID() => string.Empty;
    }
}
