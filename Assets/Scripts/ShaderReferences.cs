using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderReferences : MonoBehaviour
{
    public ComputeShader shadowMapCalculator;
    private ComputeBuffer transparentBlockBuffer;
    private ComputeBuffer transparentObjBuffer;

    void Start(){
        SetTransparencyBuffer(BlockEncyclopediaECS.blockTransparent, BlockEncyclopediaECS.objectTransparent);
    }

    public ComputeShader GetShadowMapShader(){
        return this.shadowMapCalculator;
    }

    public void SetTransparencyBuffer(byte[] blockTransparency, byte[] objectTransparency){
        byte[] newBlockTransp = new byte[blockTransparency.Length + GetBufferExtensionSize(blockTransparency.Length%4)];
        byte[] newObjTransp = new byte[objectTransparency.Length + GetBufferExtensionSize(objectTransparency.Length%4)];

        blockTransparency.CopyTo(newBlockTransp, 0);
        objectTransparency.CopyTo(newObjTransp, 0);

        transparentBlockBuffer = new ComputeBuffer(newBlockTransp.Length/4, sizeof(int));
        transparentObjBuffer = new ComputeBuffer(newObjTransp.Length/4, sizeof(int));

        transparentBlockBuffer.SetData(newBlockTransp);
        transparentObjBuffer.SetData(newObjTransp);

        shadowMapCalculator.SetBuffer(0, "isTransparentBlock", transparentBlockBuffer);
        shadowMapCalculator.SetBuffer(0, "isTransparentObj", transparentObjBuffer);
    }

    private int GetBufferExtensionSize(int remainder){
        switch(remainder){
            case 0:
                return 0;
            case 1:
                return 3;
            case 2:
                return 2;
            case 3:
                return 1;
            default:
                return 0;
        }
    }

    void OnApplicationQuit(){
        transparentBlockBuffer.Dispose();
        transparentObjBuffer.Dispose();
    }
}
