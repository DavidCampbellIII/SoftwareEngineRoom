using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Machine
{
    public string Name { get; set; }
    public List<Machine> inputs { get; }
    public List<Machine> outputs { get; }

    public Machine(string name)
    {
        Name = name;
        inputs = new List<Machine>();
        outputs = new List<Machine>();
    }
}
