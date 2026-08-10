using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaPapelShopConfig
{
    public int Numero { get; set; }

    public long PriceNormal { get; set; }

    public long PriceBig { get; set; }

    public byte LimittedYn { get; set; }

    public DateTime? UpdateDate { get; set; }
}
