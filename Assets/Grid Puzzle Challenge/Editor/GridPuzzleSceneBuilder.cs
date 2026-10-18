using System;
using System.IO;
using GridPuzzle.Audio;
using GridPuzzle.Config;
using GridPuzzle.Core;
using GridPuzzle.Input;
using GridPuzzle.UI;
using GridPuzzle.View;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GridPuzzle.Editor
{
    /// <summary>
    /// Automated production builder that programmatically creates the complete playable
    /// Grid Puzzle Challenge game with all configurations, prefabs, UI hierarchy, and scene setup.
    /// </summary>
    [InitializeOnLoad]
    public static class GridPuzzleSceneBuilder
    {
        private const string RootFolder = "Assets/Grid Puzzle Challenge";
        private const string MaterialsFolder = RootFolder + "/Materials";
        private const string PrefabsFolder = RootFolder + "/Prefabs";
        private const string ConfigFolder = RootFolder + "/Config";
        private const string ScenesFolder = RootFolder + "/Scenes";

        static GridPuzzleSceneBuilder()
        {
            EditorApplication.delayCall += AutoBuildIfMissing;
        }

        private static void AutoBuildIfMissing()
        {
            string scenePath = $"{ScenesFolder}/GridPuzzleGame.unity";
            if (!File.Exists(scenePath))
            {
                BuildAll();
            }
        }

        [MenuItem("Grid Puzzle/Build Complete Playable Scene")]
        public static void BuildAll()
        {
            Debug.Log("[GridPuzzleBuilder] Starting automated build for 'Grid Puzzle Challenge'...");

            EnsureFoldersExist();

            // 1. Create or load Config asset
            var config = CreateOrLoadConfig();

            // 2. Create Prefabs (Cell & Tile)
            var (prefabCell, prefabTile) = CreatePrefabs(config);

            // 3. Create Complete Scene
            CreateScene(config, prefabCell, prefabTile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[GridPuzzleBuilder] ✅ Successfully built complete playable scene in Assets/Grid Puzzle Challenge/Scenes/GridPuzzleGame.unity!");
        }

        private static void EnsureFoldersExist()
        {
            CreateFolderIfMissing("Assets", "Grid Puzzle Challenge");
            CreateFolderIfMissing(RootFolder, "Materials");
            CreateFolderIfMissing(RootFolder, "Prefabs");
            CreateFolderIfMissing(RootFolder, "Config");
            CreateFolderIfMissing(RootFolder, "Scenes");
            CreateFolderIfMissing(RootFolder, "Scripts");
            CreateFolderIfMissing(RootFolder, "Audio");
        }

        private static void CreateFolderIfMissing(string parent, string newFolder)
        {
            string full = $"{parent}/{newFolder}";
            if (!AssetDatabase.IsValidFolder(full))
            {
                AssetDatabase.CreateFolder(parent, newFolder);
            }
        }

        private static GridPuzzleConfig CreateOrLoadConfig()
        {
            string path = $"{ConfigFolder}/GridPuzzleConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<GridPuzzleConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GridPuzzleConfig>();
                config.rows = 4;
                config.columns = 4;
                config.initialStepBudget = 30;
                config.targetScore = 2048;
                config.targetTileValue = 2048;
                config.startingTileCount = 2;
                config.startingIceCount = 3;
                config.startingStoneCount = 1;
                config.slideDuration = 0.14f;
                config.spawnScaleDuration = 0.18f;
                config.mergePopDuration = 0.16f;

                AssetDatabase.CreateAsset(config, path);
            }
            return config;
        }

        private static (GameObject prefabCell, TileView prefabTile) CreatePrefabs(GridPuzzleConfig config)
        {
            // 1. Cell Prefab
            string cellPath = $"{PrefabsFolder}/Cell_Prefab.prefab";
            GameObject cellGo = new GameObject("Cell_Prefab", typeof(RectTransform), typeof(Image));
            var cellRt = cellGo.GetComponent<RectTransform>();
            cellRt.sizeDelta = new Vector2(100f, 100f);
            var cellImg = cellGo.GetComponent<Image>();
            cellImg.color = config.cellSlotColor;

            GameObject prefabCell = PrefabUtility.SaveAsPrefabAsset(cellGo, cellPath);
            UnityEngine.Object.DestroyImmediate(cellGo);

            // 2. Tile Prefab
            string tilePath = $"{PrefabsFolder}/Tile_Prefab.prefab";
            GameObject tileGo = new GameObject("Tile_Prefab", typeof(RectTransform), typeof(Image));
            var tileRt = tileGo.GetComponent<RectTransform>();
            tileRt.sizeDelta = new Vector2(100f, 100f);
            var tileImg = tileGo.GetComponent<Image>();
            tileImg.color = config.tile2;

            // Value Text (TMP)
            GameObject textGo = new GameObject("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(tileGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "2";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 38f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = config.GetTextColor(2);

            // Obstacle Badge Text (TMP)
            GameObject badgeGo = new GameObject("ObstacleBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeGo.transform.SetParent(tileGo.transform, false);
            var badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.5f, 0.7f);
            badgeRt.anchorMax = new Vector2(1f, 1f);
            badgeRt.sizeDelta = Vector2.zero;

            var badgeTmp = badgeGo.GetComponent<TextMeshProUGUI>();
            badgeTmp.text = "❄ 2";
            badgeTmp.alignment = TextAlignmentOptions.TopRight;
            badgeTmp.fontSize = 18f;
            badgeTmp.color = Color.white;
            badgeGo.SetActive(false);

            // Wire TileView
            TileView tileView = tileGo.AddComponent<TileView>();
            SerializedObject soTile = new SerializedObject(tileView);
            soTile.FindProperty("backgroundGraphic").objectReferenceValue = tileImg;
            soTile.FindProperty("valueText").objectReferenceValue = tmp;
            soTile.FindProperty("obstacleBadgeText").objectReferenceValue = badgeTmp;
            soTile.ApplyModifiedProperties();

            GameObject savedTilePrefab = PrefabUtility.SaveAsPrefabAsset(tileGo, tilePath);
            UnityEngine.Object.DestroyImmediate(tileGo);

            return (prefabCell, savedTilePrefab.GetComponent<TileView>());
        }

        private static void CreateScene(GridPuzzleConfig config, GameObject prefabCell, TileView prefabTile)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f); // Dark slate
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            camObj.transform.position = new Vector3(0f, 0f, -10f);
            camObj.tag = "MainCamera";

            // 2. EventSystem
            CreateEventSystem();

            // 3. UI Canvas Hierarchy
            GameObject canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            // Fullscreen Swipe Detector background
            GameObject bgDetector = new GameObject("SwipeDetector", typeof(RectTransform), typeof(Image), typeof(SwipeInputDetector));
            bgDetector.transform.SetParent(canvasObj.transform, false);
            var bgRt = bgDetector.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bgDetector.GetComponent<Image>();
            bgImg.color = new Color(0.08f, 0.09f, 0.12f, 1f); // Seamless dark backdrop
            var swipeInput = bgDetector.GetComponent<SwipeInputDetector>();

            // UI Root Container
            GameObject uiRoot = new GameObject("UIRoot", typeof(RectTransform));
            uiRoot.transform.SetParent(canvasObj.transform, false);
            var uiRt = uiRoot.GetComponent<RectTransform>();
            uiRt.anchorMin = Vector2.zero;
            uiRt.anchorMax = Vector2.one;
            uiRt.sizeDelta = Vector2.zero;

            // Header Section
            var headerGo = CreatePanel(uiRoot.transform, "HeaderPanel", new Vector2(0f, 0.72f), new Vector2(1f, 0.98f));

            // Title
            var titleTmp = CreateTMPText(headerGo.transform, "TitleText", "GRID PUZZLE CHALLENGE", 44f, FontStyles.Bold, new Color(0.95f, 0.96f, 0.98f), new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.95f));

            // Metrics Row: Score, Best, Steps
            var metricsRow = CreatePanel(headerGo.transform, "MetricsRow", new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.72f));

            var (scoreBadge, scoreText) = CreateMetricBadge(metricsRow.transform, "ScoreBox", "SCORE", "0", new Vector2(0f, 0f), new Vector2(0.31f, 1f));
            var (bestBadge, bestText) = CreateMetricBadge(metricsRow.transform, "BestBox", "BEST", "0", new Vector2(0.34f, 0f), new Vector2(0.65f, 1f));
            var (stepsBadge, stepsText) = CreateMetricBadge(metricsRow.transform, "StepsBox", "STEPS", config.initialStepBudget.ToString(), new Vector2(0.68f, 0f), new Vector2(1f, 1f));

            // Objective Text
            var objText = CreateTMPText(headerGo.transform, "ObjectiveBanner", $"❄ {config.startingIceCount} Left  |  Goal: {config.targetScore}", 28f, FontStyles.Normal, new Color(0.4f, 0.8f, 1.0f), new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.25f));

            // Combo Banner (floating over board)
            var comboText = CreateTMPText(uiRoot.transform, "ComboBanner", "COMBO x2.0! 🔥", 36f, FontStyles.Bold, new Color(1f, 0.85f, 0.2f), new Vector2(0.1f, 0.67f), new Vector2(0.9f, 0.73f));
            comboText.gameObject.SetActive(false);

            // Board Area
            GameObject boardObj = new GameObject("Board", typeof(RectTransform), typeof(Image), typeof(GridBoardView));
            boardObj.transform.SetParent(uiRoot.transform, false);
            var boardRt = boardObj.GetComponent<RectTransform>();
            boardRt.anchorMin = new Vector2(0.5f, 0.42f);
            boardRt.anchorMax = new Vector2(0.5f, 0.42f);
            boardRt.sizeDelta = new Vector2(760f, 760f);

            var boardImg = boardObj.GetComponent<Image>();
            boardImg.color = config.boardBackgroundColor;

            // Slots & Tiles Containers inside Board
            GameObject slotsContainer = new GameObject("SlotsContainer", typeof(RectTransform));
            slotsContainer.transform.SetParent(boardObj.transform, false);
            var slotsRt = slotsContainer.GetComponent<RectTransform>();
            slotsRt.anchorMin = Vector2.zero;
            slotsRt.anchorMax = Vector2.one;
            slotsRt.sizeDelta = Vector2.zero;

            GameObject tilesContainer = new GameObject("TilesContainer", typeof(RectTransform));
            tilesContainer.transform.SetParent(boardObj.transform, false);
            var tilesRt = tilesContainer.GetComponent<RectTransform>();
            tilesRt.anchorMin = Vector2.zero;
            tilesRt.anchorMax = Vector2.one;
            tilesRt.sizeDelta = Vector2.zero;

            // Wire GridBoardView
            GridBoardView boardView = boardObj.GetComponent<GridBoardView>();
            SerializedObject soBoard = new SerializedObject(boardView);
            soBoard.FindProperty("boardContainer").objectReferenceValue = boardRt;
            soBoard.FindProperty("slotsContainer").objectReferenceValue = slotsRt;
            soBoard.FindProperty("tilesContainer").objectReferenceValue = tilesRt;
            soBoard.FindProperty("slotPrefab").objectReferenceValue = prefabCell;
            soBoard.FindProperty("tilePrefab").objectReferenceValue = prefabTile;
            soBoard.ApplyModifiedProperties();

            // Controls Bar
            var controlsRow = CreatePanel(uiRoot.transform, "ControlsRow", new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.18f));
            var (undoBtn, undoBadge) = CreateActionButton(controlsRow.transform, "UndoButton", "↺ UNDO", "0", new Vector2(0f, 0f), new Vector2(0.22f, 1f));
            var (bombBtn, bombBadge) = CreateActionButton(controlsRow.transform, "BombButton", "💣 BOMB", "2", new Vector2(0.26f, 0f), new Vector2(0.48f, 1f));
            var (shuffleBtn, shuffleBadge) = CreateActionButton(controlsRow.transform, "ShuffleButton", "🔀 SHUFFLE", "2", new Vector2(0.52f, 0f), new Vector2(0.74f, 1f));
            var (restartBtn, _) = CreateActionButton(controlsRow.transform, "RestartButton", "↻ RESET", "", new Vector2(0.78f, 0f), new Vector2(1f, 1f));

            // Modals: Game Over Modal
            var (gameOverModal, goScore, goBest, goRetry, goUndo) = CreateGameOverModal(uiRoot.transform);

            // Modals: Victory Modal
            var (winModal, winScore, winBonus, winStars, winNext) = CreateVictoryModal(uiRoot.transform);

            // UI Manager
            GridPuzzleUIManager uiManager = canvasObj.AddComponent<GridPuzzleUIManager>();
            SerializedObject soUI = new SerializedObject(uiManager);
            soUI.FindProperty("scoreText").objectReferenceValue = scoreText;
            soUI.FindProperty("bestScoreText").objectReferenceValue = bestText;
            soUI.FindProperty("stepsRemainingText").objectReferenceValue = stepsText;
            soUI.FindProperty("stepsRemainingBadge").objectReferenceValue = stepsBadge.GetComponent<Image>();
            soUI.FindProperty("objectiveText").objectReferenceValue = objText;
            soUI.FindProperty("comboBannerText").objectReferenceValue = comboText;

            soUI.FindProperty("undoButton").objectReferenceValue = undoBtn;
            soUI.FindProperty("undoBadgeText").objectReferenceValue = undoBadge;
            soUI.FindProperty("bombButton").objectReferenceValue = bombBtn;
            soUI.FindProperty("bombCountText").objectReferenceValue = bombBadge;
            soUI.FindProperty("shuffleButton").objectReferenceValue = shuffleBtn;
            soUI.FindProperty("shuffleCountText").objectReferenceValue = shuffleBadge;
            soUI.FindProperty("restartButton").objectReferenceValue = restartBtn;

            soUI.FindProperty("gameOverModal").objectReferenceValue = gameOverModal;
            soUI.FindProperty("gameOverScoreText").objectReferenceValue = goScore;
            soUI.FindProperty("gameOverBestText").objectReferenceValue = goBest;
            soUI.FindProperty("gameOverRetryButton").objectReferenceValue = goRetry;
            soUI.FindProperty("gameOverUndoButton").objectReferenceValue = goUndo;

            soUI.FindProperty("winModal").objectReferenceValue = winModal;
            soUI.FindProperty("winScoreText").objectReferenceValue = winScore;
            soUI.FindProperty("winMovesBonusText").objectReferenceValue = winBonus;
            soUI.FindProperty("winStarsText").objectReferenceValue = winStars;
            soUI.FindProperty("winNextButton").objectReferenceValue = winNext;
            soUI.ApplyModifiedProperties();

            // 4. Audio Manager
            GameObject audioObj = new GameObject("AudioManager");
            audioObj.AddComponent<AudioSource>();
            PuzzleAudioManager audioManager = audioObj.AddComponent<PuzzleAudioManager>();

            // 5. Game Controller
            GameObject controllerObj = new GameObject("GameController");
            GridPuzzleGameController controller = controllerObj.AddComponent<GridPuzzleGameController>();

            SerializedObject soCtrl = new SerializedObject(controller);
            soCtrl.FindProperty("boardView").objectReferenceValue = boardView;
            soCtrl.FindProperty("uiManager").objectReferenceValue = uiManager;
            soCtrl.FindProperty("swipeInput").objectReferenceValue = swipeInput;
            soCtrl.FindProperty("audioManager").objectReferenceValue = audioManager;
            soCtrl.FindProperty("config").objectReferenceValue = config;
            soCtrl.ApplyModifiedProperties();

            // Save Scene
            string scenePath = $"{ScenesFolder}/GridPuzzleGame.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        #region UI Generation Helpers

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero;
            return go;
        }

        private static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text, float fontSize, FontStyles style, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static (GameObject badgeGo, TextMeshProUGUI valueTmp) CreateMetricBadge(Transform parent, string name, string label, string defaultValue, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.16f, 0.18f, 0.24f, 1f);

            var labelTmp = CreateTMPText(go.transform, "Label", label, 20f, FontStyles.Bold, new Color(0.6f, 0.65f, 0.75f), new Vector2(0f, 0.55f), new Vector2(1f, 0.95f));
            var valTmp = CreateTMPText(go.transform, "Value", defaultValue, 32f, FontStyles.Bold, Color.white, new Vector2(0f, 0.05f), new Vector2(1f, 0.6f));

            return (go, valTmp);
        }

        private static (Button btn, TextMeshProUGUI badgeTmp) CreateActionButton(Transform parent, string name, string label, string badge, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.22f, 0.26f, 0.35f, 1f);

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.35f, 0.48f, 1f);
            colors.pressedColor = new Color(0.18f, 0.2f, 0.28f, 1f);
            colors.disabledColor = new Color(0.15f, 0.16f, 0.2f, 0.5f);
            btn.colors = colors;

            var labelTmp = CreateTMPText(go.transform, "Label", label, 20f, FontStyles.Bold, Color.white, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f));

            TextMeshProUGUI badgeTmp = null;
            if (!string.IsNullOrEmpty(badge))
            {
                badgeTmp = CreateTMPText(go.transform, "Badge", badge, 16f, FontStyles.Bold, new Color(1f, 0.8f, 0.2f), new Vector2(0.65f, 0.6f), new Vector2(0.95f, 0.95f));
            }

            return (btn, badgeTmp);
        }

        private static (GameObject modalGo, TextMeshProUGUI scoreTmp, TextMeshProUGUI bestTmp, Button retryBtn, Button undoBtn) CreateGameOverModal(Transform parent)
        {
            var modal = CreatePanel(parent, "GameOverModal", Vector2.zero, Vector2.one);
            var bg = modal.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.85f);

            var dialog = CreatePanel(modal.transform, "DialogBox", new Vector2(0.15f, 0.32f), new Vector2(0.85f, 0.68f));
            var dialogImg = dialog.AddComponent<Image>();
            dialogImg.color = new Color(0.14f, 0.16f, 0.22f, 1f);

            CreateTMPText(dialog.transform, "Title", "OUT OF MOVES!", 42f, FontStyles.Bold, new Color(1f, 0.3f, 0.3f), new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.92f));
            var scoreTmp = CreateTMPText(dialog.transform, "Score", "Score: 0", 28f, FontStyles.Normal, Color.white, new Vector2(0.1f, 0.52f), new Vector2(0.9f, 0.68f));
            var bestTmp = CreateTMPText(dialog.transform, "Best", "Best: 0", 24f, FontStyles.Normal, new Color(0.7f, 0.75f, 0.85f), new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.5f));

            var (undoBtn, _) = CreateActionButton(dialog.transform, "UndoBtn", "↺ UNDO MOVE", "", new Vector2(0.1f, 0.18f), new Vector2(0.48f, 0.34f));
            var (retryBtn, _) = CreateActionButton(dialog.transform, "RetryBtn", "↻ TRY AGAIN", "", new Vector2(0.52f, 0.18f), new Vector2(0.9f, 0.34f));

            modal.SetActive(false);
            return (modal, scoreTmp, bestTmp, retryBtn, undoBtn);
        }

        private static (GameObject modalGo, TextMeshProUGUI scoreTmp, TextMeshProUGUI bonusTmp, TextMeshProUGUI starsTmp, Button nextBtn) CreateVictoryModal(Transform parent)
        {
            var modal = CreatePanel(parent, "VictoryModal", Vector2.zero, Vector2.one);
            var bg = modal.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.85f);

            var dialog = CreatePanel(modal.transform, "DialogBox", new Vector2(0.15f, 0.32f), new Vector2(0.85f, 0.68f));
            var dialogImg = dialog.AddComponent<Image>();
            dialogImg.color = new Color(0.14f, 0.16f, 0.22f, 1f);

            CreateTMPText(dialog.transform, "Title", "VICTORY! 🎉", 46f, FontStyles.Bold, new Color(0.3f, 1f, 0.5f), new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.95f));
            var starsTmp = CreateTMPText(dialog.transform, "Stars", "★★★", 40f, FontStyles.Bold, new Color(1f, 0.85f, 0.2f), new Vector2(0.1f, 0.58f), new Vector2(0.9f, 0.72f));
            var scoreTmp = CreateTMPText(dialog.transform, "Score", "Final Score: 0", 28f, FontStyles.Normal, Color.white, new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.56f));
            var bonusTmp = CreateTMPText(dialog.transform, "Bonus", "Steps Bonus: +500", 22f, FontStyles.Normal, new Color(0.4f, 0.8f, 1f), new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.4f));

            var (nextBtn, _) = CreateActionButton(dialog.transform, "NextBtn", "PLAY AGAIN", "", new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.24f));

            modal.SetActive(false);
            return (modal, scoreTmp, bonusTmp, starsTmp, nextBtn);
        }

        private static void CreateEventSystem()
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        #endregion
    }
}
