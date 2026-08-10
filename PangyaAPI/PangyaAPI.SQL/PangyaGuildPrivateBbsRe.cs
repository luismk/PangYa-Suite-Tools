using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGuildPrivateBbsRe
{
    public long Seq { get; set; }

    public long GuildBbsSeq { get; set; }

    public int OwnerUid { get; set; }

    public string Text { get; set; }

    public byte State { get; set; }

    public DateTime? RegDate { get; set; }
}
