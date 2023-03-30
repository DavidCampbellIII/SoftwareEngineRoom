using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    private float testFloat;

    private void Start()
    {
        Test();
    }

    public void Test()
    {
        testFloat += 1;
    }
}
