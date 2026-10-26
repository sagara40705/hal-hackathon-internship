# ON/OFFスイッチ・切替床

## ステージへの配置

対象シーンの **TileGridの子** に、以下のPrefabを配置する。

| Prefab | 動作 |
| --- | --- |
| `Prefab/TileGroup/Group_OnOffSwitch.prefab` | 踏むたびに盤面のON/OFFを反転。足元は常設床 |
| `Prefab/TileGroup/Group_OnOffFloor_Off.prefab` | OFFで実体化する青床 |
| `Prefab/TileGroup/Group_OnOffFloor_On.prefab` | ONで実体化するオレンジ床 |

1マスの固定TileGroupなので、既存グリッドのマスに合わせて配置する。各マスをTileGridの配置可能範囲に含め、他の床と重ねない。Prefabは1マス=1 Unity単位の既存ステージに合わせたサイズ。

コントローラー参照は自動解決される。同じTileGrid内の全スイッチ・切替床が1つの状態を共有し、別のTileGridへは影響しない。

初期状態はOFF。ONから開始したい場合は、`Prefab/OnOffController.prefab`をTileGridの子へ1つ置き、**Start On**を有効にする。シーンの読み直しで初期状態に戻る。スイッチ・床のController欄は通常空欄のままでよい。

スイッチのTileCellには`OnOffSwitch.OnEnterPlayer(Player)`を設定済み。配置後にUnityEventをつなぐ必要はない。方向転換もさせたい場合は、既存のChangeDirectionを追加してTileCellの進入イベントへ設定する。

## ルール

- OFFでは青床、ONではオレンジ床が通行可能。
- 消えている床は色付きの枠とON/OFF文字で位置を表示する。ColliderとTileGridのセル登録を外すため、Playerが進入すると既存の落下処理になる。
- 消えているマスは配置可能範囲から一時的に外す。可動グループや生成タイルで埋めることはできない。実体化時には元の範囲へ戻してセルを再登録する。
- 足元の床が消えた場合も既存の`Player.Damage()`経由で落下する。ジャンプ中は足元消失で落下させず、既存の着地判定へ任せる。
- タイル移動・敵のタイル破壊の対象にならないよう、TileGroupの固定フラグは有効のまま使う。
- アイテムは通常どおりGridItems側へ配置する。床が消えている間は到着通知が発生しないため取得できない。
- スイッチは滞在中に繰り返し切り替わらず、再進入で再度作動する。

## 確認用シーン

`Assets/Gimmick/OnOffSample.unity`を開いて再生すると、Playerが2つのスイッチを順番に踏み、オレンジ床→青床を渡ってENDで停止する。本編シーンやBuild Settingsには追加していない。

Prefabが存在しない場合は、Unityの **Tools > Gimmick > ON OFF > Create Missing Prefabs** から作成できる。既存Prefab・マテリアルは上書きしない。確認用シーンも同じメニューの **Create Sample Scene** で新規生成できる。

実装・Prefab・マテリアル・生成ツールはすべてAssets/Gimmick内。既存のTileGrid、TileCell、Playerのソース変更は不要。
