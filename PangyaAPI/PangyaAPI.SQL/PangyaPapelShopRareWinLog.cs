using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaPapelShopRareWinLog
{
    public long Index { get; set; }

    public int Uid { get; set; }

    public int Typeid { get; set; }

    public int Qntd { get; set; }

    public byte BallColor { get; set; }

    public int Probabilidade { get; set; }

    public DateTime RegDate { get; set; }
}
