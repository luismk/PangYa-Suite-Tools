using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaHioEvent
{
    public int Index { get; set; }

    public int Uid { get; set; }

    public int StartHios { get; set; }

    public int ProcessHios { get; set; }

    public int Status { get; set; }

    public DateTime? FinishDate { get; set; }
}
