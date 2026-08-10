using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGuildNotice
{
    public long Seq { get; set; }

    public int GuildUid { get; set; }

    public int OwnerUid { get; set; }

    public string Title { get; set; }

    public string Text { get; set; }

    public byte State { get; set; }

    public long Views { get; set; }

    public DateTime RegDate { get; set; }
}
