using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaCommandGmLog
{
    public long Index { get; set; }

    public int CommandType { get; set; }

    public int GmUid { get; set; }

    public string GmNick { get; set; }

    public long Capability { get; set; }

    public string NickGift { get; set; }

    public int UidGift { get; set; }

    public int ItemTypeid { get; set; }

    public int ItemQntd { get; set; }

    public DateTime RegDate { get; set; }
}
