using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGachaUserWon
{
    public int Index { get; set; }

    public int GachaNum { get; set; }

    public int Uid { get; set; }

    public string ItemName { get; set; }

    public int ItemTypeid { get; set; }

    public DateTime? GetDate { get; set; }
}
