# ポップアップ画像の再生成

`BuildPopupArtwork.cs` は Unity Pipeline から呼び出す制作ツールです。
Assets外に置き、ゲーム本体のビルドには含めません。

```powershell
unity command run_script --file Tools/Popup/BuildPopupArtwork.cs --entry BuildPopupArtwork.Build --project-path . --format json
```

Unityを停止状態にして実行してください。3枚の画像と既存の共通Prefabを更新します。
`BuildSelected` に `[0]` / `[1]` / `[2]` を渡すと1枚だけ更新できます。

- 画像: `Assets/Game/Popup/Texture/{FirstStage,BossStage,Jump}.png`、1920×1080。
- 素材: `Assets/Game/Popup/Texture/Generated/GameStyle/`。枠・開始ボタン・左ボタンを示すマウス図の3点を個別に組み込みimage_genで生成。タイトルUIを参照。
- プロンプト: 同フォルダーの `GenerationManifest.json`。
- 実ゲーム撮影: `Assets/Game/Popup/Texture/Captures/`。撮影元は `CaptureManifest.json`。
- 日本語フォント: 既存の `LINESeedJP-Bold.ttf`。制作時だけ一時フォントを作り、共有フォントは変更しません。

素材の透明な中央、説明文のはみ出し、撮影画像のサイズ・縦横比の変化を検出した場合は書き出しを中断します。
Sprite GUIDとカタログIDは維持します。元の撮影画像と過去の生成素材は保存したままです。

共通Prefabは `PopupCardPresentation` で1920×1080の比率を保って画面に収めます。
閉じるボタンは画像内の右下に固定され、画像と一緒に拡大縮小します。
表示・ホバー・押下演出は `unscaledDeltaTime` を使い、ゲーム停止中も動きます。

基本操作は入力方法と実物ラベル、ボスは攻撃タイルを交互に使う関係、ジャンプは距離と着地点を図示します。
ジャンプの着地点は実ゲームのグリッド座標から取得。撮影画像はNPOT変換を無効にして案内線の位置を保ちます。
共通ボタンは「はじめる」。既存の停止・閉じる・再開処理とカタログIDは維持しています。

2026-10-29確認: PopupTestで3種類×4解像度（1920×1080、1280×720、1024×768、2560×1080）。
画像とボタンの収まり・16:9維持に加え、各IDを実際に指定したPrefabで、停止中の移動停止、背景クリック遮断、
ホバー・押下演出、マウスでの開始、閉じたフレームの入力遮断を確認。Enterでの開始・再開も確認済み。
初見ユーザーによる理解度テストは別途必要です。

再検証は `VerifyPopupPresentation.cs` の `Prepare` → Play → `Verify` → Stop → `Restore` の順です。
各操作を `unity command run_script --file Tools/Popup/VerifyPopupPresentation.cs --entry VerifyPopupPresentation.メソッド名` で実行します。
`Prepare` を復元前に重複して実行しないでください。
検証は一時的なInput Systemデバイスと設定のコピーを使い、実マウスとの競合とEditorの非フォーカス時のイベント破棄を避けます。
デバイス・入力設定・Game Viewサイズはfinallyで、再生開始シーンとバックグラウンド設定は`Restore`で復元します。
実行結果は `VerificationReport.json`、実行時キャプチャはリポジトリ直下の `output/popup/GameStyle-*.png` にあります。
