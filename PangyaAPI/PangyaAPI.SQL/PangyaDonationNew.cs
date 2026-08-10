using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaDonationNew
{
    public long Index { get; set; }

    public int Uid { get; set; }

    public byte Plataforma { get; set; }

    public string Email { get; set; }

    public DateTime Date { get; set; }

    public DateTime? Update { get; set; }

    public string Code { get; set; }

    public byte Type { get; set; }

    public byte Status { get; set; }

    public string Reference { get; set; }

    public double GrossAmount { get; set; }

    public double NetAmount { get; set; }

    public DateTime? Escrow { get; set; }

    public long EpinId { get; set; }

    public DateTime RegDate { get; set; }
}
