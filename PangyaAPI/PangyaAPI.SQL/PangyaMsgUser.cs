using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaMsgUser
{
    public long MsgIdx { get; set; }

    public int Uid { get; set; }

    public int UidFrom { get; set; }

    public short Valid { get; set; }

    public string Msg { get; set; }

    public DateTime RegDate { get; set; }
}
