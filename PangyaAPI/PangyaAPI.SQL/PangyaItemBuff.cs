using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaItemBuff
{
    public long Index { get; set; }

    public int Uid { get; set; }

    public int Typeid { get; set; }

    public DateTime RegDate { get; set; }

    public DateTime EndDate { get; set; }

    public short Tipo { get; set; }

    public int Percent { get; set; }

    public byte UseYn { get; set; }
}
