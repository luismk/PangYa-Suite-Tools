using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaPlayerLocation
{
    public int Uid { get; set; }

    public short Channel { get; set; }

    public short Lobby { get; set; }

    public short Room { get; set; }

    public short Place { get; set; }

    public Guid? RoomId { get; set; }
}
