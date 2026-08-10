using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaLuciaAttendance
{
    public int Uid { get; set; }

    public long CountDay { get; set; }

    public DateTime? LastDayAttendance { get; set; }

    public DateTime? LastDayGetItem { get; set; }

    public int TryHackingCount { get; set; }

    public byte BlockType { get; set; }

    public DateTime? BlockEndDate { get; set; }
}
