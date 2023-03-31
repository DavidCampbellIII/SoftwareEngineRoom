using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Machine
{
    public string Name { get; set; }
    public List<Connection> Connections { get; set; }

    public Machine(string name)
    {
        Name = name;
        Connections = new List<Connection>();
    }
}
