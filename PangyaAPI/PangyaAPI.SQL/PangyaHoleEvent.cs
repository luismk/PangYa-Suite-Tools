using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaHoleEvent
{
    public int Index { get; set; }

    public int Uid { get; set; }

    public int StartHoles { get; set; }

    public int ProcessHoles { get; set; }

    public int Status { get; set; }

    public DateTime? FinishDate { get; set; }
}
