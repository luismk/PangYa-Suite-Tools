using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaWorldTourConfig
{
    public int EventId { get; set; }

    public string Name { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
