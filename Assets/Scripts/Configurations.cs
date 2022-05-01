using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Configurations
{
    public static bool FULLBRIGHT = true;

    public static int GetFullbright(){
        if(Configurations.FULLBRIGHT)
            return 1;
        else
            return 0;
    }
}
