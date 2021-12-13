using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvailabilityQueue
{
    private List<ulong> queue;

    public AvailabilityQueue(){
        this.queue = new List<ulong>(){0};
    }

    public void Add(ulong item){
        if(this.queue.Count == 0)
            this.queue.Add(item);
        else
            this.Insert(this.queue.BinarySearch(item), item);
    }

    public ulong Pop(){
        if(this.Count() == 1)
            this.queue.Add(this.queue[0]+1);

        ulong item = this.queue[0];
        this.queue.RemoveAt(0);
        return item;
    }

    private void Insert(int index, ulong item){
        this.queue.Insert(index, item);
    }

    public int Count(){
        return this.queue.Count;
    }
}
