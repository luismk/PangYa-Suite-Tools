using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaPremioindicacaoLog
{
    public int LogId { get; set; }

    public int AdmUid { get; set; }

    public int Uid { get; set; }

    public int Cash { get; set; }

    public int CookiePoint { get; set; }

    public int Pangs { get; set; }

    public int ItemTypeid1 { get; set; }

    public int ItemTypeid2 { get; set; }

    public int ItemTypeid3 { get; set; }

    public int ItemTypeid4 { get; set; }

    public int ItemTypeid5 { get; set; }

    public int ItemQntd1 { get; set; }

    public int ItemQntd2 { get; set; }

    public int ItemQntd3 { get; set; }

    public int ItemQntd4 { get; set; }

    public int ItemQntd5 { get; set; }

    public DateTime LogDate { get; set; }
}
