using System;
using UnityEngine;
using TMPro;

namespace com.tiledmedia.clearvr.demos {
    public class ClearVRSubtitleListener : MonoBehaviour
    {

        [SerializeField]
        private TextMeshProUGUI _textMeshPro;

        public void SetText(String argText) {
            if (_textMeshPro) {
                _textMeshPro.text = argText;
            }
        }

    }
}
