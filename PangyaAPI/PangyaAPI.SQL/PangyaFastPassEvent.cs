using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaFastPassEvent
{
    public int Uid { get; set; }

    public byte HolesInit { get; set; }

    public long HolesCounter { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime RegDate { get; set; }
}
