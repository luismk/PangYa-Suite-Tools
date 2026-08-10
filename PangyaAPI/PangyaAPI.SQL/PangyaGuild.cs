using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGuild
{
    public long GuildUid { get; set; }

    public string GuildId { get; set; }

    public string GuildName { get; set; }

    public int GuildLeader { get; set; }

    public int GuildSubMaster { get; set; }

    public short GuildConditionLevel { get; set; }

    public byte GuildState { get; set; }

    public byte GuildFlag { get; set; }

    public byte GuildPermitionJoin { get; set; }

    public long GuildPang { get; set; }

    public long GuildPoint { get; set; }

    public int GuildWin { get; set; }

    public int GuildLose { get; set; }

    public int GuildDraw { get; set; }

    public string GuildMarkImg { get; set; }

    public int GuildMarkImgIdx { get; set; }

    public int GuildNewMarkIdx { get; set; }

    public string GuildIntroImg { get; set; }

    public string GuildNotice { get; set; }

    public string GuildInfo { get; set; }

    public DateTime GuildRegDate { get; set; }

    public DateTime? GuildAcceptDate { get; set; }

    public DateTime? GuildClosureDate { get; set; }
}
