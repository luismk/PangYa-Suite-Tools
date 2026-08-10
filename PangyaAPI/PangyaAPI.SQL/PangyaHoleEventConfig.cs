using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaHoleEventConfig
{
    public int? EventId { get; set; }

    public DateTime StartEvent { get; set; }

    public DateTime EndEvent { get; set; }
}
