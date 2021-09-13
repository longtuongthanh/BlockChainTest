using System;
using System.Collections.Generic;
using System.Text;

public static partial class Extensions
{
    public static void WriteMessage(this object x)
    {
        Console.WriteLine(x);
    }
}
