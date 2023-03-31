using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Connection
{
    public Machine Source { get; set; }
    public Machine Target { get; set; }

    public Connection(Machine source, Machine target)
    {
        Source = source;
        Target = target;
    }
}
