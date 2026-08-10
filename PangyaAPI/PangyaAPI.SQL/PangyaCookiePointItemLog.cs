using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaCookiePointItemLog
{
    public long Index { get; set; }

    public long? CpIdLog { get; set; }

    public int? Typeid { get; set; }

    public int? Qnty { get; set; }

    public long? Price { get; set; }
}
