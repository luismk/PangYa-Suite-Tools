using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaBotGmEventTime
{
    public long Index { get; set; }

    public TimeOnly InicioTime { get; set; }

    public TimeOnly FimTime { get; set; }

    public byte ChannelId { get; set; }

    public byte Valid { get; set; }

    public DateTime RegDate { get; set; }
}
