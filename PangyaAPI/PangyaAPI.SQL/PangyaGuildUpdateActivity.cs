using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGuildUpdateActivity
{
    public long Index { get; set; }

    public int GuildUid { get; set; }

    public int OwnerUpdate { get; set; }

    public int PlayerUid { get; set; }

    public byte TypeUpdate { get; set; }

    public byte State { get; set; }

    public DateTime RegDate { get; set; }
}
