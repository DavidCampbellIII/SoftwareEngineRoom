using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room
{
    public string Name { get; set; }
    public List<Machine> Machines { get; set; }

    public Room(string name)
    {
        Name = name;
        Machines = new List<Machine>();
    }
}
