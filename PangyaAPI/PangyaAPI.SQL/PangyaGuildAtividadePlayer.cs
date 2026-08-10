using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGuildAtividadePlayer
{
    public long Idx { get; set; }

    public int Uid { get; set; }

    public int GuildUid { get; set; }

    public int Flag { get; set; }

    public DateTime RegDate { get; set; }
}
