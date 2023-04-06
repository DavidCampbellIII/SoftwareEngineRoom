using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionsTest
{
    public static void Test(this string str)
    {
        int numChars = str.Length;
    }
}
