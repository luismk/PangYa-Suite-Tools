using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGuildRanking
{
    public long Index { get; set; }

    public int GuildUid { get; set; }

    public int Rank { get; set; }

    public int LastRank { get; set; }

    public DateTime RegDate { get; set; }
}
