using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaDonationEpin
{
    public long Index { get; set; }

    public long DonationId { get; set; }

    public int Uid { get; set; }

    public Guid Epin { get; set; }

    public long Qntd { get; set; }

    public int? RetriveUid { get; set; }

    public byte Valid { get; set; }
}
