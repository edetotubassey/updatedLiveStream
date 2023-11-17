// This Editor only script is located in the Scrips folder since it needs access to the internal ClearVRLayoutManager.layoutParametersList field
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace com.tiledmedia.clearvr {
    [CustomEditor(typeof(ClearVRLayoutManager))]
    public class ClearVRLayoutManagerEditor : Editor {
        // This is a duplicate of ClearVREditorUtils as that one resides in the Editor Assembly, whereas this one resides in the Runtime assembly.
        private static class ClearVREditorUtilsNonEditor {
            public static GUIStyle errorStyle = new GUIStyle() {
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                normal = new GUIStyleState() {
                textColor = Color.red
                }
            };
            public static GUIStyle warningStyle = new GUIStyle() {
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                normal = new GUIStyleState() {
                textColor = Color.yellow
                }
            };
            public static GUIStyle infoStyle = new GUIStyle() {
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                normal = new GUIStyleState() {
                textColor = Color.green,
                }
            };
        }

        private ClearVRLayoutManager clearVRLayoutManager;
        private static string currentlySelectedTemplate = "";
        // This values is reset every time the Inspector becomes active.
        int lastNumberOfLayoutParameters = -1;
        // private SerializedProperty clearVRLayoutManagerSerializedProperty;
        
        public void OnEnable() {
            clearVRLayoutManager = (ClearVRLayoutManager) target;
            // clearVRLayoutManagerSerializedProperty = serializedObject.FindProperty("layoutParametersList");
        }
        
        public override void OnInspectorGUI() {
            if(clearVRLayoutManager == null || clearVRLayoutManager.layoutParametersList == null) {
                return; // For Unity 2019 compatibility, ref. #6339
            }
            CheckIfLayoutParametersListSizeChanged();
            if (clearVRLayoutManager.layoutParametersList != null && clearVRLayoutManager.layoutParametersList.Count > 0) {
                List<String> layoutParametersNames = new List<String>();
                string feedback = "All LayoutParameter have a unique name.";
                GUIStyle feedbackStyle = ClearVREditorUtilsNonEditor.infoStyle;
                foreach (LayoutParameters layoutParameters in clearVRLayoutManager.layoutParametersList) {
                    String layoutName = layoutParameters.name;
                    if(!String.IsNullOrEmpty(layoutName)) {
                        if(layoutParametersNames.Contains(layoutName)) {
                            feedback = String.Format("Found LayoutParameters with duplicated names.", layoutName);
                            feedbackStyle = ClearVREditorUtilsNonEditor.errorStyle;
                        }
                    } else {
                        feedback = "Found LayoutParameters without name.";
                        feedbackStyle = ClearVREditorUtilsNonEditor.errorStyle;
                    }
                    layoutParametersNames.Add(layoutParameters.name);
                }
                GUILayout.Label(feedback, feedbackStyle);
                TemplateSelectionDropDown(); // Dropdown to hide or show the selected template
            }
            serializedObject.Update();
            DrawDefaultInspector(); // Draw the rest of the inspector
            // Using the EditorGUI.PropertyField() API renders the property as a reorderable list of which the children are not foldable with some of them stuck in folded state.
            // EditorGUI.PropertyField(clearVRLayoutManagerSerializedProperty, new GUIContent("Layout Parameters List "), true /* recursive */);
        }

        private void CheckIfLayoutParametersListSizeChanged() {
            int numberOfLayoutParameters = clearVRLayoutManager.layoutParametersList.Count;
            if(lastNumberOfLayoutParameters != -1) {
                if(numberOfLayoutParameters > lastNumberOfLayoutParameters && numberOfLayoutParameters > 0) {
                    // One was added.
                    // We rely on the fact that the "+" button only ever adds an entry at the end of the list. It never adds an entry in the middle.
                    // By explicitly calling the constructor we set all fields to the correct default values.
                    clearVRLayoutManager.layoutParametersList[numberOfLayoutParameters - 1] = new LayoutParameters();
                }
            } // else: the inspector just became active, no new entry could have been "added", so we ignore this.
            lastNumberOfLayoutParameters = numberOfLayoutParameters;
        }
        
        private void TemplateSelectionDropDown() {
            Rect templateSelectionDropDown = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Select Template");
            DrawDropdown(new Rect(templateSelectionDropDown.width / 2, templateSelectionDropDown.y, templateSelectionDropDown.width / 2, templateSelectionDropDown.height), new GUIContent(currentlySelectedTemplate, "Select the template to show in the scene."));
            EditorGUILayout.EndHorizontal();
        }

        public void DrawDropdown(Rect position, GUIContent label) {
            if (!EditorGUI.DropdownButton(position, label, FocusType.Passive)) {
                return;
            }

            void handleItemClicked(System.Object layoutName) {
                if (layoutName is string) {
                    string asString = layoutName as string;
                    switch (asString) {
                        case "All":
                            // Show all of the templates
                            ToggleAllDisplayObjectMappings(true);
                            break;
                        case "None":
                            // Hide all of the templates
                            ToggleAllDisplayObjectMappings(false);
                            break;
                        default:
                            ShowSelectedTemplateDisplayObjectMappingsAndHideOthers(asString);
                            break;
                    }
                    currentlySelectedTemplate = asString;
                }
            }

            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("All"), false, handleItemClicked, "All");
            menu.AddItem(new GUIContent("None"), false, handleItemClicked, "None");
            foreach (LayoutParameters layoutParameters in clearVRLayoutManager.layoutParametersList) {
                menu.AddItem(new GUIContent(layoutParameters.name), false, handleItemClicked, layoutParameters.name);
            }
            menu.DropDown(position);
        }

        public void ToggleDisplayObjectMappingsOfTemplate(LayoutParameters layoutParameters, bool shouldActivate) {
            List<GameObject> listOfGameObjects = new List<GameObject>();
            if(shouldActivate) {
                foreach (DisplayObjectMapping displayObjectMapping in layoutParameters.displayObjectMappings) {
                    if(displayObjectMapping.clearVRDisplayObjectController != null && displayObjectMapping.clearVRDisplayObjectController.gameObject != null) {
                        listOfGameObjects.Add(displayObjectMapping.clearVRDisplayObjectController.gameObject);
                    }
                }
            }
            Selection.objects = listOfGameObjects.ToArray();
        }

        public void ToggleAllDisplayObjectMappings(bool shouldActivate) {
            foreach (LayoutParameters layoutParameters in clearVRLayoutManager.layoutParametersList) {
                ToggleDisplayObjectMappingsOfTemplate(layoutParameters, shouldActivate);
            }
        }

        public void ShowSelectedTemplateDisplayObjectMappingsAndHideOthers(string layoutName) {
            ToggleAllDisplayObjectMappings(false);
            LayoutParameters layoutParameters = this.clearVRLayoutManager.layoutParametersList.Find(x => x.name == layoutName);
            if (layoutParameters != null) {
                ToggleDisplayObjectMappingsOfTemplate(layoutParameters, true);
            }
        }

        public int GetNumberOfDisplayObjectMappingsInAllLayouts() {
            int counter = 0;
            if (clearVRLayoutManager && clearVRLayoutManager.layoutParametersList.Count > 0) {
                foreach (LayoutParameters layoutParameters in clearVRLayoutManager.layoutParametersList) {
                    counter += layoutParameters.displayObjectMappings.Count;
                }
            }
            return counter;
        }
    }
}
#endif