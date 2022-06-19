using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snow_Block : Blocks
{
    public Snow_Block(){
        this.name = "Snow";
        this.solid = true;
        this.transparent = 0;
        this.invisible = false;
        this.liquid = false;
        this.affectLight = true;

        this.tileTop = 9;
        this.tileSide = 9;
        this.tileBottom = 9;

        this.maxHP = 50;
    }
}
