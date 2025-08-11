using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

[CustomEditor(typeof(DataClient))]
public class DataMEditor   : Editor
{
    private string pasteText = "";
    private int selectedListIndex = 0;
    private string[] listOptions;
    private FieldInfo[] listFields;

    private void OnEnable()
    {
        var type = typeof(DataClient);
        listFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(List<>))
            .ToArray();
        listOptions = listFields.Select(f => f.Name).ToArray();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("구글 시트 데이터 붙여넣기 (헤더 포함)", EditorStyles.boldLabel);
        pasteText = EditorGUILayout.TextArea(pasteText, GUILayout.MinHeight(60));
        if (listOptions != null && listOptions.Length > 0)
        {
            selectedListIndex = EditorGUILayout.Popup("적용할 리스트", selectedListIndex, listOptions);
            if (GUILayout.Button("파싱 및 적용"))
            {
                ParseAndApplyDataUniversal();
            }
        }
    }

    private void ParseAndApplyDataUniversal()
    {
        if (string.IsNullOrWhiteSpace(pasteText)) return;
        var lines = pasteText.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToArray();
        if (lines.Length < 2) return;

        var header = Regex.Split(lines[0], "\t");
        var dataLines = lines.Skip(1);

        var so = new SerializedObject(target);
        var listProp = so.FindProperty(listOptions[selectedListIndex]);
        listProp.ClearArray();

        foreach (var line in dataLines)
        {
            var cols = Regex.Split(line, "\t");
            if (cols.Length < header.Length) continue;

            int newIndex = listProp.arraySize;
            listProp.InsertArrayElementAtIndex(newIndex);
            var elemProp = listProp.GetArrayElementAtIndex(newIndex);

            for (int i = 0; i < header.Length; i++)
            {
                var fieldName = header[i].Trim();
                var fieldProp = elemProp.FindPropertyRelative(fieldName);
                if (fieldProp == null) continue;

                string strVal = cols[i];
                switch (fieldProp.propertyType)
                {
                    case SerializedPropertyType.String:
                        fieldProp.stringValue = strVal;
                        break;
                    case SerializedPropertyType.Float:
                        if (float.TryParse(strVal, out var f)) fieldProp.floatValue = f;
                        break;
                    case SerializedPropertyType.Integer:
                        if (int.TryParse(strVal, out var n)) fieldProp.intValue = n;
                        break;
                    case SerializedPropertyType.Generic: // ObscuredTypes fall here
                        var valueProp = fieldProp.FindPropertyRelative("hiddenValue"); // Obscured* types use this
                        if (valueProp != null)
                        {
                            // We can't directly write to hiddenValue as it's encrypted.
                            // Instead, we assign to the parent property which triggers the implicit operator.
                            // For the editor, we can try setting the parent's value directly.
                            // But the most reliable way is to set the underlying value if possible,
                            // or just use reflection on the real object.
                            // Let's stick to what works with SerializedProperty.
                            // The issue is that Obscured types have custom drawers.
                            // A simpler way that works with SerializedProperty is to find the property the drawer uses.
                            // It seems direct assignment to SerializedProperty for Obscured types is tricky.
                            // Let's revert to setting the real object and then marking it dirty.
                            // The previous reflection approach failed, let's try to fix it.
                            // The issue might be that `field.SetValue` on a struct copy doesn't work.
                            // Let's re-do this part with a hybrid approach.
                            break; // Fallback to object modification
                        }
                        break;
                }
            }
        }
        so.ApplyModifiedProperties();

        // Since SerializedProperty has trouble with ObscuredTypes, let's use reflection again but more carefully.
        var dataM = (DataClient)target;
        var listField = listFields[selectedListIndex];
        var elemType = listField.FieldType.GetGenericArguments()[0];
        var newList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elemType));

        foreach (var line in dataLines)
        {
            var cols = Regex.Split(line, "\t");
            if (cols.Length < header.Length) continue;
            var elem = Activator.CreateInstance(elemType);
            for (int i = 0; i < header.Length; i++)
            {
                var field = elemType.GetField(header[i].Trim(), BindingFlags.Instance | BindingFlags.Public);
                if (field == null) continue;
                
                string strVal = cols[i];
                try
                {
                    var converter = System.ComponentModel.TypeDescriptor.GetConverter(field.FieldType);
                    if (converter != null && converter.CanConvertFrom(typeof(string)))
                    {
                        var value = converter.ConvertFromString(strVal);
                        field.SetValue(elem, value);
                    }
                    else // Fallback for Obscured types
                    {
                         if (field.FieldType == typeof(CodeStage.AntiCheat.ObscuredTypes.ObscuredString))
                            field.SetValue(elem, (CodeStage.AntiCheat.ObscuredTypes.ObscuredString)strVal);
                        else if (field.FieldType == typeof(CodeStage.AntiCheat.ObscuredTypes.ObscuredFloat) && float.TryParse(strVal, out var f))
                            field.SetValue(elem, (CodeStage.AntiCheat.ObscuredTypes.ObscuredFloat)f);
                        else if (field.FieldType == typeof(CodeStage.AntiCheat.ObscuredTypes.ObscuredInt) && int.TryParse(strVal, out var n))
                            field.SetValue(elem, (CodeStage.AntiCheat.ObscuredTypes.ObscuredInt)n);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Could not parse or set value for field {field.Name} from value '{strVal}'. Error: {ex.Message}");
                }
            }
            newList.Add(elem);
        }
        listField.SetValue(dataM, newList);
        EditorUtility.SetDirty(dataM);
        AssetDatabase.SaveAssets();
    }
}
