using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravel_Block : Blocks
{
    public Gravel_Block(){
        this.name = "Gravel";
        this.solid = true;
        this.transparent = 0;
        this.invisible = false;
        this.liquid = false;
        this.affectLight = true;

        this.tileTop = 30;
        this.tileSide = 30;
        this.tileBottom = 30;

        this.maxHP = 80;
    }
}
