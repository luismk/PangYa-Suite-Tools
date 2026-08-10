using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class ManiaCookie
{
    public int Id { get; set; }

    public string CpDescription { get; set; }

    public int? CpValue { get; set; }

    public decimal? CpPrice { get; set; }
}
