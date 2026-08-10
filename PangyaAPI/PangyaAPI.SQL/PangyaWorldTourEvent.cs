using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaWorldTourEvent
{
    public int Index { get; set; }

    public int Uid { get; set; }

    public int Course { get; set; }

    public int Completed { get; set; }

    public DateTime? FinishData { get; set; }
}
