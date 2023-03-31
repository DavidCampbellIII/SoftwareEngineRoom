using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Machine
{
    public string ID { get; }
    public List<Machine> inputs { get; }
    public List<Machine> outputs { get; }

    public string Name { get; }

    public Machine(string id)
    {
        ID = id;
        inputs = new List<Machine>();
        outputs = new List<Machine>();

        Name = id.Split('.').Last();
    }
}
