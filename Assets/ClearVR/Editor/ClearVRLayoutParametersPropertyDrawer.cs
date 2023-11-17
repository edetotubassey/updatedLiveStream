using UnityEditor;
using UnityEngine;
using System;
namespace com.tiledmedia.clearvr {
    [CustomPropertyDrawer(typeof(LayoutParameters))]
    public class ClearVRLayoutParametersPropertyDrawer : PropertyDrawer {
        private readonly float ySpacing = EditorGUIUtility.singleLineHeight;
        private const int lineCountNotExpanded = 2;
        private const int lineCountExpanded = lineCountNotExpanded + 7;
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float height = 0;
            if (!property.isExpanded) {
                height = (ySpacing * lineCountNotExpanded) ;
            } else {
                height = (ySpacing * lineCountExpanded) + GetDisplayObjectMappingsTotalHeight(property, label);
            }
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            if (!property.isExpanded) {
                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, ySpacing), property, GUIContent.none);
                // EditorGUI.PropertyField(position, property, GUIContent.none);
            } else {
                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, ySpacing), property, GUIContent.none);
            }
            EditorGUI.EndProperty();
            Rect topLabel = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Layout"));
            float yOffset = ySpacing;
            SerializedProperty displayObjectMappings = property.FindPropertyRelative("_displayObjectMappings");
            string layoutParametersFeedback = "Layout OK.";
            GUIStyle layoutParametersFeedbackStyle = ClearVREditorUtils.infoStyle;
            if (property.isExpanded) {
                Rect uniqueNamePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y + yOffset, position.width, ySpacing), new GUIContent("Unique name"));
                EditorGUI.PropertyField(uniqueNamePosition, property.FindPropertyRelative("_name"), GUIContent.none);
                if(String.IsNullOrEmpty(property.FindPropertyRelative("_name").stringValue)) {
                    layoutParametersFeedback = "No name specified";
                    layoutParametersFeedbackStyle = ClearVREditorUtils.errorStyle;
                }
                yOffset+=ySpacing;

                Rect audioFeedIndexPosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y + yOffset, position.width, ySpacing), new GUIContent("Audio feed index"));
                EditorGUI.PropertyField(audioFeedIndexPosition, property.FindPropertyRelative("_audioFeedIndex"), GUIContent.none);
                yOffset+=ySpacing;

                Rect audioTrackIndexPosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y + yOffset, position.width, ySpacing), new GUIContent("Audio track index"));
                EditorGUI.PropertyField(audioTrackIndexPosition, property.FindPropertyRelative("_audioTrackIndex"), GUIContent.none);
                yOffset+=ySpacing;

                Rect audioPreferredLanguageIndexPosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y + yOffset, position.width, ySpacing), new GUIContent("Audio preferred language"));
                EditorGUI.PropertyField(audioPreferredLanguageIndexPosition, property.FindPropertyRelative("_preferredAudioLanguage"), GUIContent.none);
                yOffset+=ySpacing;

                Rect subtitlesFeedIndexPosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y + yOffset, position.width, ySpacing), new GUIContent("Subtitles feed index"));
                EditorGUI.PropertyField(subtitlesFeedIndexPosition, property.FindPropertyRelative("_subtitleFeedIndex"), GUIContent.none);
                yOffset+=ySpacing;

                Rect subtitlesTrackIndexPosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y + yOffset, position.width, ySpacing), new GUIContent("Subtitles track index"));
                EditorGUI.PropertyField(subtitlesTrackIndexPosition, property.FindPropertyRelative("_subtitleTrackIndex"), GUIContent.none);
                yOffset+=ySpacing;

                Rect subtitlesPreferredLanguageIndexPosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y + yOffset, position.width, ySpacing), new GUIContent("Subtitles preferred language"));
                EditorGUI.PropertyField(subtitlesPreferredLanguageIndexPosition, property.FindPropertyRelative("_preferredSubtitlesLanguage"), GUIContent.none);
                yOffset+=ySpacing;

                // Remember that it is impossible to access class fields of derived classes using the FindProperty() API.
                // Therefore, we opt for a mechanism where each derived class has a specifically named variable with a unique value to distinguish between derived types.
                for (int i = 0; i < displayObjectMappings.arraySize; i++) {
                    SerializedProperty displayObjectControllerProperty = displayObjectMappings.GetArrayElementAtIndex(i).FindPropertyRelative("_clearVRDisplayObjectController");
                    if(displayObjectControllerProperty != null) {
                        if(displayObjectControllerProperty.objectReferenceValue == null) {
                            continue;
                        }
                        SerializedObject serializedObject = new SerializedObject(displayObjectControllerProperty.objectReferenceValue);
                        MeshTextureModes meshTextureMode = MeshTextureModes.Unknown;
                        for(int j = 0; j < 4; j++) {
                            String name = "_editorGUI_ID_" + j.ToString("D2");
                            SerializedProperty meshTextureModePropertyMaybe = serializedObject.FindProperty(name);
                            if(meshTextureModePropertyMaybe != null) {
                                meshTextureMode = (MeshTextureModes) meshTextureModePropertyMaybe.intValue;
                            }
                        }
                        if(meshTextureMode == MeshTextureModes.OVROverlay && displayObjectMappings.arraySize > 1) {
                            layoutParametersFeedback = "You can have only one Display Object Mapping per Layout when using an OVROVerlay.";
                            layoutParametersFeedbackStyle = ClearVREditorUtils.errorStyle;
                            break;
                        }
                    }
                }
                EditorGUI.LabelField(new Rect(position.x, position.y + yOffset, position.width, ySpacing), layoutParametersFeedback, layoutParametersFeedbackStyle);
                yOffset+=ySpacing;
                // The DOM list is shifted 10 units to the left.
                const float domRightOffset = 10;
                EditorGUI.PropertyField(new Rect(position.x + domRightOffset, position.y + yOffset, position.width - domRightOffset, EditorGUI.GetPropertyHeight(displayObjectMappings)), displayObjectMappings, new GUIContent("Display Object Mappings"), true);
            }
            GUI.enabled = true;
        }

        public float GetDisplayObjectMappingsTotalHeight(SerializedProperty property, GUIContent label) {
            float height = ySpacing * 2; // double spacing so that the +/- button doesnt get confused with the display object mappings.
            SerializedProperty displayObjectMappings = property.FindPropertyRelative("_displayObjectMappings");
            if(displayObjectMappings.isExpanded) {
                if(displayObjectMappings.arraySize == 0) {
                    // If the list has zero elements, an extra line is added that says: "No items"
                    return height += ySpacing * 2;
                }
                // The extra two originates from additional padding that is added. Note that this 2 is NOT added to ClearVRDisplayObjectMappingPropertyDrawer.GetPropertyHeight()
                height += 40 /* spacing for the +/- list button */ + 2;
                for (int i = 0; i < displayObjectMappings.arraySize; i++) {
                    SerializedProperty displayObjectMapping = displayObjectMappings.GetArrayElementAtIndex(i);
                    height += EditorGUI.GetPropertyHeight(displayObjectMapping) + 2;
                }
            }
            return height;
        }
    }
}
