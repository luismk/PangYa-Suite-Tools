using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaRescuePwdLog
{
    public long Index { get; set; }

    public int Uid { get; set; }

    public byte Tipo { get; set; }

    public Guid KeyUniq { get; set; }

    public byte State { get; set; }

    public DateTime SendDate { get; set; }
}
