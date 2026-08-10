using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaPersonalShopConfig
{
    public int Index { get; set; }

    public string Name { get; set; }

    public int Id { get; set; }

    public int Price { get; set; }

    public DateTime? RegDate { get; set; }
}
