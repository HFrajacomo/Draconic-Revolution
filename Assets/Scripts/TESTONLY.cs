using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class TESTONLY : MonoBehaviour
{   
    public ComputeShader shader;
    public ComputeBuffer voxelDataBuffer;
    public ComputeBuffer heightMapBuffer;
    public ComputeBuffer transparentBlockBuffer;
    public ComputeBuffer transparentObjBuffer;
    public ComputeBuffer shadowMapBuffer;

    public ComputeBuffer debugBuffer;

    private VoxelData vd;
    private ushort[] data = new ushort[8]{1, 1, 0, 1, 0, 0, 0, 1}; // {1, 1, 0, 1, 0, 0, 0, 1}
    private byte[] heightMap = new byte[]{0, 1, 0, 1};
    private byte[] isTransparentBlock = {1, 0, 0, 0};
    private byte[] isTransparentObj = {1, 1, 1, 1};
    private byte[] shadowMap;

    private int[] debug = new int[4];

    void Start(){
        this.shadowMap = new byte[this.data.Length];
        vd = new VoxelData(this.data);
        vd.CalculateShadowMap_BURST();

        vd.PrintHeight();
        vd.PrintShadow();
    }

    private void RunShader(int x, int y, int z){
        voxelDataBuffer = new ComputeBuffer(data.Length/2, sizeof(int));
        heightMapBuffer = new ComputeBuffer(heightMap.Length/4, sizeof(int));
        transparentBlockBuffer = new ComputeBuffer(isTransparentBlock.Length/4, sizeof(int));
        transparentObjBuffer = new ComputeBuffer(isTransparentObj.Length/4, sizeof(int));
        shadowMapBuffer = new ComputeBuffer(data.Length/4, sizeof(int));
        debugBuffer = new ComputeBuffer(debug.Length, sizeof(int));

        voxelDataBuffer.SetData(data);
        heightMapBuffer.SetData(heightMap);
        transparentBlockBuffer.SetData(isTransparentBlock);
        transparentObjBuffer.SetData(isTransparentObj);
        shadowMapBuffer.SetData(shadowMap);
        debugBuffer.SetData(debug);

        shader.SetBuffer(0, "data", voxelDataBuffer);
        shader.SetBuffer(0, "heightMap", heightMapBuffer);
        shader.SetBuffer(0, "isTransparentBlock", transparentBlockBuffer);
        shader.SetBuffer(0, "isTransparentObj", transparentObjBuffer);
        shader.SetBuffer(0, "shadowMap", shadowMapBuffer);
        shader.SetBuffer(0, "debugBuffer", debugBuffer);

        shader.Dispatch(0, x, y, z);

        shadowMapBuffer.GetData(shadowMap);
        debugBuffer.GetData(debug);

        voxelDataBuffer.Dispose();
        heightMapBuffer.Dispose();
        transparentBlockBuffer.Dispose();
        transparentObjBuffer.Dispose();
        shadowMapBuffer.Dispose();
        debugBuffer.Dispose();

        ShowDebug(sizeof(int));
        ShowShadow(sizeof(byte));
    }

    private void ShowDebug(int bytesize){
        string output = "Debug: ";
        int size = 0;

        foreach(int i in debug){
            output += " " + i.ToString();
            size++;

            if(size%(sizeof(int)/bytesize) == 0)
                output += " | ";
        }

        print(output);
    }

    private void ShowShadow(int bytesize){
        string output = "Shadow: ";
        int size = 0;

        foreach(ushort i in shadowMap){
            output += " " + i.ToString();
            size++;

            if(size%(sizeof(int)/bytesize) == 0)
                output += " | ";
        }

        print(output);
    }
}
