# 🛠️ ClamChart 実装プラン

[SPEC.md](SPEC.md) を元にした実装の手順とチェックリスト。チェックボックスで進捗を管理する。  
各項目の末尾の `§` は SPEC.md の該当箇所を表す。  

## 📊 進捗サマリ

| フェーズ | 内容 | 版 | 状態 |
|---|---|:-:|:-:|
| 🧰 Phase 0 | 準備とスパイク（未決事項の検証） | — | ⬜ |
| 🌱 Phase 1 | 基盤、`ClamChartView`（第 2 Y 軸を含む）、`ClamPieChartView`、`ClamSparklineView` | 0.1 | ⬜ |
| 📊 Phase 2 | ダッシュボード部品、重ね合わせ、Grafana 風の表現 | 0.2 | ⬜ |
| 🔭 Phase 3 | 拡張チャート、ClamGrid 連携 | 0.3〜 | ⬜ |

状態: ⬜ 未着手 / 🟨 進行中 / ✅ 完了  

## 🏷️ 絵文字の凡例

| 絵文字 | 種類 | 絵文字 | 種類 |
|:-:|---|:-:|---|
| 🧱 | 基盤・プロジェクト構成 | ✋ | 入力・タッチ |
| 🔗 | データ・バインド・MVVM | 🎬 | アニメーション |
| 📐 | 計算・レイアウト（純粋ロジック） | 🧪 | 単体テスト |
| 🎨 | 描画 | 📱 | サンプルアプリ |
| 🔍 | 検証・計測 | 📖 | ドキュメント |
| 🧭 | 判断・記録（SPEC への反映） | ⚠️ | 注意点 |

---

## 🚦 着手判定

SPEC.md は Phase 1 の実装に必要な範囲（クラス構成・プロパティ・既定値・動作規則）を定義済みで、**Phase 0 のスパイクを終えれば実装を開始できる**。  

| 観点 | 状態 | 補足 |
|---|:-:|---|
| コントロールとプロパティ | ✅ | §4.2、§7 に型・既定値・Bindable・フェーズを記載 |
| データの受け渡し・MVVM | ✅ | §5（アクセサ、変更追跡、双方向プロパティ、イベントと Command） |
| スタイル・パレット | ✅ | §6（`ChartStyle` の項目、プリセット、パレット） |
| アニメーション・入力 | ✅ | §8、§9（無効化、トラックボール、タッチの扱い） |
| 軸範囲・第 2 Y 軸 | ✅ | §7.1.4、§7.1.8 |
| プロジェクト構成・テスト | ✅ | §4.4、§14 |
| 未決事項 1: シリーズ・軸の基底（`Element` か） | 🟨 | Phase 0 のスパイクで確定する |
| 未決事項 2: タッチ透過（`HitOnly`）の実現方法 | 🟨 | Phase 0 のスパイクで確定する。`PassThrough` は確実に実現できる |
| 未決事項 3〜7 | ✅ | 推奨案のまま進めても Phase 1 に影響しない |

---

## 🧭 進め方

- 📏 **1 項目 ≒ 1 コミット、1 セクション ≒ 1 PR** を目安にする。
- 📐 計算・レイアウトは MAUI に依存しない純粋なクラスにし、**テストを先に書く**。
- 📱 機能を追加したら、同じ PR で Example の画面に載せて実機で確認する。
- 🧭 実装中に SPEC と違う判断をしたら、SPEC.md を同じ PR で更新する。

### ✅ 各項目の完了の定義

- [ ] 🧱 ビルドで警告ゼロ（StyleCop、`AnalysisMode All`、`Nullable` の警告はエラー）
- [ ] 🧪 追加したロジックに単体テストがある
- [ ] 📱 Example で動作を確認した（表示・タッチ・アニメーション）
- [ ] 📖 公開 API を追加・変更したら API.md を更新した
- [ ] ⚠️ 新しいテキストファイルは CRLF（AGENTS.md）

---

## 🧰 Phase 0: 準備とスパイク

### 🏗️ 0-1 リポジトリの骨格

clam-gauge の構成をそのまま写す。  

- [ ] 🧱 `ClamChart/ClamChart.csproj` を作成（`net10.0;net10.0-android`、`UseMaui`、`IsTrimmable`、`IsAotCompatible`、Android 30、`*.android.cs` の条件付きコンパイル）§14.1
- [ ] 🧱 パッケージ情報を設定（`PackageId` / `Title` = `ClamChart`、`Description`、`PackageTags`）§14.1
- [ ] 🧱 依存パッケージを追加（`Microsoft.Maui.Controls 10.0.110`、`SkiaSharp 4.153.1`、`SkiaSharp.Views.Maui.Controls 4.153.1`、`SkiaExtension 1.2.0`）§1.4
- [ ] 🧱 `Assembly.cs`（`CLSCompliant(false)`、`InternalsVisibleTo("ClamChart.Tests")`）
- [ ] 🧪 `ClamChart.Tests` を作成（`net10.0`、`OutputType Exe`、xUnit v3、Microsoft Testing Platform、`GlobalUsing.cs`、`GlobalSuppressions.cs`）§14.2
- [ ] 📱 `Example` を作成（`net10.0-android`、`MauiXamlInflator=SourceGen`、`MauiStrictXamlCompilation`、Smart.Mvvm / Smart.Navigation / BunnyTail.DependencyInjection、`ViewId`、メニュー、F1〜F4）§14.3
- [ ] 🧱 `ClamChart.slnx` に Example / Library / Tests を登録、`ClamChart.ExcludeExample.slnx` を作成
- [ ] 🔍 空のライブラリ・テスト・Example がビルドでき、Example が実機で起動する

### 🧪 0-2 スパイク（捨てるコードで検証）

- [ ] 🔍 **タッチ透過**: `SKCanvasView` の `Touch` で `Pressed` に `Handled = false` を返したとき、重ねた下の View（`Border` の `TapGestureRecognizer`、`ScrollView`、`CollectionView`）にタッチが届くか確認 §16.2-2
- [ ] 🔍 **タッチ透過（代替案）**: 届かない場合に `InputTransparent` の切り替えで `HitOnly` 相当を実現できるか確認 §16.2-2
- [ ] 🔍 **シリーズの基底**: `Element` 派生のシリーズを論理子に追加し、`BindingContext` の継承・`x:DataType` のコンパイル済みバインド（SourceGen）・`x:Name` が動くか確認 §16.2-1
- [ ] 🔍 **透明背景とくり抜き**: 透明な `SKCanvasView` で `SKBlendMode.Clear` を使ったとき、下の View が透けて見えるか確認 §6.2
- [ ] 🔍 **アニメーション**: `this.Animate` で 1,000 点 × 3 本の補間描画が 60 fps 出るか計測 §12.1
- [ ] 🔍 **OS のアニメーション設定**: `Settings.Global.ANIMATOR_DURATION_SCALE` を `.android.cs` から読めるか確認 §8.5
- [ ] 🔍 **スクロールの停止**: 長押しで `RequestDisallowInterceptTouchEvent` を有効にし、`ScrollView` 内でトラックボールのドラッグが奪われないか確認 §9.3

### 🧭 0-3 判断の記録

- [ ] 🧭 スパイクの結果を SPEC.md §16 に反映し、未決事項 1・2 を決定事項へ移す
- [ ] 🧭 Phase 1 の PR の切り方（下記の 1-A〜1-J）を確定する

---

## 🌱 Phase 1: 基盤とメインチャート（0.1）

### 🧱 1-A 共通基盤

- [ ] 🧱 `ChartViewBase : SKCanvasView, IDisposable` を作成（公開・抽象、partial で `.Animation.cs` / `.Input.cs` / `.android.cs` に分割）§4.1、§4.4
- [ ] 🧱 `OnPaintSurface` で DIP にスケールし、Renderer に `RenderState` を渡す（Clam 共通のパターン）§4.3
- [ ] 🧱 プロパティ変更のコールバックを 4 種に分ける（Data / Layout / Render / Style）§4.3
- [ ] 🧱 同じ UI ループ内の変更を 1 回の再計算にまとめる（`Dispatcher.Dispatch` とフラグ）§4.3
- [ ] 🧱 ライフサイクル（Handler の付け外し、`Unloaded`、`IsVisible`、`Dispose`、`ObjectDisposedException`）§4.5
- [ ] 🧱 UI スレッド以外からの変更で `InvalidOperationException` §4.5
- [ ] 🧱 共通プロパティ: `ChartStyle`、`Palette`、`Density` / `EffectiveDensity`、`ContentPadding`、`Culture`、`EmptyText` §4.2
- [ ] 🧱 共通メソッド: `Invalidate()`、`PlayEntranceAnimation()`、`CompleteAnimations()`、`CancelInteraction()` §4.2
- [ ] 🧱 共通イベント: `ItemTapped` / `ChartTapped` / `FrameRendered` と対応する Command §4.2、§5.5
- [ ] 🧱 `ClamChartSettings`（`AnimationEnabled`、`AnimationScale`、`RespectSystemAnimationScale`）§8.5
- [ ] 🎨 `ChartStyle` レコード（全項目、検証、`with` での差し替え）§6.3
- [ ] 🎨 `ChartStyles.Light` / `ChartStyles.Dark` §6.5
- [ ] 🎨 `ChartPalettes.Default`（ライト・ダークの 8 色、固定順）§6.4
- [ ] 🎨 `ChartTextFormat`（`Format` / `Converter` / `Formatter` の 3 段、コンパクト表記の既定書式）§5.6
- [ ] 🎨 SkiaExtension の `FontResolver` で文字を描く（`FallbackFonts` による 1 文字ずつの代替）
- [ ] 🎨 `PaintCache`（`SKPaint` / `SKPath` の使い回し）§12.2
- [ ] 📐 `DensityResolver`（`CompactSize` / `MinimalSize` による切り替え）§11.1
- [ ] 🧪 スタイルの検証、密度の境界値、書式の 3 段の優先順位をテスト

### 🔗 1-B データ層

- [ ] 🔗 `ChartPoint` / `ChartTimePoint` / `ChartEntry`（`sealed record`）§5.1
- [ ] 🔗 `IChartAccessorProvider` と `ChartAccessorCollection<T>`（`double` / `double?` / `DateTime` / `DateTimeOffset` / `string?` / `Color?` のゲッター）§5.2
- [ ] 🔗 値の取り出しの判定順（キー → ライブラリのモデル → 数値）と、型の不一致の `InvalidOperationException` §5.1、§5.2
- [ ] 🔗 `ValueSelector` などのデリゲートでの受け渡し §5.2
- [ ] 🔗 `DataSnapshot`（`double[]` への抽出、`ArrayPool` の利用）§4.3、§12.2
- [ ] 🔗 `ChartCollectionWatcher`（Handler がある間だけ購読）§4.5
- [ ] 🔗 `INotifyCollectionChanged` の Add / Remove / Replace / Move / Reset を差分として記録 §5.3
- [ ] 🔗 項目の `INotifyPropertyChanged` の追跡（`TrackItemChanges`、参照等価の集合で購読管理）§5.3
- [ ] 🔗 欠損の扱い（`EmptyPointMode`: `Gap` / `Zero` / `Interpolate`）§5.1
- [ ] 🧪 アクセサの登録と取得、型の不一致、判定順、差分の記録、欠損の扱いをテスト

### 📐 1-C スケールとレイアウト

- [ ] 📐 `NiceScale`（1・2・2.5・5 の倍数、負の値、極小・極大）§7.1.4
- [ ] 📐 `AxisRangeResolver`（`Minimum` / `Maximum` の固定、`SoftMinimum` / `SoftMaximum`、`IncludeZero`、`NiceRange`、上下限の逆転時のフォールバック）§7.1.4
- [ ] 📐 第 2 Y 軸の範囲（`TickAlignment` の `AlignToPrimary` / `Independent`）§7.1.8
- [ ] 📐 `TickGenerator`（数値、日時の間隔の自動選択と書式、カテゴリ）§7.1.4
- [ ] 📐 `LabelThinning`（重なりの検出と間引き、両端の保持、`MaxLabelCount`）§7.1.4
- [ ] 📐 `StackCalculator`（`StackGroup`、`Normal` / `Percent`、正負の混在、非表示シリーズ、軸ごとの積み上げ）§7.1.3
- [ ] 📐 直交座標のレイアウト（プロット領域、左右の Y 軸、X 軸、凡例、`Orientation = Horizontal`）§7.1
- [ ] 📐 凡例のレイアウト（`List`、折り返し、「+N」の省略）§7.1.5
- [ ] 📐 カテゴリの統合（全シリーズの X を出現順に）§7.1.4
- [ ] 📐 ヒットマップ（X の二分探索、拡張したヒット領域）§9.2
- [ ] 🧪 上記すべてをテスト（`AxisRangeResolver` は §14.2 の観点を網羅）

### 🎨 1-D `ClamChartView` の描画

- [ ] 🎨 `ClamChartView` のプロパティ（`Series`、`XAxis` / `YAxis` / `SecondaryYAxis`、`Orientation`、`ValueAccessors`、`XKey`、`StackMode`、凡例、`DataLabelsMode`）§7.1.2
- [ ] 🔗 シリーズ・軸を論理子として追加し、`BindingContext` を継承させる §4.1
- [ ] 🎨 軸（ラベル、タイトル、基線、横のヘアラインのグリッド、`Position`、`IsInversed`）§7.1.4、§6.2
- [ ] 🎨 第 2 Y 軸（右側のラベル、軸タイトル横のシリーズの色マーカー、`Independent` 時の短い目盛り線）§7.1.8
- [ ] 🎨 `LineSeries`（太さ、`Linear` / `Smooth`（単調 3 次）/ `Step`、破線、`MarkerMode`）§7.1.3
- [ ] 🎨 マーカーの周囲のくり抜き（`SKBlendMode.Clear`）§6.2
- [ ] 🎨 `AreaSeries`（`FillOpacity`、`Solid` / `Gradient`、`BaselineValue`、積み上げ）§7.1.3
- [ ] 🎨 `BarSeries`（データ側だけの角丸、最大幅、集合、積み上げの隙間、負の値の色）§7.1.3
- [ ] 🎨 棒のある Y 軸は 0 を含める §7.1.3
- [ ] 🎨 データラベル（`Last` / `MinMax` / `All`、収まらなければ描かない）§7.1.5
- [ ] 🎨 凡例（`List`、2 本以上かつ `Full` で自動表示）§7.1.2
- [ ] 🎨 密度による表示の切り替え（`Full` / `Compact` / `Minimal`）§7.1.7
- [ ] 🎨 パレットの割り当て（シリーズの位置で固定、9 本目以降は `OtherColor`）§6.4
- [ ] 🎨 空データ（`EmptyText`、既定は何も描かない）と透明背景 §11.3、§11.4
- [ ] 🧪 スモーク描画とピクセル検査（透明背景、くり抜きの透明、棒の角丸の向き）§14.2

### ✋ 1-E 入力

- [ ] ✋ `ChartGestureController`（タップ、長押し 500 ms / 8 DIP、縦方向の譲渡、2 本目の指での取り消し）§9.2、§9.3
- [ ] ✋ プラットフォームブリッジ（`RequestDisallowInterceptTouchEvent`、フォーカス喪失時の取り消し）§9.7
- [ ] ✋ `TouchMode`（`Auto` / `Interactive` / `HitOnly` / `PassThrough`）と `Auto` の判定 §9.1
- [ ] ✋ タップ（ヒット判定、`ItemTapped` / `ChartTapped`、`Handled` での抑止）§9.2、§5.5
- [ ] ✋ 選択（`SelectionMode`: `None` / `Item` / `Category`、`SelectedItem` / `SelectedIndex` / `SelectedSeries` の TwoWay）§7.1.2
- [ ] ✋ トラックボール（`LongPress` / `Touch`、吸着、縦線、両軸のマーカー、ツールチップ）§9.3
- [ ] 🔗 `TrackedItem` / `TrackedIndex`（OneWayToSource）と `Tracking*` イベント §5.4
- [ ] ✋ 凡例タップでの表示切り替え（`LegendToggleEnabled`、最後の 1 本は隠さない）§9.5
- [ ] ✋ 押下の表示（`Minimal` でチャート全体がボタンのとき）§9.2
- [ ] ✋ 取り消しの条件（サイズ変更、`Unloaded`、`IsEnabled` / `IsVisible`、Handler の変更、`Dispose`）
- [ ] 🧪 ジェスチャーの状態遷移を時刻を明示してテスト、ヒット判定をテスト

### 🎬 1-F アニメーション

- [ ] 🎬 `ChartAnimator`（View ごとに 1 本の `Animate`、名前定数、進捗 0〜1）§8.6
- [ ] 🎬 `SnapshotInterpolator`（値のモーフ、末尾追加・先頭削除の流れ、途中の追加・削除、軸範囲の補間）§8.3
- [ ] 🎬 途中で次の変更が来たら表示中の状態から補間し直す §8.3
- [ ] 🎬 登場アニメーション（`Auto` / `None` / `Fade` / `Grow` / `Reveal`、要素ごとのずらし `ElementProgress`）§8.2
- [ ] 🎬 選択・強調（減光、マーカー、150 ms）§8.4
- [ ] 🎬 シリーズの表示切り替えのフェード・縮小 §8.3
- [ ] 🎬 無効化（`AnimationEnabled`、`AnimationDuration = 0`、`ClamChartSettings`、OS 設定）と無効時の静的な描画 §8.5
- [ ] 🎬 ライフサイクルでの停止（`AbortAnimation` と最終状態への同期）§8.6
- [ ] 🧪 補間の結果、途中からの再補間、無効時に `Animate` を呼ばないことをテスト（付録 B の教訓）

### 🥧 1-G `ClamPieChartView`

- [ ] 📐 `PieLayout`（角度、`StartAngle` / `SweepAngle`、隙間、`MaxSlices` / `OthersThreshold` による「その他」、並べ替えと色の対応）§7.2.2
- [ ] 🎨 円とドーナツ（`InnerRadius`、既定 0.6）、半円 §7.2.1、§16.1
- [ ] 🎨 ラベル（`Inside`、`LabelContent`）と凡例（`LegendValueMode`）§7.2.2
- [ ] 🎨 中央の値（`CenterValueMode`、`CenterLabel`、内径に収まるよう縮小）§7.2.2
- [ ] ✋ スライスの選択と `ExplodeOffset`、中央・外側のタップで解除 §7.2.2
- [ ] 🎬 登場（`StartAngle` から広がる）、割合の補間、選択時の切り離し §8.2〜§8.4
- [ ] 🧪 `PieLayout` とヒット判定（角度）をテスト

### 〰️ 1-H `ClamSparklineView`

- [ ] 🎨 `Line` / `Area` / `Bar` / `WinLoss` §7.3.2
- [ ] 🎨 マーカー（`First` / `Last` / `High` / `Low`）、`ReferenceValue`、`Minimum` / `Maximum` §7.3.2
- [ ] 🔗 `Values`（`IReadOnlyList<double>` / `double[]` はコピーしない）と `ItemsSource` + アクセサ §7.3.2
- [ ] 🔍 描画 1 回あたりの割り当てゼロを確認 §11.2、§12.1
- [ ] 🎬 既定で登場アニメーションなし、更新アニメーションあり §7.3.2
- [ ] 🧪 スモーク描画、`Minimum` / `Maximum` による尺度の固定をテスト

### 📱 1-I サンプルアプリ

- [ ] 📱 `ChartMenu`（メニュー）§14.3
- [ ] 📱 `ChartLine`（線・面・スムーズ・ステップ、トラックボール、スクラブ表示）
- [ ] 📱 `ChartBar`（集合・積み上げ・100%・横棒ランキング、選択）
- [ ] 📱 `ChartTimeSeries`（日時軸、1D / 1W / 1M の範囲切り替え）
- [ ] 📱 `ChartDualAxis`（湿度を面で右軸、気温を線で左軸、`AlignToPrimary` / `Independent` の切り替え、上下限の固定・Soft）
- [ ] 📱 `ChartPie`（円・ドーナツ・半円、選択と中央値）
- [ ] 📱 `ChartSparkline`（`CollectionView` の行）
- [ ] 📱 `ChartDensity`（サイズを変えたときの密度の変化）
- [ ] 📱 ライト／ダークの切り替え（暗黙スタイル `ApplyToDerivedTypes`）§6.5

### 📖 1-J ドキュメント

- [ ] 📖 README（Clam 共通の見出し: What is this / Quick Start / Binding and MVVM / Supported features / API / Clam family）
- [ ] 📖 API.md（プロパティ・メソッド・イベント・スタイル・モデルの表）
- [ ] 📖 スクリーンショット（Document/*.png）
- [ ] 📖 ClamCalendar / ClamGrid / ClamGauge の README の「Clam family」に ClamChart を追加（各リポジトリで別 PR）

### 🏁 1-K Phase 1 の完了条件

- [ ] 🔍 §12.1 の目標値（ダッシュボード以外）を実機で計測して満たす
- [ ] 🔍 トリミングを有効にした Example の発行で、トリミング・AOT 警告ゼロ
- [ ] 🧪 `Layout/` / `Input/` / `Data/` / `Animation/` のテストがすべて通る
- [ ] 🧱 `Directory.Build.props` の `Version` を `0.1.0` にする
- [ ] 📖 README / API.md が Phase 1 の API と一致している

---

## 📊 Phase 2: ダッシュボード部品と重ね合わせ（0.2）

### 🔢 2-A `ClamStatView`

- [ ] 🎨 タイトル・値・単位・差分・傾向線（自動縮小する値の文字）§7.4
- [ ] 🎨 差分の色（方向 × `IncreaseIsGood`）と状態アイコン（ベクター）§7.4.2
- [ ] 🎨 `ColorMode`（`None` / `Indicator` / `Background` / `BackgroundGradient`）、`Fill` §7.4.2
- [ ] 🎨 `Layout`（`Auto` / `Vertical` / `Horizontal`）、`TrendPlacement`（`Bottom` / `Background` / `Right`）§7.4.2
- [ ] 🎬 カウントアップ §8.1
- [ ] 📱 Example（KPI タイル）

### 📶 2-B `ClamBarGaugeView`

- [ ] 🎨 `Basic` / `Gradient` / `Segmented`（LCD: セルごとに閾値の色、未到達は 25%）§7.5
- [ ] 🎨 ラベルの位置、値の表示（`Value` / `Percent` / `ValueOfMaximum`）、目標線 §7.5.2
- [ ] 🎨 並べ替え・上位 N 件 §7.5.2
- [ ] 📐 `AutoHeight`（項目数からの高さの計測）§7.5.2
- [ ] 🎬 0 から伸びる、項目ごとに 40 ms ずらす §8.2

### ⭕ 2-C `ClamRadialBarView`

- [ ] 🎨 同心円のリング、丸い端、同系色のトラック、区切り表示、`Wrap` §7.6
- [ ] 🎨 中央のテキストと凡例 §7.6.2
- [ ] ✋ リングの選択 §7.6.2

### 🎯 2-D 閾値・注釈・散布図

- [ ] 🔗 `ChartThreshold`（基準色＋段階、`Level`）§6.6
- [ ] 🎨 `ThresholdsMode`（`Lines` / `Bands` / `ColorSeries`。塗り分けは硬い境目のグラデーションのシェーダー）§7.1.2、§10
- [ ] 🎨 `ChartReferenceLine` / `ChartReferenceBand`（`Value` のバインド）§7.1.6
- [ ] 🎨 `ScatterSeries` §7.1.3
- [ ] 🎨 `ClamSparklineView` の閾値・`EmphasisMode`・正常範囲 §7.3.2

### 📈 2-E Grafana 風の表現

- [ ] 🎨 凡例の表（`LegendMode = Table`、`LegendValues`、`LegendWidth`、狭いときは隠す）§7.1.2、§7.1.5
- [ ] 📐 時間窓（`DateTimeAxis.WindowSize` / `EndTime`、右寄せ）§7.1.4
- [ ] 🎨 `ChartPalettes.Grafana` / `ChartPalettes.Shades`（段階の補間、循環させない）§6.4
- [ ] 📱 Example: template-avalonia-linux の Performance 画面に近いパネル構成で確認 §13.4

### 🪟 2-F ウィジェット・重ね合わせ

- [ ] 🎨 `ChartStyles.OverlayLight` / `OverlayDark`（縁取り文字、グリッドなし）§6.5
- [ ] 🎨 `EdgeToEdge`、`ContentPadding` による前景の回避 §11.3
- [ ] ✋ `HitOnly` の実装（Phase 0 で決めた方式）§9.1
- [ ] 🎨 `IsBusy` のスケルトン §11.4
- [ ] 🔗 `TrackingGroup`（弱参照の登録、同じ Window 内で同期）§9.4
- [ ] 🎨 `CategoryWidth`（横 `ScrollView` 用）§7.1.4
- [ ] 📐 `Downsampler`（LTTB）§12.2
- [ ] 🎨 `RenderToImage` / `SaveAsPngAsync` §11.5
- [ ] 🎨 `ChartDrawing.DrawSparkline`（MAUI 非依存）§11.5
- [ ] ✋ 触覚（`HapticFeedbackEnabled`）§9.2、§9.3
- [ ] 🔗 アクセシビリティの自動説明（`AutomationSummaryFormatter`）§9.8

### ✨ 2-G エフェクト（すべてオプトイン）

- [ ] 🎨 線の光彩（`GlowRadius`、`Minimal` と 500 点超では無効）§10
- [ ] 🎬 ライブ表示の波紋（`LiveIndicatorVisible`）§10
- [ ] 🎬 警告の脈動（`AlertAnimationEnabled`）§10
- [ ] 🎬 無効化時は静的な表示になることを確認 §8.5

### 📱 2-H サンプルと検証

- [ ] 📱 `ChartLive`（ストリーミング、波紋）
- [ ] 📱 `ChartDashboard`（KPI・ラジアルバー・円・推移・バーゲージ、ライト／ダーク）§11.7
- [ ] 📱 `ChartOverlay`（カード背景、画像の上、タッチ透過）§11.3
- [ ] 🔍 template-maui の Kit / Money / Network Realtime 画面を ClamChart に置き換えて検証 §13.3
- [ ] 🔍 ダッシュボードの目標値（§12.1）を満たす
- [ ] 🧱 `Version` を `0.2.0` にする、README / API.md を更新

---

## 🔭 Phase 3: 拡張（0.3〜）

- [ ] 🕸️ `ClamRadarChartView` §7.7
- [ ] 🟩 `ClamHeatmapView`（行列・カレンダー）§7.7
- [ ] 🫧 `BubbleSeries` §7.7
- [ ] 🌡️ `RangeBarSeries`（最低・最高）§7.7
- [ ] 🧭 State timeline・ツリーマップを作るか判断 §7.7
- [ ] 🧭 ClamGrid のセル描画フックとスパークライン列の設計（ClamGrid 側の変更）§11.6
- [ ] 🎨 `ChartDrawing.DrawRadialBar` / `DrawBarGauge` §11.5

---

## ⚠️ リスクと対策

| リスク | 影響 | 対策 | 確認する時期 |
|---|---|---|---|
| 🖐️ タッチ透過が MAUI のラッパー View で下に届かない | 重ね合わせ（`HitOnly`）が実現できない | Phase 0 で検証。だめなら `InputTransparent` の切り替え、最悪 `PassThrough` と `Interactive` の 2 択に絞る | Phase 0 |
| 🧩 `Element` 派生のシリーズで SourceGen のコンパイル済みバインドが効かない | XAML で `{Binding}` が書けない、トリミング警告 | Phase 0 で検証。だめなら `BindableObject` ＋ 手動の `BindingContext` 伝播 | Phase 0 |
| 🐢 補間描画が重い（点数が多い・同時に多数のチャート） | 60 fps を割る | 配列の使い回し、`SKPath.Rewind`、LTTB、画面外では描かない。Phase 1 の途中で計測 | Phase 0・1 |
| 📏 第 2 Y 軸の `AlignToPrimary` で端数のラベルになる | 読みにくい | 自動範囲は切りの良い間隔まで広げる。上下限を固定した場合は `Independent` を推奨（SPEC §7.1.8） | Phase 1 |
| 🎨 Grafana 風の鮮やかな表現と「主張しない」既定の両立 | 既定の見た目がぶれる | 鮮やかな表現はすべてオプトイン（`BackgroundGradient`、`Grafana` パレット） | Phase 2 |
| 🔤 日本語などの文字の欠け | ラベルが表示されない | SkiaExtension の `FallbackFonts` を使い、Example で日本語のラベルを確認 | Phase 1 |
