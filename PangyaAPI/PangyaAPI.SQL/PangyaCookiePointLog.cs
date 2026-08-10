using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaCookiePointLog
{
    public long Id { get; set; }

    public int? Uid { get; set; }

    public byte? Type { get; set; }

    public int? MailId { get; set; }

    public long? Cookie { get; set; }

    public int? ItemQnty { get; set; }

    public DateTime RegDate { get; set; }
}
