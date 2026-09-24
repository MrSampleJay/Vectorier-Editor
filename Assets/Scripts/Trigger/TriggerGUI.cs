using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Vectorier.Component;
using Vectorier.EditorScript.Tools;

namespace Vectorier.Trigger
{
    [Serializable]
    public class TriggerGUI : EditorWindow
    {
        [SerializeField]
        TriggerComponent targetObject;

        [SerializeField]
        Dictionary<TriggerLoop, ReorderableList> ActionLists;

        [SerializeField]
        Dictionary<TriggerLoop, ReorderableList> EventLists;

        [SerializeField]
        List<TriggerLoop> Loops;

        // For temporary storage of template loops when parsing a template
        // Which is used for previews
        List<TriggerLoop> TemplateContainer;

        string TemplateName;

        bool _IsUsingTemplate;

        bool _AutoSave = true;

        private void OnEnable()
        {
            _IsUsingTemplate = false;
            Loops = new();
            ActionLists = new();
            EventLists = new();
            TemplateManager.Init();
            TemplateManager.OpenDoc();
            LoadXML(targetObject.contentXml);
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnProjectChange()
        {
            
        }

        [UnityEditor.Callbacks.DidReloadScripts]        
        public static void OnReload()
        {
            TemplateManager.Init();
            TemplateManager.OpenDoc();

        }

        private void OnUndoRedoPerformed()
        {
            if (!targetObject)
                return;

            LoadXML(targetObject.contentXml);
        }

        private void RecordUndo(string actionName)
        {
            if (targetObject)
                Undo.RecordObject(targetObject, actionName);
        }

        private void SaveToTarget()
        {
            if (!targetObject || Loops == null)
                return;

            // Rebuilding runtime references is handled by TriggerComponent now.

            EditorUtility.SetDirty(targetObject);

            if (!_AutoSave)
                return;

            targetObject.contentXml = BuildContent();
            EditorUtility.SetDirty(targetObject);
        }

        private string BuildContent()
        {
            var doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Content");
            doc.AppendChild(root);

            if (Loops != null)
            {
                foreach (TriggerLoop loop in Loops)
                    loop?.AppendToElement(root, doc);
            }

            var stringBuilder = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                ConformanceLevel = ConformanceLevel.Fragment
            };

            using (var writer = XmlWriter.Create(stringBuilder, settings))
            {
                // Write Template first if using one
                if (_IsUsingTemplate && !string.IsNullOrEmpty(TemplateName))
                {
                    writer.WriteStartElement("Template");
                    writer.WriteAttributeString("Name", TemplateName);
                    writer.WriteEndElement();
                }

                // Write all child nodes of the root (the loops)
                foreach (XmlNode child in root.ChildNodes)
                {
                    child.WriteTo(writer);
                }
            }

            return stringBuilder.ToString();
        }

        // TriggerData component removed: no longer create or store data on the GameObject.

        private void OnGUI()
        {
            TriggerComponent selectedComp;
            string templateName = "";
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Editing Component:");
            selectedComp = EditorGUILayout.ObjectField(targetObject, typeof(TriggerComponent), true) as TriggerComponent;
            GUILayout.EndHorizontal();

            // TODO: Add a toggle for auto-save
            // TODO: Add a template path field for the template manager

            ProcessKeyEvents();

            GUILayout.BeginHorizontal();
            if (_IsUsingTemplate)
            {
                templateName = TemplateManager.TemplateEnum(EditorGUILayout.GetControlRect(), TemplateName);
            }

            _IsUsingTemplate = GUILayout.Toggle(_IsUsingTemplate, "Use Template", GUILayout.Width(100f));
            GUILayout.EndHorizontal();

            if (selectedComp != targetObject)
            {
                targetObject = selectedComp;
                Loops = new List<TriggerLoop>();
                LoadXML(selectedComp ? selectedComp.contentXml : string.Empty);
            }

            if (templateName != TemplateName)
            {
                ParseTemplate(templateName);
                TemplateName = templateName;
            }

            if (Event.current != null &&
                (Event.current.type == EventType.MouseDown || Event.current.type == EventType.KeyDown))
                RecordUndo("Edit Trigger");

            DisplayLoops();
            if (GUI.changed)
            {
                SaveToTarget();
            }
        }

        private void DisplayLoops()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Loops");

            TriggerNode.EditingAttributes = GUILayout.Toggle(TriggerNode.EditingAttributes, "Edit Attributes", GUILayout.Width(100f));

            // Add Button!
            if (GUILayout.Button("+", GUILayout.Width(20f)))
            {
                InitializeLoop(Loops);
            }
            GUILayout.EndHorizontal();

            // Display All The Loops
            if (Loops == null || ActionLists == null || EventLists == null)
                return;

            foreach (TriggerLoop loop in Loops)
            {
                if (loop == null)
                    continue;

                bool foundActions = ActionLists.TryGetValue(loop, out ReorderableList list);
                bool foundEvents = EventLists.TryGetValue(loop, out ReorderableList list1);

                if (foundActions && foundEvents)
                {
                    loop.Display(list, list1);
                }
                else
                {
                    EditorGUILayout.HelpBox("Error Displaying Loop: Missing Lists", MessageType.Error);
                }
            }

            // :skull:
            for(int i = 0; i < Loops.Count; i++)
            {
                TriggerLoop loop = Loops[i];
                if (loop == null)
                    continue;

                if (loop.ListAction == LoopListQueuedAction.Delete)
                {
                    RecordUndo("Delete Trigger Loop");
                    Loops.Remove(loop);
                    SaveToTarget();
                }
                int index = Loops.IndexOf(loop);
                if (index < 0)
                    continue;

                if (loop.ListAction == LoopListQueuedAction.MoveUp && index > 0)
                {
                    RecordUndo("Move Trigger Loop");
                    Loops.RemoveAt(index);
                    index--;
                    Loops.Insert(index, loop);
                    SaveToTarget();
                }

                if (loop.ListAction == LoopListQueuedAction.MoveDown && index < Loops.Count - 1)
                {
                    RecordUndo("Move Trigger Loop");
                    Loops.RemoveAt(index);
                    index++;
                    Loops.Insert(index, loop);
                    SaveToTarget();
                }
            }
        }

        private void ProcessKeyEvents()
        {
            Event e = Event.current;
            bool altPressed = e.type == EventType.KeyDown && e.keyCode == KeyCode.LeftAlt || e.keyCode == KeyCode.RightAlt;
            bool tPressed = e.type == EventType.KeyDown && e.keyCode == KeyCode.T;
            if (altPressed)
            {
                TriggerNode.EditingAttributes = !TriggerNode.EditingAttributes;
                e.Use();
            }
        }

        private ReorderableList InitReorderableList(List<TriggerNode> nodeList, string headerName = "List", bool isTemplate = false)
        {
            ReorderableList List = new ReorderableList(nodeList, typeof(TriggerNode), true, false, true, true);
            List.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (nodeList == null || index < 0 || index >= nodeList.Count || nodeList[index] == null)
                    return;

                TriggerNode currAction = nodeList[index];
                float width = position.width - 20f;

                if (index % 2 == 0)
                {
                    Rect fullRect = new Rect(rect.x - 20f, rect.y, rect.width + 25f, rect.height);
                    EditorGUI.DrawRect(fullRect, new Color(0f, 0f, 0f, 0.20f));
                }

                currAction.Display(rect, width);
            };

            List.elementHeightCallback = (index) =>
            {
                if (List.list == null || index < 0 || index >= List.list.Count)
                    return EditorGUIUtility.singleLineHeight;

                float width = position.width - 20f;
                TriggerNode node = List.list[index] as TriggerNode;
                return node == null ? EditorGUIUtility.singleLineHeight : node.GetHeight(width);
            };

            List.onRemoveCallback = (actionList) =>
            {
                if (actionList == null || actionList.list == null || actionList.index < 0 ||
                    actionList.index >= actionList.list.Count || actionList.list[actionList.index] is not TriggerNode node)
                    return;

                RecordUndo("Remove Trigger Node");
                node.RemoveFromList();
                SaveToTarget();
            };

            List.onMouseDragCallback = RecordUndoBeforeReorder;
            List.onReorderCallback = actionList => SaveToTarget();

            // Header drawing logic will be handled by the classes handling the list display, so we just set a simple label here.
            List.drawHeaderCallback = (rect) =>
            {
                EditorGUI.LabelField(rect, headerName, EditorStyles.boldLabel);
            };

            return List;
        }

        private ReorderableList GetList(Dictionary<TriggerLoop, ReorderableList> listDict, TriggerLoop loop)
        {
            if (listDict == null || loop == null)
                return null;
            listDict.TryGetValue(loop, out ReorderableList list);
            return list;
        }

        private void LoadXML(string contentInnerXml)
        {
            Loops ??= new();
            ActionLists ??= new();
            EventLists ??= new();
            Loops.Clear();
            ActionLists.Clear();
            EventLists.Clear();
            TemplateName = string.Empty;
            _IsUsingTemplate = false;

            var doc = new XmlDocument();
            try
            {
                doc.LoadXml("<Content>" + (contentInnerXml ?? "") + "</Content>");
            }
            catch (XmlException)
            {
                return;
            }
            var root = doc.DocumentElement;

            if (root == null) 
                return;
            if (root?["Template"] != null)
            {
                string template = root["Template"].Attributes?["Name"]?.Value;
                TemplateName = template;
                _IsUsingTemplate = true;
                if (!string.IsNullOrEmpty(template))
                {
                    ParseTemplate(template);
                }
            }

            foreach(XmlNode node in root.ChildNodes)
            {
                // lmao
                if(node.Name == "Loop")
                    InitializeLoop(Loops, node);
            }
            Repaint();
        }

        private void ParseTemplate(string template)
        {
            XmlNode templateNode = TemplateManager.GetTemplate(template);
            if (templateNode != null)
            {
                foreach (XmlNode node in templateNode.ChildNodes)
                {
                    if (node.Name == "Loop")
                        InitializeLoop(TemplateContainer, node);
                }
            }
        }

        private void InitializeLoop(List<TriggerLoop> Loops, XmlNode node = null)
        {
            TriggerLoop newLoop = null;
            if (node != null)
                newLoop = new TriggerLoop(node, Loops);
            else
                newLoop = new TriggerLoop(Loops);

            ReorderableList newEventList = InitReorderableList(newLoop.Events, "Events");
            ReorderableList newActionList = InitReorderableList(newLoop.Actions, "Actions");

            newActionList.onAddCallback = (actionList) =>
            {
                if (actionList?.list == null)
                    return;
                RecordUndo("Add Trigger Action");
                actionList.list.Add(new TriggerAction((List<TriggerNode>)actionList.list));
                SaveToTarget();
            };

            newEventList.onAddCallback = (eventList) =>
            {
                if (eventList?.list == null)
                    return;
                RecordUndo("Add Trigger Event");
                eventList.list.Add(new TriggerEvent((List<TriggerNode>)eventList.list));
                SaveToTarget();
            };

            EventLists.Add(newLoop, newEventList);
            ActionLists.Add(newLoop, newActionList);
            Loops.Add(newLoop);
        }

        private void RecordUndoBeforeReorder(ReorderableList list)
        {
            RecordUndo("Reorder Trigger Nodes");
        }

        [MenuItem("Vectorier/Tools/Testify", false, 38)]
        public static TriggerGUI OpenWindow()
        {
            var window = GetWindow<TriggerGUI>("Testify");
            window.Show();
            return window;
        }
    }
}
