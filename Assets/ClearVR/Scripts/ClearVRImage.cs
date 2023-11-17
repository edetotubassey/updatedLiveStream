using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System;
namespace com.tiledmedia.clearvr {
    class ClearVRImage: Image {
        
        public override Material GetModifiedMaterial(Material baseMaterial) {
            
            Material cModifiedMat = base.GetModifiedMaterial(baseMaterial);

            ClearVRDisplayObjectControllerSprite cvrdocs = this.GetComponent<ClearVRDisplayObjectControllerSprite>();
            if(cvrdocs != null) {
                cvrdocs.UpdateMaterial(cModifiedMat);
                cvrdocs.UpdateShaderAndMaySetDirty(null, false);
            } // else: ClearVRImage was attached to a GameObject without DOCSprite (which is legal).

            return cModifiedMat;
        }

   
    };
}