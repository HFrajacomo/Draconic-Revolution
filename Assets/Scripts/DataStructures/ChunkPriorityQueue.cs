using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkPriorityQueue
{
    private List<ChunkDistance> initialQueue; // Queue that processes only prioritary elements
    private List<ChunkDistance> queue; // Second queue that handles normal execution
    private List<ChunkDistance> backupQueue; // Cache queue to recalculate distances

    private HashSet<ChunkPos> chunks; // HashSet that contains all ChunkPos currently in the Queue

    private ChunkPos playerPosition; // Player current Distance
    private DistanceMetric metric;
    private bool DEBUG_QUEUE;

    public ChunkPriorityQueue(bool debug=false, DistanceMetric metric=DistanceMetric.MANHATTAN){
        this.queue = new List<ChunkDistance>();
        this.initialQueue = new List<ChunkDistance>();
        this.chunks = new HashSet<ChunkPos>();
        this.metric = metric;
        this.DEBUG_QUEUE = debug;
    }

    public void Add(ChunkPos x, bool initial=false){
        float distance = playerPosition.DistanceFrom(x, this.metric);
        float newDist = 0;

        if(this.Contains(x)){
            if(DEBUG_QUEUE)
                Debug.Log("already contains: " + x);
            return;
        }

        List<ChunkDistance> q;

        if(initial)
            q = this.initialQueue;
        else
            q = this.queue;

        // Empty Queue
        if(q.Count == 0){
            q.Add(new ChunkDistance(x, distance));
            this.chunks.Add(x);

            if(DEBUG_QUEUE)
                Debug.Log("added: " + x);
            return;
        }

        for(int i=0; i < q.Count; i++){
            newDist = playerPosition.DistanceFrom(q[i].pos, this.metric);
            if(distance < newDist){
                q.Insert(i, new ChunkDistance(x, distance));
                this.chunks.Add(x);

                if(DEBUG_QUEUE)
                    Debug.Log("added: " + x + " to position: " + i);
                return;
            }
        }

        if(newDist == playerPosition.DistanceFrom(q[q.Count-1].pos, this.metric)){
            q.Add(new ChunkDistance(x, distance));
            this.chunks.Add(x);

            if(DEBUG_QUEUE)
                Debug.Log("added: " + x);
        }
        else{
            q.Insert(0, new ChunkDistance(x, distance));
            this.chunks.Add(x);

            if(DEBUG_QUEUE)
                Debug.Log("inserted in the beginning: " + x);
        }
    }

    public bool Contains(ChunkPos x){
        return this.chunks.Contains(x);
    }

    public int GetSize(){
        return this.queue.Count + this.initialQueue.Count;
    }

    public ChunkPos Peek(){
        if(GetSize() > 0){
            if(this.initialQueue.Count > 0)
                return this.initialQueue[0].pos;
            else{
                if(DEBUG_QUEUE)
                    Debug.Log("Peeking: " + this.queue[0]);

                return this.queue[0].pos;
            }
        }

        return playerPosition;
    }

    public float PeekDistance(){
        if(GetSize() > 0){
            if(this.initialQueue.Count > 0)
                return this.initialQueue[0].distance;
            else{
                if(DEBUG_QUEUE)
                    Debug.Log("Peeking: " + this.queue[0]);

                return this.queue[0].distance;
            }
        }

        return -1;
    } 

    public ChunkPos Pop(){
        if(this.initialQueue.Count > 0){
            ChunkPos aux = this.initialQueue[0].pos;
            this.initialQueue.RemoveAt(0);
            this.chunks.Remove(aux);
            return aux;
        }
        else{
            ChunkPos aux = this.queue[0].pos;

            if(this.DEBUG_QUEUE)
                Debug.Log("popping: " + aux);

            this.queue.RemoveAt(0);
            this.chunks.Remove(aux);
            return aux;            
        }

    }

    public void Remove(ChunkPos x){
        for(int i=0; i < this.initialQueue.Count; i++){
            if(this.initialQueue[i].pos == x){
                this.initialQueue.RemoveAt(i);
                this.chunks.Remove(x);
                return;
            }
        }

        for(int i=0; i < this.queue.Count; i++){
            if(this.queue[i].pos == x){
                this.queue.RemoveAt(i);
                this.chunks.Remove(x);
                return;
            }
        }        
    }

    public void Clear(){
        this.queue.Clear();
        this.chunks.Clear();
    }

    public void SetPlayerPosition(ChunkPos pos){
        this.playerPosition = pos;

        RenewDistances();
    }

    private void RenewDistances(){
        ChangeReference(ref this.queue, ref this.backupQueue);

        this.chunks.Clear();

        for(int i=0; i < this.backupQueue.Count; i++){
            Add(this.backupQueue[i].pos);
        }

        this.backupQueue.Clear();
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
    public float distance;

    public ChunkDistance(ChunkPos pos, float distance){
        this.pos = pos;
        this.distance = distance;
    }

    public override string ToString(){
        return this.pos.ToString() + " -> " + this.distance;
    }
}