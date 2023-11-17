using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace com.tiledmedia.clearvr.demos {
    [CustomEditor(typeof(InputExtensions))]
    public class InputExtensionsEditor : Editor {
        public override void OnInspectorGUI() {
            // Show default inspector property editor
            DrawDefaultInspector();
        }

        
    }
}