using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaNewCardPack
{
    public int Index { get; set; }

    public string Name { get; set; }

    public int Typeid { get; set; }

    public short Quantidade { get; set; }

    public short Tipo { get; set; }

    public short RateN { get; set; }

    public short RateR { get; set; }

    public short RateSr { get; set; }

    public short RateSc { get; set; }
}
