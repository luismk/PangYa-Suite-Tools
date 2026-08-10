using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class TdCharEquipS4
{
    public long Seq { get; set; }

    public int Uid { get; set; }

    public int CharItemid { get; set; }

    public int Itemid { get; set; }

    public DateTime? InDate { get; set; }

    public int EquipNum { get; set; }

    public int EquipType { get; set; }

    public string UseYn { get; set; }
}
