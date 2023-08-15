using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Handles DataHoles and makes sure there's little fragmentation to disk
*/
public class FragmentationHandler{
    private List<DataHole> data;
    public byte[] cachedHoles = new byte[384]; // 32 Holes per Read

    public FragmentationHandler(bool loaded){
        this.data = new List<DataHole>(){};

        if(!loaded)
            this.data.Add(new DataHole(0, -1, infinite:true));
    }

    // Finds a position in RegionFile that fits
    // a chunk with given size
    public long FindPosition(int size){
        long output;

        for(int i=0; i < this.data.Count; i++){
            if(data[i].size > size){
                output = data[i].position;
                data.Insert(i+1, new DataHole(data[i].position + size, (int)data[i].size - size));
                data.RemoveAt(i);
                return output;
            }
            else if(data[i].size == size){
                output = data[i].position;
                data.RemoveAt(i);
                return output;              
            }
        }

        output = data[data.Count-1].position;
        data.Add(new DataHole(data[data.Count-1].position + size, -1, infinite:true));
        data.RemoveAt(data.Count-2);
        return output;
    }

    // Puts hole data in CachedHoles
    // Returns the amount of bytes written and a reference bool that serves as a flag
    // When the flag is true, caching has been completed. If false, more CacheHoles need to be called
    // Offset is a multiplier of 384 indices
    public int CacheHoles(int offset, ref bool done){
        done = this.data.Count - offset*32 <= 32;
        int index=0;

        if(done){
            for(int i=offset*32; i < this.data.Count; i++){
                data[i].Bytefy(this.cachedHoles, index);
                index += 12;
            }
        }
        else{
            for(int i=offset*32; i < (offset+1)*32; i++){
                data[i].Bytefy(this.cachedHoles, index);
                index += 12;
            }           
        }
        return index;
    }

    // Adds a DataHole to list in a priority list fashion
    public void AddHole(long pos, int size, bool infinite=false){
        if(infinite){
            this.data.Add(new DataHole(pos, -1, infinite:true));
            return;
        }

        for(int i=0; i<this.data.Count;i++){
            if(this.data[i].position > pos){
                this.data.Insert(i, new DataHole(pos, size));
                MergeHoles(i);
                return;
            }
        }

        // Adds a hole if there isn't any
        this.data.Add(new DataHole(pos, size));
        return;
    }

    // Removes if hole has no size
    private bool RemoveZero(DataHole dh){
        if(dh.size == 0){
            this.data.Remove(dh);
            return true;
        }
        return false;
    }

    public int Count(){
        return this.data.Count;
    }

    // Checks if the current Fragmentation Handler only has the infite hole entry
    public bool IsDefragged(){return this.data.Count == 1 && this.data[0].infinite;}

    // Merges DataHoles starting from pos in data list if there's any
    // ONLY USE WHEN JUST ADDED A HOLE IN POS
    private void MergeHoles(int index){
        if(this.data[index].position + this.data[index].size == this.data[index+1].position){
            
            // If neighbor hole is infinite
            if(this.data[index+1].infinite){
                this.data.RemoveAt(index+1);
                this.data[index] = new DataHole(this.data[index].position, -1, infinite:true);
            }
            // If neighbor is a normal hole
            else{
                this.data[index] = new DataHole(this.data[index].position, this.data[index].size + this.data[index+1].size);
                this.data.RemoveAt(index+1);
            }
        }
    }

}

// The individual data spots that can either be dead data or free unused data
public struct DataHole{
    public long position;
    public bool infinite;
    public int size;

    public DataHole(long pos, int size, bool infinite=false){
        this.position = pos;
        this.infinite = infinite;
        this.size = size;
    }

    // Turns DataHole into byte format for HLE files
    public void Bytefy(byte[] b, int offset){
        b[offset] = (byte)(this.position >> 56);
        b[offset+1] = (byte)(this.position >> 48);
        b[offset+2] = (byte)(this.position >> 40);
        b[offset+3] = (byte)(this.position >> 32);
        b[offset+4] = (byte)(this.position >> 24);
        b[offset+5] = (byte)(this.position >> 16);
        b[offset+6] = (byte)(this.position >> 8);
        b[offset+7] = (byte)this.position;
        b[offset+8] = (byte)(this.size >> 24);
        b[offset+9] = (byte)(this.size >> 16);
        b[offset+10] = (byte)(this.size >> 8);
        b[offset+11] = (byte)this.size;
    }
}