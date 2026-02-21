using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(AudioAlchemist))]
public class AudioAlchemistEditor : Editor
{
    private SerializedProperty soundSubjectsProp;
    private static MethodInfo playMethod;
    private static MethodInfo stopAllMethod;

    private Dictionary<int, bool> subjectFoldouts = new Dictionary<int, bool>();
    private Dictionary<int, Dictionary<int, bool>> soundFoldouts = new Dictionary<int, Dictionary<int, bool>>();

    private string searchString = "";
    private Texture headerTexture;

    private void OnEnable()
    {
        // Evitar errores si no hay un target válido
        if (target == null)
            return;

        // Buscar la propiedad del array de soundSubjects
        soundSubjectsProp = serializedObject.FindProperty("soundSubjects");

        // Cargar textura del header (opcional)
        headerTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Editor/Audio Alchemist/sprites/header.png"
        );

        // Preparar métodos de play/stop preview usando Reflection
        Type audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        if (audioUtil != null)
        {
            playMethod = audioUtil.GetMethod("PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            stopAllMethod = audioUtil.GetMethod("StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        // Inicializar foldouts
        RebuildFoldouts();
    }


    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        RebuildFoldouts();

        // ---------------------------------------------------------
        // HEADER
        // ---------------------------------------------------------
        if (headerTexture != null)
        {
            Rect headerRect = GUILayoutUtility.GetRect(0, 140, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(headerRect, headerTexture, ScaleMode.ScaleToFit);
        }

        EditorGUILayout.Space(6);

        // ---------------------------------------------------------
        // SEARCH BAR
        // ---------------------------------------------------------
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical(GUILayout.Width(320));
        GUIStyle centered = new GUIStyle(EditorStyles.label);
        centered.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("Search", centered);
        searchString = EditorGUILayout.TextField(searchString);
        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // ---------------------------------------------------------
        // GLOBAL BUTTONS
        // ---------------------------------------------------------
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Collapse All"))
        {
            foreach (int key in subjectFoldouts.Keys.ToList())
                subjectFoldouts[key] = false;

            foreach (var dict in soundFoldouts.Values)
                foreach (int key in dict.Keys.ToList())
                    dict[key] = false;
        }

        if (GUILayout.Button("Expand All"))
        {
            foreach (int key in subjectFoldouts.Keys.ToList())
                subjectFoldouts[key] = true;

            foreach (var dict in soundFoldouts.Values)
                foreach (int key in dict.Keys.ToList())
                    dict[key] = true;
        }

        if (GUILayout.Button("Stop All Previews"))
        {
            if (stopAllMethod != null) stopAllMethod.Invoke(null, null);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // ---------------------------------------------------------
        // SETTINGS
        // ---------------------------------------------------------
        EditorGUILayout.LabelField("Audio Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mixer"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("masterParam"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("musicParam"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sfxParam"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("uiParam"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSfxSources"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("uiPoolSize"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("destroyOnLoad"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("logWarnings"));

        EditorGUILayout.Space();


        // ---------------------------------------------------------
        // GROUPS
        // ---------------------------------------------------------
        for (int i = 0; i < soundSubjectsProp.arraySize; i++)
        {
            RebuildFoldouts();

            if (i >= soundSubjectsProp.arraySize) break;

            var subject = soundSubjectsProp.GetArrayElementAtIndex(i);
            var groupNameProp = subject.FindPropertyRelative("groupName");
            var sounds = subject.FindPropertyRelative("sounds");

            string groupName = groupNameProp.stringValue;

            // FILTER
            if (!string.IsNullOrEmpty(searchString))
            {
                if (!groupName.ToLower().Contains(searchString.ToLower()) &&
                    !SoundsMatchSearch(sounds, searchString))
                    continue;
            }

            // INIT FOLDOUT KEYS
            if (!subjectFoldouts.ContainsKey(i)) subjectFoldouts[i] = true;
            if (!soundFoldouts.ContainsKey(i)) soundFoldouts[i] = new Dictionary<int, bool>();

            // ---------------------------------------------------------
            // GROUP BOX
            // ---------------------------------------------------------
            Color original = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 1f, 0.85f, 1f);
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = original;

            // GROUP HEADER
            EditorGUILayout.BeginHorizontal();
            Texture folderIcon = EditorGUIUtility.IconContent("Folder").image;
            Rect fRect = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(5));
            subjectFoldouts[i] = EditorGUI.Foldout(fRect, subjectFoldouts[i], GUIContent.none, true);

            Rect iconRect = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18));
            GUI.DrawTexture(iconRect, folderIcon, ScaleMode.ScaleToFit);

            groupNameProp.stringValue = GUILayout.TextField(groupNameProp.stringValue, GUILayout.MinWidth(120), GUILayout.MaxWidth(200));

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField($"#{sounds.arraySize}", GUILayout.Width(30));

            GUIStyle xStyle = new GUIStyle(GUI.skin.button);
            xStyle.fixedWidth = 22;
            xStyle.fixedHeight = 18;

            if (GUILayout.Button("✖", xStyle))
            {
                soundSubjectsProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                RebuildFoldouts();
                break;
            }

            EditorGUILayout.EndHorizontal();

            // ---------------------------------------------------------
            // GROUP CONTENT
            // ---------------------------------------------------------
            if (subjectFoldouts[i])
            {
                EditorGUI.indentLevel++;

                for (int j = 0; j < sounds.arraySize; j++)
                {
                    RebuildFoldouts();

                    if (j >= sounds.arraySize) break;

                    var sound = sounds.GetArrayElementAtIndex(j);
                    var clipProp = sound.FindPropertyRelative("clip");
                    var idProp = sound.FindPropertyRelative("id");

                    AudioClip clip = clipProp.objectReferenceValue as AudioClip;

                    // SAFE ENUM INDEX
                    int enumIndex = idProp.enumValueIndex;
                    string id = "InvalidEnum";
                    if (enumIndex >= 0 && enumIndex < idProp.enumNames.Length)
                        id = idProp.enumNames[enumIndex];

                    // Search filter
                    if (!string.IsNullOrEmpty(searchString))
                    {
                        if (!((clip != null && clip.name.ToLower().Contains(searchString.ToLower())) ||
                              id.ToLower().Contains(searchString.ToLower())))
                            continue;
                    }

                    // Ensure soundFoldouts exists
                    if (!soundFoldouts[i].ContainsKey(j))
                        soundFoldouts[i][j] = false;

                    string displayName = clip != null ? clip.name : id;

                    // Color based on priority
                    int priority = sound.FindPropertyRelative("priority").intValue;
                    Color bg = Color.Lerp(Color.green, Color.red, priority / 256f);
                    if (!string.IsNullOrEmpty(searchString)) bg = Color.yellow;

                    original = GUI.backgroundColor;
                    GUI.backgroundColor = bg * 0.3f + Color.white * 0.7f;
                    EditorGUILayout.BeginVertical("box");
                    GUI.backgroundColor = original;

                    // SOUND HEADER
                    EditorGUILayout.BeginHorizontal();
                    soundFoldouts[i][j] = EditorGUILayout.Foldout(soundFoldouts[i][j], $"🎵 {displayName}", true);

                    GUILayout.FlexibleSpace();

                    GUIStyle sx = new GUIStyle(GUI.skin.button);
                    sx.fixedWidth = 22;
                    sx.fixedHeight = 18;

                    if (GUILayout.Button("✖", sx))
                    {
                        sounds.DeleteArrayElementAtIndex(j);
                        serializedObject.ApplyModifiedProperties();
                        RebuildFoldouts();
                        break;
                    }

                    EditorGUILayout.EndHorizontal();

                    // SOUND CONTENT
                    if (soundFoldouts[i][j])
                    {
                        EditorGUI.indentLevel++;

                        EnsureSoundDefaults(sound);

                        EditorGUILayout.PropertyField(idProp);
                        EditorGUILayout.PropertyField(clipProp);

                        EditorGUILayout.LabelField("Volume", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(sound.FindPropertyRelative("volume"));
                        EditorGUILayout.PropertyField(sound.FindPropertyRelative("volumeMin"));
                        EditorGUILayout.PropertyField(sound.FindPropertyRelative("volumeMax"));

                        EditorGUILayout.LabelField("Pitch", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(sound.FindPropertyRelative("pitch"));
                        EditorGUILayout.PropertyField(sound.FindPropertyRelative("pitchMin"));
                        EditorGUILayout.PropertyField(sound.FindPropertyRelative("pitchMax"));

                        EditorGUILayout.PropertyField(sound.FindPropertyRelative("spatial"));
                        EditorGUILayout.PropertyField(sound.FindPropertyRelative("mixerGroup"));

                        EditorGUILayout.Space();

                        // LOOP + FADE IN
                        EditorGUILayout.PropertyField(sound.FindPropertyRelative("loop"));

                        var fadeInProp = sound.FindPropertyRelative("fadeIn");
                        var fadeInDurProp = sound.FindPropertyRelative("fadeInDuration");

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(fadeInProp);
                        if (fadeInProp.boolValue)
                            EditorGUILayout.PropertyField(fadeInDurProp, new GUIContent("Duration"));
                        EditorGUILayout.EndHorizontal();

                        // FADE OUT
                        var fadeOutProp = sound.FindPropertyRelative("fadeOut");
                        var fadeOutDurProp = sound.FindPropertyRelative("fadeOutDuration");

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(fadeOutProp);
                        if (fadeOutProp.boolValue)
                            EditorGUILayout.PropertyField(fadeOutDurProp, new GUIContent("Duration"));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.Space();

                        // MINI PLAYER
                        EditorGUILayout.BeginHorizontal();

                        if (GUILayout.Button("▶ Play"))
                        {
                            if (clip != null)
                            {
                                if (stopAllMethod != null) stopAllMethod.Invoke(null, null);
                                if (playMethod != null) playMethod.Invoke(null, new object[] { clip, 0, false });
                            }
                        }

                        if (GUILayout.Button("⏹ Stop All"))
                        {
                            if (stopAllMethod != null) stopAllMethod.Invoke(null, null);
                        }

                        if (Application.isPlaying)
                        {
                            if (GUILayout.Button("▶ Play (Runtime)"))
                            {
                                AudioID enumID = AudioID.None;

                                int idx = idProp.enumValueIndex;
                                if (idx >= 0 && idx < idProp.enumNames.Length)
                                    enumID = (AudioID)idx;

                                bool loop = sound.FindPropertyRelative("loop").boolValue;
                                float vol = sound.FindPropertyRelative("volume").floatValue;
                                float pitch = sound.FindPropertyRelative("pitch").floatValue;

                                if (loop)
                                    AudioAlchemist.Instance.Play(enumID);
                                else
                                    AudioAlchemist.Instance.PlayOneShot(enumID);
                            }
                        }

                        EditorGUILayout.EndHorizontal();
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                }

                // ADD SOUND BUTTON
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("➕ Add Sound", GUILayout.Width(160)))
                {
                    int old = sounds.arraySize;
                    sounds.arraySize++;
                    var s = sounds.GetArrayElementAtIndex(old);
                    AssignDefaults(s);

                    serializedObject.ApplyModifiedProperties();
                    RebuildFoldouts();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        // ADD GROUP BUTTON
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("➕ Add Group", GUILayout.Width(160)))
        {
            int old = soundSubjectsProp.arraySize;
            soundSubjectsProp.arraySize++;
            var g = soundSubjectsProp.GetArrayElementAtIndex(old);
            g.FindPropertyRelative("groupName").stringValue = "";
            g.FindPropertyRelative("sounds").arraySize = 0;

            serializedObject.ApplyModifiedProperties();
            RebuildFoldouts();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();

        // SHORTCUTS
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.shift)
        {
            if (e.keyCode == KeyCode.P) TriggerPlaySelected();
            if (e.keyCode == KeyCode.S) TriggerStopAll();
        }
    }

    // ---------------------------------------------------------
    // SEARCH HELPERS
    // ---------------------------------------------------------
    private bool SoundsMatchSearch(SerializedProperty sounds, string search)
    {
        search = search.ToLower();

        for (int j = 0; j < sounds.arraySize; j++)
        {
            var s = sounds.GetArrayElementAtIndex(j);
            var clip = s.FindPropertyRelative("clip").objectReferenceValue as AudioClip;

            var idP = s.FindPropertyRelative("id");
            int enumIndex = idP.enumValueIndex;

            string id = "InvalidEnum";
            if (enumIndex >= 0 && enumIndex < idP.enumNames.Length)
                id = idP.enumNames[enumIndex];

            if ((clip != null && clip.name.ToLower().Contains(search)) ||
                id.ToLower().Contains(search))
                return true;
        }

        return false;
    }

    // ---------------------------------------------------------
    // DEFAULT ASSIGNMENT
    // ---------------------------------------------------------
    private void EnsureSoundDefaults(SerializedProperty s)
    {
        if (s == null) return;

        void SetDefault(SerializedProperty p, float v)
        {
            if (Math.Abs(p.floatValue) < 0.0001f) p.floatValue = v;
        }

        SetDefault(s.FindPropertyRelative("volume"), 0.5f);
        SetDefault(s.FindPropertyRelative("volumeMin"), 0.5f);
        SetDefault(s.FindPropertyRelative("volumeMax"), 0.5f);
        SetDefault(s.FindPropertyRelative("pitch"), 1f);
        SetDefault(s.FindPropertyRelative("pitchMin"), 1f);
        SetDefault(s.FindPropertyRelative("pitchMax"), 1f);
        SetDefault(s.FindPropertyRelative("spatial"), 0f);
        SetDefault(s.FindPropertyRelative("fadeInDuration"), 0.2f);
        SetDefault(s.FindPropertyRelative("fadeOutDuration"), 0.2f);

        if (s.FindPropertyRelative("priority").intValue == 0)
            s.FindPropertyRelative("priority").intValue = 50;
    }

    private void AssignDefaults(SerializedProperty s)
    {
        s.FindPropertyRelative("clip").objectReferenceValue = null;
        s.FindPropertyRelative("volume").floatValue = 0.5f;
        s.FindPropertyRelative("volumeMin").floatValue = 0.5f;
        s.FindPropertyRelative("volumeMax").floatValue = 0.5f;
        s.FindPropertyRelative("pitch").floatValue = 1f;
        s.FindPropertyRelative("pitchMin").floatValue = 1f;
        s.FindPropertyRelative("pitchMax").floatValue = 1f;
        s.FindPropertyRelative("spatial").floatValue = 0f;
        s.FindPropertyRelative("loop").boolValue = false;
        s.FindPropertyRelative("fadeIn").boolValue = false;
        s.FindPropertyRelative("fadeOut").boolValue = false;
        s.FindPropertyRelative("fadeInDuration").floatValue = 0.2f;
        s.FindPropertyRelative("fadeOutDuration").floatValue = 0.2f;
        s.FindPropertyRelative("priority").intValue = 50;
        s.FindPropertyRelative("id").enumValueIndex = 0;
    }

    // ---------------------------------------------------------
    // REBUILD FOLDOUT STATE
    // ---------------------------------------------------------
    private void RebuildFoldouts()
    {
        if (soundSubjectsProp == null)
            return;

        int subjectCount = soundSubjectsProp.arraySize;

        // Inicializar o sincronizar foldouts de grupos
        for (int i = 0; i < subjectCount; i++)
        {
            if (!subjectFoldouts.ContainsKey(i))
                subjectFoldouts[i] = false;

            // Inicializar foldouts anidados de sonidos
            if (!soundFoldouts.ContainsKey(i))
                soundFoldouts[i] = new Dictionary<int, bool>();

            var soundsProp = soundSubjectsProp.GetArrayElementAtIndex(i).FindPropertyRelative("sounds");
            if (soundsProp == null)
                continue;

            for (int j = 0; j < soundsProp.arraySize; j++)
            {
                if (!soundFoldouts[i].ContainsKey(j))
                    soundFoldouts[i][j] = false;
            }

            // Eliminar foldouts de sonidos obsoletos
            var existingKeys = soundFoldouts[i].Keys.ToList();
            foreach (int key in existingKeys)
            {
                if (key >= soundsProp.arraySize)
                    soundFoldouts[i].Remove(key);
            }
        }

        // Eliminar foldouts de grupos obsoletos
        var subjectKeys = subjectFoldouts.Keys.ToList();
        foreach (int key in subjectKeys)
        {
            if (key >= subjectCount)
                subjectFoldouts.Remove(key);
        }
    }


    // ---------------------------------------------------------
    // PLAY SELECTED
    // ---------------------------------------------------------
    private void TriggerPlaySelected()
    {
        RebuildFoldouts();

        for (int i = 0; i < soundSubjectsProp.arraySize; i++)
        {
            if (!subjectFoldouts.ContainsKey(i) || !subjectFoldouts[i]) continue;

            var sounds = soundSubjectsProp.GetArrayElementAtIndex(i).FindPropertyRelative("sounds");

            for (int j = 0; j < sounds.arraySize; j++)
            {
                if (!soundFoldouts.ContainsKey(i) ||
                    !soundFoldouts[i].ContainsKey(j) ||
                    !soundFoldouts[i][j])
                    continue;

                var s = sounds.GetArrayElementAtIndex(j);

                var clip = s.FindPropertyRelative("clip").objectReferenceValue as AudioClip;
                if (clip == null) continue;

                int enumIndex = s.FindPropertyRelative("id").enumValueIndex;

                if (enumIndex < 0 || enumIndex >= Enum.GetValues(typeof(AudioID)).Length)
                    continue;

                AudioID id = (AudioID)enumIndex;

                if (AudioAlchemist.Instance != null)
                {
                    float vol = s.FindPropertyRelative("volume").floatValue;
                    float pitch = s.FindPropertyRelative("pitch").floatValue;
                    bool loop = s.FindPropertyRelative("loop").boolValue;
                    AudioAlchemist.Instance.PlayCustom((int)id, vol, pitch, loop);
                }
            }
        }
    }

    private void TriggerStopAll()
    {
        if (stopAllMethod != null) stopAllMethod.Invoke(null, null);
    }
}
