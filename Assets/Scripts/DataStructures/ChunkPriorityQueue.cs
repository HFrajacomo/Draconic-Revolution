using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkPriorityQueue
{
    private List<ChunkPos> queue;
    private ChunkPos playerPosition;

    public ChunkPriorityQueue(){
        this.queue = new List<ChunkPos>();
    }

    public void Add(ChunkPos x){
        int distance = playerPosition.DistanceFrom(x);

        // Empty Queue
        if(this.queue.Count == 0){
            this.queue.Add(x);
            return;
        }

        for(int i=0; i < this.queue.Count; i++){
            if(playerPosition.DistanceFrom(this.queue[i]) > distance){
                if(i == 0){
                    this.queue.Insert(0, x);
                    return;
                }
                else{
                    this.queue.Insert(i, x);
                    return;
                }
            }
        }

        this.queue.Add(x);
    }

    public bool Contains(ChunkPos x){
        return this.queue.Contains(x);
    }

    public int GetSize(){
        return this.queue.Count;
    }

    public void Remove(ChunkPos x){
        if(Contains(x)){
            this.queue.Remove(x);
        }
    }

    public ChunkPos Peek(){
        if(this.queue.Count > 0)
            return this.queue[0];
        else
            return this.playerPosition;
    }

    public ChunkPos Pop(){
        ChunkPos aux = this.queue[0];
        this.queue.RemoveAt(0);
        return aux;
    }

    public void Clear(){
        this.queue.Clear();
    }

    public void SetPlayerPosition(ChunkPos pos){
        this.playerPosition = pos;
    }
}
