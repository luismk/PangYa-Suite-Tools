using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaCommand
{
    public long Idx { get; set; }

    public int CommandId { get; set; }

    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public int Arg3 { get; set; }

    public int Arg4 { get; set; }

    public int Arg5 { get; set; }

    public int Target { get; set; }

    public DateTime RegDate { get; set; }

    public DateTime? ReserveDate { get; set; }

    public short Flag { get; set; }

    public short Valid { get; set; }
}
