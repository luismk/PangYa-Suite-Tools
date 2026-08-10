using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGmGiftWebLog
{
    public long Index { get; set; }

    public int GmUid { get; set; }

    public int PlayerUid { get; set; }

    public int MsgId { get; set; }

    public DateTime RegDate { get; set; }
}
