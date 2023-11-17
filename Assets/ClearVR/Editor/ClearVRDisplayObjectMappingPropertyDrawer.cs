using UnityEditor;
using UnityEngine;

namespace com.tiledmedia.clearvr {
    [CustomPropertyDrawer(typeof(DisplayObjectMapping))]
    public class ClearVRDisplayObjectMappingPropertyDrawer : PropertyDrawer {
        private ClearVRDisplayObjectControllerBase cachedDisplayObject = null;
        private readonly float ySpacing = EditorGUIUtility.singleLineHeight;
        private static readonly int lineCountExpanded = 6 - 1 /* disabled ContentFormat field for now, see also below */;
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float height = 0;
            if (!property.isExpanded) {
                height = ySpacing;
            } else {
                height = (ySpacing * lineCountExpanded);
            }
            return height;
        }

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            if (!property.isExpanded) {
                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, ySpacing), property, GUIContent.none);
                // EditorGUI.PropertyField(position, property, GUIContent.none);
            } else {
                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, ySpacing), property, GUIContent.none);
            }
            EditorGUI.EndProperty();
            EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Display Object Mapping"));
            float yOffset = ySpacing;

            if (property.isExpanded) {
                // Caching the properties for optimization
                SerializedProperty clearVRDisplayObjectController = property.FindPropertyRelative("_clearVRDisplayObjectController");

                Rect feedIndexPosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y + yOffset, position.width, ySpacing), new GUIContent("Feed index"));
                EditorGUI.PropertyField(feedIndexPosition, property.FindPropertyRelative("_feedIndex"), GUIContent.none);
                yOffset+=ySpacing;

                Rect displayObjectContentPosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y + yOffset, position.width, ySpacing), new GUIContent("Display"));
                EditorGUI.PropertyField(displayObjectContentPosition, clearVRDisplayObjectController, GUIContent.none);
                yOffset+=ySpacing;

                if (clearVRDisplayObjectController.objectReferenceValue == null) {
                    EditorGUI.LabelField(new Rect(position.max.x - (position.width), position.y + yOffset, position.width, ySpacing), "Display Object is not assigned and will not be mapped!", ClearVREditorUtils.warningStyle);
                } else {
                    if (GUI.Button(new Rect(position.max.x - (position.width / 5), position.y + yOffset, position.width / 5, ySpacing), "Select")) {
                        Selection.activeGameObject = GetGameObject(clearVRDisplayObjectController.objectReferenceValue as ClearVRDisplayObjectControllerBase);
                    }
                }
                yOffset+=ySpacing;

                Rect displayObjectClassTypePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y + yOffset, position.width, ySpacing), new GUIContent("Display object type "));
                EditorGUI.PropertyField(displayObjectClassTypePosition, property.FindPropertyRelative("_displayObjectClassType"), GUIContent.none);
                yOffset+=ySpacing;
                
                // Temporarily disabled, do not forget to update `lineCountExpanded` when we uncomment.
                // Rect contentFormatContentPosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y + yOffset, position.width, ySpacing), new GUIContent("Content format"));
                // EditorGUI.PropertyField(contentFormatContentPosition, property.FindPropertyRelative("_contentFormat"), GUIContent.none);
                // yOffset+=ySpacing;
            }
        }

        public GameObject GetGameObject(ClearVRDisplayObjectControllerBase clearVRDisplayObjectController) {
            if (clearVRDisplayObjectController != null) {
                return clearVRDisplayObjectController.gameObject;
            } else {
                throw new System.Exception("No GameObject associated to this DisplayObject. Are you using a prefab?");
            }
        }
    }
}