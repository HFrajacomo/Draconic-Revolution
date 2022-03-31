using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sand_Block : Blocks
{
    public Sand_Block(){
        this.name = "Sand";
        this.solid = true;
        this.transparent = 0;
        this.invisible = false;
        this.liquid = false;
        this.affectLight = true;

        this.tileTop = 8;
        this.tileSide = 8;
        this.tileBottom = 8;    
    }
}
