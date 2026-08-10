using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaDonationLog
{
    public long Index { get; set; }

    /// <summary>
    /// quem registrou a do doação para o usuário
    /// </summary>
    public int AdmUid { get; set; }

    public int Uid { get; set; }

    /// <summary>
    /// 0 nenhum, 1 Paypal, 2 PagSeguro
    /// </summary>
    public byte Plataforma { get; set; }

    public int Cash { get; set; }

    public int CookiePoint { get; set; }

    public string Email { get; set; }

    public string Obs { get; set; }

    public DateTime RedDate { get; set; }
}
