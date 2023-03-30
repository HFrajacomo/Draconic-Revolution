using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvailabilityQueue
{
    private List<ulong> queue;
    private bool inverted;

    public AvailabilityQueue(bool inverted=false){
        if(inverted)
            this.queue = new List<ulong>(){ulong.MaxValue};
        else
            this.queue = new List<ulong>(){0};

        this.inverted = inverted;
    }

    public void Add(ulong item){
        if(this.queue.Count == 0)
            this.queue.Add(item);
        else
            this.Insert(TransformResult(this.queue.BinarySearch(item)), item);
    }

    public ulong Pop(){
        if(this.Count() == 1){
            if(!inverted)
                this.queue.Add(this.queue[0]+1);
            else
                this.queue.Add(this.queue[0]-1);
        }

        ulong item;

        if(!inverted){
            item = this.queue[0];
            this.queue.RemoveAt(0);
        }
        else{
            item = this.queue[this.queue.Count-1];
            this.queue.RemoveAt(this.queue.Count-1);
        }

        return item;
    }

    private void Insert(int index, ulong item){
        this.queue.Insert(index, item);
    }

    public int Count(){
        return this.queue.Count;
    }

    private int TransformResult(int index){
        if(index >= 0)
            return index;
        return (-index) - 1;
    }
}
