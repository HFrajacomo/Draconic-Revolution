using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Priority Queue of ChunkPos ordered by their distance to a reference position (usually the player's chunk)
	Every distance has its own FIFO bucket, so Add, Pop and Remove are amortized O(1)
	Removal is lazy: the entry stays in its bucket and is discarded once it reaches the front
	SetPlayerPosition re-buckets every live entry in O(n), keeping their relative order
	There is no fixed maximum distance, buckets are created on demand
*/
public class ChunkPriorityQueue
{
    private List<Queue<ChunkDistance>> buckets = new List<Queue<ChunkDistance>>();
    private Dictionary<ChunkPos, int> members = new Dictionary<ChunkPos, int>(); // ChunkPos -> stamp of its live entry
    private List<ChunkDistance> rebuildCache = new List<ChunkDistance>();

    private int lowestBucket = 0;
    private int totalEntries = 0; // Live and stale entries
    private int stampCounter = 0;

    private ChunkPos playerPosition;
    private DistanceMetric metric;
    private int edgeLimit;

    private static readonly int MAXIMUM_BUCKETS = 8192;
    private static readonly int COMPACTION_SLACK = 1024;


    // edgeLimit is only used by DistanceMetric.EDGE_LIMITING
    public ChunkPriorityQueue(DistanceMetric metric=DistanceMetric.MANHATTAN, int edgeLimit=0){
        this.metric = metric;
        this.edgeLimit = edgeLimit;
    }

    public void Add(ChunkPos x){
        if(this.members.ContainsKey(x))
            return;

        this.stampCounter++;
        this.members.Add(x, this.stampCounter);
        Push(new ChunkDistance(x, GetDistance(x), this.stampCounter));
    }

    public bool Contains(ChunkPos x){
        return this.members.ContainsKey(x);
    }

    public int GetSize(){
        return this.members.Count;
    }

    public ChunkPos Peek(){
        ChunkDistance entry;

        if(PeekLive(out entry))
            return entry.pos;

        return this.playerPosition;
    }

    public float PeekDistance(){
        ChunkDistance entry;

        if(PeekLive(out entry))
            return entry.distance;

        return -1;
    }

    // Returns the player position if the queue is empty
    public ChunkPos Pop(){
        ChunkDistance entry;

        if(!PeekLive(out entry))
            return this.playerPosition;

        this.buckets[this.lowestBucket].Dequeue();
        this.totalEntries--;
        this.members.Remove(entry.pos);

        return entry.pos;
    }

    public bool TryPop(out ChunkPos pos){
        if(GetSize() == 0){
            pos = this.playerPosition;
            return false;
        }

        pos = Pop();
        return true;
    }

    public void Remove(ChunkPos x){
        if(!this.members.Remove(x))
            return;

        if(this.members.Count == 0){
            ClearBuckets();
            return;
        }

        // Prevents stale entries from piling up when many elements are removed without being popped
        if(this.totalEntries - this.members.Count > this.members.Count + COMPACTION_SLACK)
            Rebuild();
    }

    public void Clear(){
        this.members.Clear();
        ClearBuckets();
    }

    public void SetPlayerPosition(ChunkPos pos){
        this.playerPosition = pos;

        Rebuild();
    }

    public ChunkPos GetPlayerPosition(){
        return this.playerPosition;
    }

    private int GetDistance(ChunkPos x){
        int distance = this.playerPosition.DistanceFrom(x, this.metric, edgeLimit:this.edgeLimit);

        if(distance < 0)
            return 0;
        if(distance >= MAXIMUM_BUCKETS)
            return MAXIMUM_BUCKETS-1;

        return distance;
    }

    private bool IsLive(ChunkDistance entry){
        int stamp;

        if(!this.members.TryGetValue(entry.pos, out stamp))
            return false;

        return stamp == entry.stamp;
    }

    private void Push(ChunkDistance entry){
        while(this.buckets.Count <= entry.distance)
            this.buckets.Add(null);

        if(this.buckets[entry.distance] == null)
            this.buckets[entry.distance] = new Queue<ChunkDistance>();

        this.buckets[entry.distance].Enqueue(entry);
        this.totalEntries++;

        if(entry.distance < this.lowestBucket)
            this.lowestBucket = entry.distance;
    }

    // Finds the first live entry, discarding stale entries on the way
    private bool PeekLive(out ChunkDistance entry){
        Queue<ChunkDistance> bucket;

        while(this.members.Count > 0 && this.lowestBucket < this.buckets.Count){
            bucket = this.buckets[this.lowestBucket];

            if(bucket == null || bucket.Count == 0){
                this.lowestBucket++;
                continue;
            }

            entry = bucket.Peek();

            if(IsLive(entry))
                return true;

            bucket.Dequeue();
            this.totalEntries--;
        }

        if(this.members.Count == 0 && this.totalEntries > 0)
            ClearBuckets();

        entry = default(ChunkDistance);
        return false;
    }

    // Re-buckets all live entries in priority order using the current player position
    private void Rebuild(){
        Queue<ChunkDistance> bucket;
        ChunkDistance entry;

        this.rebuildCache.Clear();

        for(int i=this.lowestBucket; i < this.buckets.Count; i++){
            bucket = this.buckets[i];

            if(bucket == null)
                continue;

            while(bucket.Count > 0){
                entry = bucket.Dequeue();

                if(IsLive(entry))
                    this.rebuildCache.Add(entry);
            }
        }

        this.totalEntries = 0;
        this.lowestBucket = 0;

        for(int i=0; i < this.rebuildCache.Count; i++){
            entry = this.rebuildCache[i];
            Push(new ChunkDistance(entry.pos, GetDistance(entry.pos), entry.stamp));
        }

        this.rebuildCache.Clear();
    }

    private void ClearBuckets(){
        for(int i=0; i < this.buckets.Count; i++){
            if(this.buckets[i] != null)
                this.buckets[i].Clear();
        }

        this.totalEntries = 0;
        this.lowestBucket = 0;
    }

    public void Print(){
        string a = "[";

        for(int i=0; i < this.buckets.Count; i++){
            if(this.buckets[i] == null)
                continue;

            foreach(ChunkDistance entry in this.buckets[i]){
                if(IsLive(entry))
                    a += entry + ", ";
            }
        }

        a += "]";

        Debug.Log(a);
    }
}

public struct ChunkDistance{
    public ChunkPos pos;
    public int distance;
    public int stamp;

    public ChunkDistance(ChunkPos pos, int distance, int stamp){
        this.pos = pos;
        this.distance = distance;
        this.stamp = stamp;
    }

    public override string ToString(){
        return this.pos.ToString() + " -> " + this.distance;
    }
}
