using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaDonationItemLog
{
    public long Index { get; set; }

    public long DonationId { get; set; }

    public int ItemTypeid { get; set; }

    public int ItemQntd { get; set; }
}
