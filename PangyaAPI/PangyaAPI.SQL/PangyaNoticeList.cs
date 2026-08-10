using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaNoticeList
{
    public long NoticeId { get; set; }

    public string Message { get; set; }

    public int ReplayCount { get; set; }

    public int RefreshTime { get; set; }
}
