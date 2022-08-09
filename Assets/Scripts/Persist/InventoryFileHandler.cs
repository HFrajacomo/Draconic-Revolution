using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;


/*
INVENTORY FILE (.invf) encapsulates all of users inventory

SCHEMA: [InventorySize (4)][Inventories (variable size)]*

*/
public class InventoryFileHandler{
    private Stream file;
    private Stream indexFile;
    private Stream holeFile;

    private string filePath;
    private string indexFilePath;
    private string holeFilePath;
    private string worldDir;
    private string worldsPath;

    private const string FILENAME = "inventory.invf";
    private const string INDEXNAME = "index.iind";
    private const string HOLENAME = "inventory.hle";
    private const int headerSize = 4;

    private byte[] buffer = new byte[10950]; // Buffer size that would cover a worst-case scenario inventory
    private byte[] indexBuffer = new byte[16000]; // Supports over 1000 players
    private byte[] intBuffer = new byte[4];
    private byte[] doubleLongBuffer = new byte[16];

    private FragmentationHandler fragmentationHandler;
    private Dictionary<ulong, ulong> index = new Dictionary<ulong, ulong>();


    public InventoryFileHandler(){
        #if UNITY_EDITOR
            this.worldsPath = "Worlds\\";
            this.worldDir = "Worlds\\" + World.worldName + "\\";
            this.filePath = this.worldDir + FILENAME;
            this.indexFilePath = this.worldDir + INDEXNAME;
            this.holeFilePath = this.worldDir + HOLENAME;
        #else
            // If is in Dedicated Server
            if(!World.isClient){
                this.worldsPath = "Worlds\\";
                this.worldDir = "Worlds\\" + World.worldName + "\\";
                this.filePath = this.worldDir + FILENAME;
                this.indexFilePath = this.worldDir + INDEXNAME;
                this.holeFilePath = this.worldDir + HOLENAME;
            }
            // If it's a Local Server
            else{
                this.worldsPath = EnvironmentVariablesCentral.clientExeDir + "\\Worlds\\";
                this.worldDir = this.worldsPath + World.worldName;
                this.filePath = this.worldDir + FILENAME;
                this.indexFilePath = this.worldDir + INDEXNAME;
                this.holeFilePath = this.worldDir + HOLENAME;
            }
        #endif
    
        LoadFiles();
        LoadIndex();

        if(this.holeFile.Length > 0){
            this.fragmentationHandler = new FragmentationHandler(true);
            LoadHoles();
        }
        else{
            this.fragmentationHandler = new FragmentationHandler(false);
        }
        
    }


    /*
    Saves the inventory of given playerId having given slots to (.invf) file
    */
    public void SaveInventory(ulong playerId, PlayerServerInventorySlot[] slots){
        int bytesWritten = 0;
        long filePosition;
        int inventorySize;

        for(int i=0; i < slots.Length; i++){
            bytesWritten += slots[i].SaveToBuffer(this.buffer, bytesWritten);
        }

        // If inventory is new to the file
        if(!this.index.ContainsKey(playerId)){
            filePosition = this.fragmentationHandler.FindPosition(bytesWritten+headerSize);
            this.index.Add(playerId, (ulong)filePosition);
            UnloadIndex();

            WriteInt(slots.Length);
            this.file.Write(this.intBuffer, 0, 4);
            this.file.Write(this.buffer, 0, bytesWritten);
            this.file.Flush();
        }
        // If inventory was already saved
        else{
            filePosition = (long)this.index[playerId];
            ReadHeader(filePosition);
            inventorySize = ReadInt(this.intBuffer, 0);

            this.fragmentationHandler.AddHole(filePosition, inventorySize+headerSize);
            filePosition = this.fragmentationHandler.FindPosition(bytesWritten);
            SaveHoles();

            // Changes index
            if(filePosition != (long)this.index[playerId]){
                this.index[playerId] = (ulong)filePosition;
                UnloadIndex();
            }

            // Saves inventory
            WriteInt(slots.Length);
            this.file.Write(this.intBuffer, 0, 4);
            this.file.Write(this.buffer, (int)filePosition, (int)(filePosition+bytesWritten));
            this.file.Flush();
        }
    }

    
    /*
    Loads the contents of given player inventory from (.invf) file.
    */
    public PlayerServerInventorySlot[] LoadInventory(ulong playerId){
        int readSize;
        int refVoid = 0;
        int filePosition = (int)this.index[playerId];
        ReadHeader(filePosition);
        readSize = ReadInt(intBuffer, 0);

        this.file.Read(this.buffer, filePosition+4, filePosition+4+readSize);

        return PlayerServerInventorySlot.BuildInventory(buffer, 0, PlayerServerInventory.playerInventorySize, ref refVoid);
    }
    

    // Save hole data to the HLE file
    public void SaveHoles(){
        bool done = false;
        int offset = 0;
        int writtenBytes = 0;

        this.holeFile.SetLength(0);
        writtenBytes = this.fragmentationHandler.CacheHoles(offset, ref done);
        this.holeFile.Write(this.fragmentationHandler.cachedHoles, 0, writtenBytes);
        this.holeFile.Flush();

        while(!done){
            offset++;
            writtenBytes = this.fragmentationHandler.CacheHoles(offset, ref done);
            this.holeFile.Write(this.fragmentationHandler.cachedHoles, 0, writtenBytes);
            this.holeFile.Flush();      
        }
    }

    // Loads all DataHole data to Fragment Handlers list
    public void LoadHoles(){
        this.holeFile.Seek(0, SeekOrigin.Begin);

        byte[] holeBuffer = new byte[this.holeFile.Length];

        this.holeFile.Read(holeBuffer, 0, (int)holeFile.Length);
        AddHolesFromBuffer(holeBuffer, (int)holeFile.Length);
    }

    public void AddHole(long pos, int size, bool infinite=false){
        this.fragmentationHandler.AddHole(pos, size, infinite:infinite);
    }

    // Writes all index data to index file
    public void UnloadIndex(){
        int position = 0;

        foreach(long l in this.index.Keys){
            ReadLong(l, position);
            position += 8;
            ReadLong((long)this.index[(ulong)l], position);
            position += 8;
        }

        this.indexFile.SetLength(0);
        this.indexFile.Write(this.indexBuffer, 0, position);
        this.indexFile.Flush();
    }

    // Closes all streams and saves index and holes
    public void Close(){
        UnloadIndex();
        SaveHoles();

        this.file.Close();
        this.indexFile.Close();
        this.holeFile.Close();
    }

    // Checks if a playerId exists in index
    public bool IsIndexed(ulong playerId){
        return this.index.ContainsKey(playerId);
    }

    // Reads header information and adds it to intBuffer
    private void ReadHeader(long filePosition){
        this.file.Read(this.intBuffer, (int)filePosition, (int)filePosition+4);
    }

    // Loads the (.iind) file to the actual index in RAM
    private void LoadIndex(){
        ulong a, b;

        this.indexFile.Seek(0, SeekOrigin.Begin);
        byte[] indexBuffer = new byte[this.indexFile.Length];
        this.indexFile.Read(indexBuffer, 0, (int)this.indexFile.Length);
        
        for(int i=0; i < this.indexFile.Length/8; i+=2){
            a = ReadUlong(indexBuffer, i*8);
            b = ReadUlong(indexBuffer, (i+1)*8);

            Debug.Log("inserted key: " + a);
            this.index.Add(a, b);
        }
    }

    private ulong ReadUlong(byte[] buff, int pos){
        ulong a;

        a = buff[pos];
        a = a << 8;
        a += buff[pos+1];
        a = a << 8;
        a += buff[pos+2];
        a = a << 8;
        a += buff[pos+3];
        a = a << 8;
        a += buff[pos+4];
        a = a << 8;
        a += buff[pos+5];
        a = a << 8;
        a += buff[pos+6];
        a = a << 8;
        a += buff[pos+7];

        return a;        
    }

    private void ReadLong(long l, int position){
        this.indexBuffer[position] = (byte)(l >> 56);
        this.indexBuffer[position+1] = (byte)(l >> 48);
        this.indexBuffer[position+2] = (byte)(l >> 40);
        this.indexBuffer[position+3] = (byte)(l >> 32);
        this.indexBuffer[position+4] = (byte)(l >> 24);
        this.indexBuffer[position+5] = (byte)(l >> 16);
        this.indexBuffer[position+6] = (byte)(l >> 8);
        this.indexBuffer[position+7] = (byte)l;       
    }

    private void LoadFiles(){
        if(!Directory.Exists(this.worldsPath))
            Directory.CreateDirectory(this.worldsPath);
        if(!Directory.Exists(this.worldDir))
            Directory.CreateDirectory(this.worldDir);

        if(File.Exists(this.filePath))
            this.file = File.Open(this.filePath, FileMode.Open);
        else
            this.file = File.Open(this.filePath, FileMode.Create);

        if(File.Exists(this.indexFilePath))
            this.indexFile = File.Open(this.indexFilePath, FileMode.Open);
        else
            this.indexFile = File.Open(this.indexFilePath, FileMode.Create);

        if(File.Exists(this.holeFilePath))
            this.holeFile = File.Open(this.holeFilePath, FileMode.Open);
        else
            this.holeFile = File.Open(this.holeFilePath, FileMode.Create);
    }


    // Adds holes read from buffer data
    private void AddHolesFromBuffer(byte[] holeBuffer, int readBytes){
        long a;
        int b;

        for(int i=0; i < readBytes; i+= 12){
            a = ReadLongHole(holeBuffer, i);
            b = ReadIntHole(holeBuffer, i+8);

            if(b > 0){
                AddHole(a, b);
            }
            else{
                AddHole(a, -1, infinite:true);
            }
        }
    }

    // Reads a long in byte[] cachedHoles at position n
    private long ReadLongHole(byte[] holeBuffer, int pos){
        long a;

        a = holeBuffer[pos];
        a = a << 8;
        a += holeBuffer[pos+1];
        a = a << 8;
        a += holeBuffer[pos+2];
        a = a << 8;
        a += holeBuffer[pos+3];
        a = a << 8;
        a += holeBuffer[pos+4];
        a = a << 8;
        a += holeBuffer[pos+5];
        a = a << 8;
        a += holeBuffer[pos+6];
        a = a << 8;
        a += holeBuffer[pos+7];

        return a;
    }

    // Reads an int in byte[] cachedHoles at position n
    private int ReadIntHole(byte[] holeBuffer, int pos){
        int a;

        a = holeBuffer[pos];
        a = a << 8;
        a += holeBuffer[pos+1];
        a = a << 8;
        a += holeBuffer[pos+2];
        a = a << 8;
        a += holeBuffer[pos+3];

        return a;
    }

    // Reads an int in byte[] cachedIndex at position n
    private int ReadInt(byte[] buff, int pos){
        int a;

        a = buff[pos];
        a = a << 8;
        a += buff[pos+1];
        a = a << 8;
        a += buff[pos+2];
        a = a << 8;
        a += buff[pos+3];

        return a;
    }

    // Writes an int to the intArray
    private void WriteInt(int a){
        this.intBuffer[0] = (byte)(a >> 24);
        this.intBuffer[1] = (byte)(a >> 16);
        this.intBuffer[2] = (byte)(a >> 8);
        this.intBuffer[3] = (byte)a;
    }
}