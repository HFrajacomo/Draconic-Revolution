using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Moonstone_Block : Blocks
{
    public Moonstone_Block(){
        this.name = "Moonstone";
        this.solid = true;
        this.transparent = 0;
        this.invisible = false;
        this.liquid = false;
        this.affectLight = true;

        this.tileTop = 31;
        this.tileSide = 31;
        this.tileBottom = 31;

        this.maxHP = 400;
    }
}
