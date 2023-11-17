#pragma warning disable 0168
#pragma warning disable 0162
#pragma warning disable 0219
#if UNITY_EDITOR
    using System;
    using System.IO;
    using System.Text;
    using System.Xml;
    using UnityEditor;
    using UnityEngine;
    namespace com.tiledmedia.clearvr {
        public class XMLHelpers {
            public static void UpdateAndroidManifestXml(string argFileFullPath, string argNodePath, string argKey, string argNewValue) {
                UpdateAndroidManifestXml(argFileFullPath, argNodePath, argKey, argNewValue, false);
            }

            public static void UpdateAndroidManifestXml(string argFileFullPath, string argNodePath, string argKey, string argNewValue, bool argCreateBackup) {
                XmlDocument doc = new XmlDocument();
                XmlNode node;
                if(!File.Exists(argFileFullPath)) {
                    throw new Exception(String.Format("[ClearVR] Cannot update AndroidManifest.xml, file {0} not found.", argFileFullPath));
                }
                if(argCreateBackup) {
                    if(GetIsFileFullPathInAssetsFolder(argFileFullPath)) {
                        // File located in project (should always be the case)
                        String sourceFileInAssetFolder = GetAssetsFolderPathFromFileFullPath(argFileFullPath);
                        AssetDatabase.CopyAsset(sourceFileInAssetFolder, sourceFileInAssetFolder + ".bak");
                    } else {
                        File.Copy(argFileFullPath, argFileFullPath + ".bak");
                    }
                }
                doc.Load(argFileFullPath);
                try {
                    node = doc.SelectSingleNode(argNodePath);
                } catch (Exception e) {
                    throw new Exception(String.Format("[ClearVR] Unable to access expected XML node: {0} for key: {1} and new value: {2}. Error: {3}", argNodePath, argKey, argNewValue, e));
                }
                try {
                    if(node.Attributes[argKey].Value == argNewValue) {
                        // No need to update, it was already set to the correct value.
                        return;
                    }
                    node.Attributes[argKey].Value = argNewValue;
                } catch (Exception e) {
                    throw new Exception(String.Format("[ClearVR] Unable to set value on expected XML node: {0} for key: {1} and new value: {2}. Error: {3}", argNodePath, argKey, argNewValue, e));
                }
                // Make changes to the document.
                using(XmlTextWriter xtw = new XmlTextWriter(argFileFullPath, Encoding.UTF8)) {
                    xtw.Formatting = Formatting.Indented; // optional, if you want it to look nice
                    doc.WriteContentTo(xtw);
                }
                if(GetIsFileFullPathInAssetsFolder(argFileFullPath)) {
                    AssetDatabase.ImportAsset(GetAssetsFolderPathFromFileFullPath(argFileFullPath));
                }
            }


            public static String GetValueFromKeyInAndroidManifestXmlSafely(string argFileFullPath, string argNodePath, string argKey) {
                XmlDocument doc = new XmlDocument();
                XmlNode node;
                if(!File.Exists(argFileFullPath)) {
                    return null;
                }
                doc.Load(argFileFullPath);
                try {
                    node = doc.SelectSingleNode(argNodePath);
                } catch (Exception e) {
                    return null;
                }
                try {
                    return node.Attributes[argKey].Value;
                } catch {
                    //fallthrough
                }
                return null;
            }

            private static bool GetIsFileFullPathInAssetsFolder(String argFileFullPath) {
                return (argFileFullPath.Contains(Application.dataPath));   
            }

            private static String GetAssetsFolderPathFromFileFullPath(String argFileFullPath) {
                return argFileFullPath.Replace(Application.dataPath, "Assets");
            }
        }
    }
#endif
#pragma warning restore 0168
#pragma warning restore 0162
#pragma warning restore 0219
