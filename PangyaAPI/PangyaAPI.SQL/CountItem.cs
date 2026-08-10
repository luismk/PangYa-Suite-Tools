using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class CountItem
{
    public int Uid { get; set; }

    public string Nome { get; set; }

    public int IdAchievement { get; set; }

    public int TypeId { get; set; }

    public int CountId { get; set; }

    public long CountNumItem { get; set; }

    public int DataSec { get; set; }

    public short Tipo { get; set; }
}
