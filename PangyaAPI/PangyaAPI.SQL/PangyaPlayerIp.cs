using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaPlayerIp
{
    public long Index { get; set; }

    public int Uid { get; set; }

    public string Ip { get; set; }

    public byte BlockBeta { get; set; }

    public short FlagDay { get; set; }

    public int ChangeCount { get; set; }

    public DateTime? ChangeDate { get; set; }
}
