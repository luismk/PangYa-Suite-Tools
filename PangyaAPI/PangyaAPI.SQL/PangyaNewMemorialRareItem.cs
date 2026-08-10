using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaNewMemorialRareItem
{
    public int? ItemTipo { get; set; }

    public string ItemNome { get; set; }

    public int? ItemTypeid { get; set; }

    public int? ItemProbabilidade { get; set; }

    public int? ItemGachaNumber { get; set; }

    public int? ItemActive { get; set; }

    public int? CoinTypeid { get; set; }
}
