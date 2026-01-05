            static Baseline2SmokeLastRunTool()
            {
                EditorApplication.playModeStateChanged += OnPlayMode;
                EditorApplication.update += () => { if (EditorPrefs.GetBool(PrefReportPending)) TryGenerateReportNow(); };
            }
