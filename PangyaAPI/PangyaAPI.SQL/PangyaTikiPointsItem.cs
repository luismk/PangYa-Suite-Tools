using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaTikiPointsItem
{
    public int Index { get; set; }

    public string ItemName { get; set; }

    public int ItemTypeid { get; set; }

    public int ItemQntd { get; set; }

    public int? ReqPoints { get; set; }

    public int? ItemFlag { get; set; }

    public int? ItemActive { get; set; }

    public DateTime? RegDate { get; set; }
}
