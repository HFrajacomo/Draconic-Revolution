using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkPriorityQueue
{
    private List<ChunkDistance> queue; // Second queue that handles normal execution
    private List<ChunkDistance> backupQueue; // Cache queue to recalculate distances

    private HashSet<ChunkPos> chunks; // HashSet that contains all ChunkPos currently in the Queue

    private int?[] partition; // Array connecting index=distance marking where every distance partition starts
    private int[] elementsInPartition; // Array connection index=distance to the amount of elements in a partition
    private int lastPartitionDistance = -1;

    private ChunkPos playerPosition; // Player current Distance
    private DistanceMetric metric;

    private bool DEBUG;

    public ChunkPriorityQueue(int renderDistance, bool debug=false, DistanceMetric metric=DistanceMetric.MANHATTAN){
        this.queue = new List<ChunkDistance>();
        this.chunks = new HashSet<ChunkPos>();
        this.partition = new int?[renderDistance*5];
        this.elementsInPartition = new int[renderDistance*5];
        this.metric = metric;
        this.DEBUG = debug;
    }

    public void Add(ChunkPos x){
        if(this.Contains(x)){
            return;
        }

        int distance = playerPosition.DistanceFrom(x, this.metric);

        // Blocks chunks from too far away from loading
        if(distance >= this.partition.Length)
            return;

        // Empty Queue
        if(this.queue.Count == 0){
            CreateNewPartition(distance);

            this.queue.Add(new ChunkDistance(x, distance));
            this.chunks.Add(x);
            return;
        }

        // If partition is new
        if(this.partition[distance] == null){
            if(!CreateNewPartition(distance)){
                ShiftAllAbove(distance);

                this.queue.Insert((int)this.partition[distance], new ChunkDistance(x, distance));
                this.chunks.Add(x);
            }
            else{
                this.queue.Insert((int)this.partition[distance], new ChunkDistance(x, distance));
                this.chunks.Add(x);
            }
        }
        // If partition already exists
        else{
            if(distance != this.lastPartitionDistance){
                ShiftAllAbove(distance);
                
                this.queue.Insert((int)this.partition[distance] + this.elementsInPartition[distance], new ChunkDistance(x, distance));
            }
            else{
                this.queue.Add(new ChunkDistance(x, distance));
            }

            this.chunks.Add(x);
            this.elementsInPartition[distance]++;
        }
    }

    public bool Contains(ChunkPos x){
        return this.chunks.Contains(x);
    }

    public int GetSize(){
        return this.queue.Count;
    }

    public ChunkPos Peek(){
        if(GetSize() > 0)
            return this.queue[0].pos;

        return playerPosition;
    }

    public float PeekDistance(){
        if(GetSize() > 0)
            return this.queue[0].distance;

        return -1;
    } 

    public ChunkPos Pop(){
        ChunkPos aux = this.queue[0].pos;
        int distance = this.queue[0].distance;

        if(this.elementsInPartition[distance] == 1){
            if(!DeletePartition(distance)){
                ShiftAllBelow(distance);
            }
        }
        else{
            this.elementsInPartition[distance]--;
            ShiftAllBelow(distance);
        }

        this.queue.RemoveAt(0);
        this.chunks.Remove(aux);
        return aux;            
    }

    public void Remove(ChunkPos x){
        if(!Contains(x))
            return;

        int dist = playerPosition.DistanceFrom(x, this.metric);

        for(int i=(int)this.partition[dist]; i < (int)this.partition[dist]+this.elementsInPartition[dist]; i++){
            if(this.queue[i].pos == x){
                this.queue.RemoveAt(i);
                this.chunks.Remove(x);

                if(this.elementsInPartition[dist] == 1){
                    if(!DeletePartition(dist)){
                        ShiftAllBelow(dist);
                    }
                }
                else{
                    this.elementsInPartition[dist]--;
                    ShiftAllBelow(dist);
                }
                return;
            }
        }        
    }

    public void Clear(){
        this.queue.Clear();
        this.chunks.Clear();
        this.lastPartitionDistance = -1;
        Array.Fill(this.partition, null);
        Array.Fill(this.elementsInPartition, 0);
    }

    public void SetPlayerPosition(ChunkPos pos){
        this.playerPosition = pos;

        RenewDistances();
    }

    private void RenewDistances(){
        ChangeReference(ref this.queue, ref this.backupQueue);

        this.chunks.Clear();
        Array.Fill(this.partition, null);
        Array.Fill(this.elementsInPartition, 0);
        this.lastPartitionDistance = -1;

        for(int i=0; i < this.backupQueue.Count; i++){
            Add(this.backupQueue[i].pos);
        }

        this.backupQueue.Clear();
    }

    // Shifts forward the partition position of all valid partition
    private void ShiftAllAbove(int distance){
        for(int i=distance+1; i <= this.lastPartitionDistance; i++){
            if(this.partition[i] == null)
                continue;

            this.partition[i]++;
        }
    }

    // Shifts backwards the partition position of all valid partition
    private void ShiftAllBelow(int distance){
        for(int i=distance+1; i <= this.lastPartitionDistance; i++){
            if(this.partition[i] == null)
                continue;

            this.partition[i]--;
        }
    }

    // Finds the latest partition index and increments one
    // Returns true if is creating a new last partition
    private bool CreateNewPartition(int distance){
        if(distance > this.lastPartitionDistance){
            this.partition[distance] = this.queue.Count;
            this.elementsInPartition[distance] = 1;
            this.lastPartitionDistance = distance;
            return true;
        }
        else{
            for(int i=distance+1; i <= this.lastPartitionDistance; i++){
                if(this.partition[i] == null)
                    continue;

                this.partition[distance] = this.partition[i];
                this.elementsInPartition[distance] = 1;
                return false;
            }

            this.partition[distance] = this.queue.Count;
            this.elementsInPartition[distance] = 1;
            this.lastPartitionDistance = distance;
            return true;
        }
    }

    // Deletes an entire partition
    // Returns true if was deleting the last partition
    private bool DeletePartition(int distance){
        if(distance == this.lastPartitionDistance){
            this.partition[distance] = null;
            this.elementsInPartition[distance] = 0;

            for(int i=distance-1; i >= 0; i--){
                if(this.partition[i] == null)
                    continue;

                this.partition[distance] = this.partition[i];
                this.lastPartitionDistance = i;
                return true;
            }

            this.lastPartitionDistance = -1;
            return true;
        }
        else{
            this.partition[distance] = null;
            this.elementsInPartition[distance] = 0;
            return false;
        }
    }

    private void ChangeReference(ref List<ChunkDistance> queue, ref List<ChunkDistance> backupQueue){
        backupQueue = queue;
        queue = new List<ChunkDistance>();
    }

    public void Print(){
        string a = "[";

        for(int i=0; i < this.queue.Count; i++)
            a += this.queue[i] + ", ";

        a += "]";

        Debug.Log(a);
    }
}

public struct ChunkDistance{
    public ChunkPos pos;
    public int distance;

    public ChunkDistance(ChunkPos pos, int distance){
        this.pos = pos;
        this.distance = distance;
    }

    public override string ToString(){
        return this.pos.ToString() + " -> " + this.distance;
    }
}