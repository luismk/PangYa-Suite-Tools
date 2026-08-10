using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGuildMember
{
    public int GuildUid { get; set; }

    public int MemberUid { get; set; }

    public string MemberMsg { get; set; }

    public int GuildPang { get; set; }

    public int GuildPoint { get; set; }

    public int MemberFlag { get; set; }

    public int MemberStateFlag { get; set; }

    public DateTime RegDate { get; set; }
}
