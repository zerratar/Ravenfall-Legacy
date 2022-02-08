using NUnit.Framework;
using RavenNest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(ItemDropHandler))]
public class ItemDropEditor : Editor
{
    private List<Item> loadedItems;
    private string[] itemNames;
    private SerializedProperty items;
    private SerializedProperty dropList;
    private JsonBasedItemRepository itemManager;
    void OnEnable()
    {

        if (this.itemManager == null || JsonBasedItemRepository.Instance == null)
        {
            this.itemManager = new JsonBasedItemRepository();
        }

        if (this.itemManager == null) {             
            return;
        }


        this.loadedItems = itemManager.GetItems();
        if (loadedItems == null || loadedItems.Count == 0)
        {
            Debug.LogError("items.json repository file could not be found.");
            return;
        }
            

        itemNames = loadedItems.Select(x => x.Name).OrderBy(x => x).ToArray();

        if (serializedObject == null)
            return;

        items = serializedObject.FindProperty("items");
        dropList = serializedObject.FindProperty("dropList");
    }

    public override void OnInspectorGUI()
    {
        if (items == null || dropList == null || serializedObject == null || loadedItems == null)
            return;

        serializedObject.Update();

        if (dropList != null)
        {
            EditorGUILayout.PropertyField(dropList);
        }

        items.isExpanded = EditorGUILayout.Foldout(items.isExpanded, "Droppable Items");
        if (items.isExpanded)
        {
            items.arraySize = EditorGUILayout.IntField("Size", items.arraySize);



            List<PropVal> sorted = new List<PropVal>();
            for (int i = 0; i < items.arraySize; ++i)
            {
                SerializedProperty item = items.GetArrayElementAtIndex(i);
                var dropChanceProp = item.FindPropertyRelative("DropChance");
                var idProp = item.FindPropertyRelative("ItemID");
                var nameProp = item.FindPropertyRelative("ItemName");
                var nameStr = idProp.stringValue;
                if (Guid.TryParse(idProp.stringValue, out var id))
                {
                    var tari = loadedItems.FirstOrDefault(x => x.Id == id);
                    if (tari != null)
                    {
                        idProp.stringValue = tari.Id.ToString();
                        nameProp.stringValue = nameStr = tari.Name;
                    }
                }

                sorted.Add(new PropVal
                {
                    Value = dropChanceProp.floatValue,
                    ID = idProp.stringValue,
                    Name = idProp.stringValue,
                    Prop = item,
                    ElementIndex = i,
                });
            }

            //for (int i = 0; i < items.arraySize; ++i)
            foreach (var i in sorted.OrderBy(x => x.Name))
            {
                SerializedProperty item = i.Prop;// items.GetArrayElementAtIndex(i);
                                                 //SerializedProperty item = items.GetArrayElementAtIndex(i);



                var idProp = item.FindPropertyRelative("ItemID");
                var nameProp = item.FindPropertyRelative("ItemName");
                var dropChanceProp = item.FindPropertyRelative("DropChance");

                EditorGUILayout.BeginHorizontal();

                if (nameProp != null)
                {
                    var selectedItemName = !string.IsNullOrEmpty(nameProp.stringValue) ? nameProp.stringValue : idProp.stringValue;
                    var selectedItemIndex = System.Array.IndexOf(itemNames, selectedItemName);
                    selectedItemIndex = EditorGUILayout.Popup(selectedItemIndex, itemNames, GUILayout.Width(320));

                    if (itemNames == null || selectedItemIndex >= itemNames.Length)
                    {
                        continue;
                    }

                    if (selectedItemIndex < itemNames.Length)
                    {
                        nameProp.stringValue = itemNames[selectedItemIndex];
                        idProp.stringValue = loadedItems.FirstOrDefault(x => x.Name == nameProp.stringValue)?.Id.ToString();
                    }
                }

                EditorGUILayout.LabelField("Drop Chance", GUILayout.Width(200));
                //dropChanceProp.floatValue = EditorGUILayout.Slider(dropChanceProp.floatValue, 0.0001f, 1f);

                var val = EditorGUILayout.Slider(dropChanceProp.floatValue * 100f, 0.01f, 100f);

                dropChanceProp.floatValue = val / 100f;

                //EditorGUILayout.PropertyField(dropChanceProp);

                if (GUILayout.Button("X"))
                {
                    items.DeleteArrayElementAtIndex(i.ElementIndex);
                    break;
                }

                EditorGUILayout.EndHorizontal();

                //EditorGUILayout.PropertyField(item);
                //EditorGUILayout.PropertyField(itemId);
                //EditorGUILayout.PropertyField(dropChance);
                //EditorGUILayout.PropertyField(item, new GUIContent("Element " + i));
            }

            if (GUILayout.Button("Add"))
            {
                items.InsertArrayElementAtIndex(items.arraySize - 1);
            }
        }

        //EditorGUILayout.PropertyField(items);
        if (dropList != null && dropList.objectReferenceValue)
        {

            var idl = dropList.objectReferenceValue as ItemDropList;

            if (idl != null && GUILayout.Button("Load from DropList"))
            {
                items.ClearArray();

                //items.arraySize = idl.Items.Length;
                for (var i = 0; i < idl.Items.Length; ++i)
                {
                    items.InsertArrayElementAtIndex(0);
                }

                for (var i = 0; i < idl.Items.Length; ++i)
                {
                    var elm = items.GetArrayElementAtIndex(i);
                    var idProp = elm.FindPropertyRelative("ItemID");
                    var nameProp = elm.FindPropertyRelative("ItemName");
                    var dropChanceProp = elm.FindPropertyRelative("DropChance");

                    var item = idl.Items[i];

                    idProp.stringValue = item.ItemID;
                    nameProp.stringValue = item.ItemName;
                    dropChanceProp.floatValue = item.DropChance;
                }
            }

            if (idl != null && GUILayout.Button("Copy all items to DropList"))
            {
                idl.Items = new ItemDrop[items.arraySize];

                for (var i = 0; i < items.arraySize; ++i)
                {
                    var drop = idl.Items[i] = new ItemDrop();

                    var sourceitem = items.GetArrayElementAtIndex(i);
                    var idProp = sourceitem.FindPropertyRelative("ItemID");
                    var nameProp = sourceitem.FindPropertyRelative("ItemName");
                    var dropChanceProp = sourceitem.FindPropertyRelative("DropChance");


                    drop.ItemID = idProp.stringValue;
                    drop.ItemName = nameProp.stringValue;
                    drop.DropChance = dropChanceProp.floatValue;
                    var tarItem = loadedItems.FirstOrDefault(x => x.Name == drop.ItemName || x.Name == drop.ItemID);

                    if (tarItem != null)
                    {
                        drop.ItemID = tarItem.Id.ToString();
                        drop.ItemName = tarItem.Name;
                    }

                }
                EditorUtility.SetDirty(idl);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    class PropVal
    {
        public SerializedProperty Prop;
        public float Value;
        public string Name;
        public string ID;
        public int ElementIndex;
    }
}
