# ClamChart API reference

## 📖 ChartViewBase

`ClamChartView`, `ClamPieChartView`, `ClamSparklineView`, `ClamStatView`, `ClamBarGaugeView` and `ClamRadialBarView` derive from `ChartViewBase`, which derives from `SKCanvasView`.  
Each view draws everything on its own canvas, has no child views and paints no background unless `BackgroundColor` is set.  
Sizes, thicknesses and font sizes are DIP, and angles are degrees clockwise from 12 o'clock.  
Invalid values throw when assigned: `ArgumentNullException` for a missing required value and `ArgumentOutOfRangeException` for a value out of range (sizes must be finite and zero or more, values and angles must be finite).  
Properties and collections must be changed on the UI thread, otherwise `InvalidOperationException` is thrown.  
Changes made in the same UI loop are processed together before the next frame.  
Changes after `Dispose()` are ignored and the public methods throw `ObjectDisposedException`.  

### 🧩 Properties

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `ChartStyle` | `ChartStyle` | `ChartStyles.Light` | ✓ | Look of everything except the data, validated when assigned (see [ChartStyle](#-chartstyle)) |
| `Palette` | `IReadOnlyList<Color>?` | `null` | ✓ | Series colors; `null` uses `ChartStyle.Palette` |
| `Density` | `ChartDensity` | `Auto` | ✓ | Level of detail; `Auto` follows the size of the view (see [Density](#-density)) |
| `EffectiveDensity` | `ChartDensity` | | | Density in use after `Auto` is resolved |
| `ContentPadding` | `Thickness?` | `null` | ✓ | Space around the content; `null` is 8 for `Full`, 4 for `Compact` and 0 for `Minimal` |
| `Culture` | `CultureInfo?` | `null` | ✓ | Culture of numbers and dates; `null` uses `CultureInfo.CurrentCulture` |
| `EmptyText` | `string?` | `null` | ✓ | Text drawn in the center when there is no data; `null` draws nothing |
| `TouchMode` | `ChartTouchMode` | `Auto` | ✓ | How touches are taken (see [Touch](#-touch)) |
| `IsBusy` | `bool` | `false` | ✓ | Draws a shimmering placeholder in the shape of the chart instead of the data and lets every touch through (see [Loading](#-loading)) |
| `HapticFeedbackEnabled` | `bool` | `false` | ✓ | A light vibration when a tap selects an element and a tick when the trackball starts or moves to another point (Android) |
| `AutomationSummaryFormatter` | `Func<ChartViewBase, string?>?` | `null` | | Description read by screen readers; `null` uses the built-in summary (see [Accessibility](#-accessibility)) |
| `AnimationEnabled` | `bool` | `true` | ✓ | `false` turns every animation off and always draws the final state |
| `AnimationDuration` | `int` | `450` | ✓ | Milliseconds of the entrance and update animations; `0` turns every animation off |
| `AnimationEasing` | `Easing` | `CubicOut` | ✓ | Easing of the entrance and update animations |
| `EntranceAnimation` | `ChartEntranceAnimation` | `Auto` | ✓ | Animation when the data first appears |
| `UpdateAnimationEnabled` | `bool` | `true` | ✓ | Animates data updates and axis range changes; legend toggles and selection still animate when it is `false` |
| `ItemTappedCommand` | `ICommand?` | `null` | ✓ | Executed after `ItemTapped` with the same arguments |
| `ChartTappedCommand` | `ICommand?` | `null` | ✓ | Executed after `ChartTapped` with the same arguments |

The entrance animation plays when the view has a handler, is visible and its data first becomes non-empty, and again when `ItemsSource` is replaced with a different number of points or `PlayEntranceAnimation()` is called.  
`Auto` reveals lines and areas from the start of the X axis, grows bars from the baseline with a small stagger, widens pies from `StartAngle` and plays nothing on a sparkline.  
Data labels, markers and the center value of a pie fade in during the last 30% of the entrance.  
Selection fades unselected elements over 150 ms.  
Every duration is multiplied by `ClamChartSettings.AnimationScale` and the animation scale of the system (see [ClamChartSettings](#-clamchartsettings)).  
Looping effects (the loading shimmer, the rings of `LiveIndicatorVisible` and the pulse of `AlertAnimationEnabled`) run only while they are shown, and stop while the view is hidden, unloaded or in the background and, for the rings and the pulse, while it is busy; with the animations off they are drawn still.  

### 🛠 Methods

| Name | Returns | Description |
|---|---|---|
| `Invalidate()` | `void` | Validates `ChartStyle` again, recreates the renderer and redraws; call it after changing the assigned style in place |
| `PlayEntranceAnimation()` | `void` | Plays the entrance animation again, for example when a tab is shown again |
| `CompleteAnimations()` | `void` | Moves the running animations to their final state |
| `CancelInteraction()` | `void` | Cancels a press, a long press or the trackball in progress |
| `RenderToImage(Size size, float scale)` | `SKImage` | Draws the chart at `size` (DIP) on a new image of `size` × `scale` pixels, without a handler too (see [Images](#-images)) |
| `SaveAsPngAsync(Stream stream, Size size, float scale)` | `Task` | Writes `RenderToImage` to a stream as PNG |
| `Dispose()` | `void` | Stops the animations and the input and releases the subscriptions and native resources |

### 📣 Events

| Name | EventArgs | Description |
|---|---|---|
| `ItemTapped` | `ChartItemEventArgs` | A data element (point, bar or slice) was tapped |
| `ChartTapped` | `ChartTappedEventArgs` | The chart was tapped outside the data elements; in `Minimal` density the whole chart is one button |
| `FrameRendered` | `ChartFrameEventArgs` | Diagnostics after each frame: drawing time, number of points and whether an animation is running |

An event is raised first, then the default action (selection, legend toggle, clearing the selection) and then the command, so the command sees the new state.  
Set `Handled = true` in the event to skip the default action and the command.  
The arguments are created only when the event has a handler or the command is set.  
`PaintSurface` of `SKCanvasView` is raised after the chart is drawn, with the canvas in pixels.  

### ✋ Touch

| `TouchMode` | Behavior |
|---|---|
| `Auto` | Chosen from the settings, as described below |
| `Interactive` | Takes every touch in the view |
| `HitOnly` | Takes the touches that start on a data element or a legend item in use, and passes the others to the views below |
| `PassThrough` | Takes no touches |

`Auto` is `Interactive` when a trackball, `ChartTapped` or `ChartTappedCommand` is set, `HitOnly` when selection, `ItemTapped`, `ItemTappedCommand`, `LegendToggleEnabled`, `LegendItemTapped` or `LegendItemTappedCommand` is set, and `PassThrough` otherwise.  
A chart with no touch settings therefore never takes touches from a card, a scroll view or a map below it.  
In `Minimal` density only whole-chart taps are taken: `Interactive` when `ChartTapped` or `ChartTappedCommand` is set, otherwise `PassThrough`, and the chart is covered with `ChartStyle.PressedOverlayColor` while it is pressed.  
A touch that moves 6 DIP or more is not a tap and a second finger cancels the touch.  

### 📐 Density

| Density | Default condition | Content |
|---|---|---|
| `Full` | Wider than 320 DIP and taller than 200 DIP | Axes, axis titles, legend, data labels, selection and trackball |
| `Compact` | 320 DIP wide or less, or 200 DIP high or less | Up to 3 labels on the value axis and the two ends of the X axis, no axis titles, a legend only when `LegendVisible` is `true`, data labels up to `Last` |
| `Minimal` | 160 DIP wide or less, or 96 DIP high or less | Data only, the last point marker and a whole-chart tap; a pie keeps the donut and the center value |

The boundaries are `ChartStyle.CompactSize` and `ChartStyle.MinimalSize`.  
Explicit settings such as `MaxLabelCount` and `LegendVisible = true` take precedence over the density.  
A sparkline is always `Minimal`.  
A bar gauge that measures its own height (`AutoHeight`) decides the density from the width only.  

### ⏳ Loading

While `IsBusy` is `true`, the view draws a placeholder in `SkeletonColor` instead of the data: a line, bars, a ring, rows or the blocks of a tile, following the kind of the chart.  
A band of `SkeletonHighlightColor` sweeps across it every 1.6 seconds; with the animations off it stays still.  
Touches pass through while busy, and the entrance animation plays when `IsBusy` returns to `false`.  

### ♿ Accessibility

When `SemanticProperties.Description` is not set by the application, the view sets it to a summary of its data after each layout, and keeps a description set by the application.  
The summary is in Japanese when `Culture` (or `CultureInfo.CurrentUICulture`) is Japanese and in English otherwise, with values in the formats of the chart.  

| View | Summary |
|---|---|
| `ClamChartView` | For each visible series: title, number of points, maximum and minimum with the X labels (`Sales: 12 points, max 120 (Mar), min 40 (Aug)`) |
| `ClamPieChartView` | Number of slices, total and the percentage of each slice |
| `ClamSparklineView` | Number of points, last, maximum and minimum |
| `ClamStatView` | Title, value with the unit and the difference with its caption |
| `ClamBarGaugeView`, `ClamRadialBarView` | Label and value of each row |

`AutomationSummaryFormatter` replaces the summary with text from the application; return `null` for no description.  
Set `AutomationProperties.IsInAccessibleTree="False"` on a chart that only decorates a card whose labels already describe it.  

### 🖼 Images

`RenderToImage` lays the chart out at the given size, chooses the density for that size and draws the final state without animations, the trackball or the pressed cover; it works without a handler, for example in a background page.  
A busy view draws a still placeholder.  
The view draws itself at its own size again afterwards, without animating from the image layout.  
`SaveAsPngAsync` encodes the image as PNG and writes it to the stream.  

## 📈 ClamChartView

Draws line, area, bar and scatter series on X and Y axes, mixed in one chart, stacked, horizontal or on a date and time axis, with thresholds and reference lines and bands.  
`Series` is the content property, so series are written as child elements in XAML.  

### 🧩 Properties

#### Series and data

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `Series` | `IList<ChartSeries>` | empty `ObservableCollection<ChartSeries>` | ✓ | Series drawn in order; add items as XAML elements or replace the list, and an `INotifyCollectionChanged` list updates the chart on change |
| `ValueAccessors` | `IChartAccessorProvider?` | `null` | ✓ | Accessors shared by all series (see [Data](#-data)) |
| `XKey` | `string?` | `null` | ✓ | X key shared by all series |

#### Axes

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `XAxis` | `ChartAxis?` | `null` | ✓ | X axis; `null` chooses one from the X values: `CategoryAxis` for text, `DateTimeAxis` for dates and `NumericAxis` without widened ends for numbers and positions |
| `YAxis` | `NumericAxis?` | `null` | ✓ | Primary value axis; `null` is an automatic `NumericAxis` |
| `SecondaryYAxis` | `NumericAxis?` | `null` | ✓ | Value axis on the opposite side for the series with `YAxis = Secondary`; created automatically when such a series exists |
| `Orientation` | `ChartOrientation` | `Vertical` | ✓ | `Horizontal` swaps X and Y for horizontal bars and rankings, with the first X at the top |
| `StackMode` | `ChartStackMode` | `Normal` | ✓ | `Percent` scales each stack to 100% |

Dates and numbers cannot share one X axis; a `CategoryAxis` takes any X in order of position.  

#### Legend and data labels

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `LegendVisible` | `bool?` | `null` | ✓ | `null` shows the legend for two or more series in `Full` density |
| `LegendPosition` | `ChartLegendPosition` | `Bottom` | ✓ | Rows at the top or the bottom, or a column on the left or the right |
| `LegendMode` | `ChartLegendMode` | `List` | ✓ | `List` lines up markers and names; `Table` is a table of names and values like the legend table of Grafana |
| `LegendValues` | `ChartLegendValues` | `None` | ✓ | Value columns of the `Table`: `Last` (headed `Last *`, the last valid value), `Min`, `Max` and `Mean` in this order |
| `LegendWidth` | `double` | `180` | ✓ | Width of a legend on the left or the right: the width of a `Table` and the largest width of a `List`; greater than 0 |
| `Downsampling` | `ChartDownsampling` | `Auto` | ✓ | `Auto` draws lines and areas with sorted X through at most two points per DIP of their width, chosen by LTTB; the data, taps and the trackball keep every point |
| `LegendToggleEnabled` | `bool` | `false` | ✓ | Tapping a legend item shows or hides its series; the last visible series cannot be hidden |
| `LegendSortEnabled` | `bool` | `false` | ✓ | Tapping a header of a `Table` sorts the rows as in Grafana: descending, then ascending, then unsorted, and another column starts descending |
| `LegendSortBy` | `ChartLegendSortKey` | `Name` | ✓ | Column the `Table` is sorted by; a value column not in `LegendValues` keeps the order; TwoWay |
| `LegendSortMode` | `ChartSortMode` | `None` | ✓ | Direction of the sort; TwoWay |
| `LegendItemTappedCommand` | `ICommand?` | `null` | ✓ | Executed after `LegendItemTapped` |
| `DataLabelsMode` | `ChartDataLabelsMode` | `None` | ✓ | Points that get a value label |

The legend lists the series with a `Title`, hidden ones included, each with a dot or a short line for line series.  
Rows wrap and more than two rows end with `+N`.  
A column on the left or the right is as wide as the longest item up to `LegendWidth` and is hidden when the plot would be narrower than twice its width.  

A `Table` has a header row (`Name` and the value columns in `SecondaryTextColor`, with an arrow beside the sorted column) and a row per series with its marker, its name truncated with an ellipsis and its values right-aligned, with a hairline in `GridLineColor` under the header and each row.  
Values are computed from the valid points in the displayed X range, formatted like the tooltip, and left empty when there is no point.  
At the top or the bottom the table takes up to 35% of the height; on the left or the right it is `LegendWidth` wide and as high as the plot.  
Rows that do not fit end with `+N`, and value columns that would leave the names less than 55 DIP are left out from the right.  
Tapping a row works like tapping a `List` item; a hidden series keeps its values with a faded marker and a muted name.  
Sorting orders only the rows of the table: names compare numbers by value and ignore case and accents, rows without a value count as the smallest, and equal values keep the series order.  

Data labels are placed as follows and dropped when they do not fit or overlap a label placed before them (last point, maximum, minimum, then the others).  

| Element | Placement |
|---|---|
| Bar | Outside the data end, or inside it when there is no room outside |
| Stacked bar | Center of the segment |
| Line and area | Above the point (right of it in a horizontal chart), or below when there is no room above |

Labels inside bars use white or `#0B0B0B`, whichever has the higher WCAG contrast with the bar, and labels outside use `SecondaryTextColor`.  
100% stacks show percentages and the other series show their values.  

#### Selection

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `SelectionMode` | `ChartSelectionMode` | `None` | ✓ | `Item` selects one point or bar, `Category` the column of one X; `None` clears the selection |
| `SelectedItem` | `object?` | `null` | ✓ | Item of the selected point, TwoWay |
| `SelectedIndex` | `int` | `-1` | ✓ | Position of the selected point in the data of its series, TwoWay |
| `SelectedSeries` | `ChartSeries?` | `null` | ✓ | Series of an `Item` selection (`null` for `Category`), TwoWay |
| `SelectionChangedCommand` | `ICommand?` | `null` | ✓ | Executed after `SelectionChanged` |

Tapping the selected element again (the same column for `Category`) clears the selection, and so does a tap outside the elements in `Interactive` mode unless `ChartTapped` is handled.  
An item set from the view model selects its first point, visible series first; an item not in the data yet is selected when it arrives, and the selection is cleared when the item leaves the data.  
Setting only `SelectedIndex` selects that position in `SelectedSeries`, or in the first visible series when it is `null`.  
In `Category` selection, `SelectedItem` and `SelectedIndex` are the point of the first visible series at that X.  
The other elements are dimmed to `ChartStyle.DimmedOpacity`: the other bars and the other line and area series for `Item`, and the other columns of bars for `Category`, which marks the points of lines and areas at that X instead.  
Labels inside a dimmed bar are dimmed with it.  

Taps hit wider areas than the drawn shapes.  

| Element | Hit area |
|---|---|
| Scatter point | 22 DIP from the point in any direction, tested before the other elements; the nearest point wins |
| Line and area point | Halfway to the neighboring points along X and 22 DIP from the point along Y, the fill included; the nearer point wins |
| Bar | The whole column of the category, split evenly between grouped bars |
| `Category` selection | The whole column of the category |

#### Trackball

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `TrackballMode` | `ChartTrackballMode` | `None` | ✓ | How the trackball starts |
| `TrackballSnapEnabled` | `bool` | `true` | ✓ | `false` draws the line and a horizontal line at the finger, like the cursor of Grafana, while the markers, the tooltip and `TrackedItem` keep the nearest point |
| `TooltipVisible` | `bool` | `true` | ✓ | Draws the tooltip; `false` keeps only the line and the markers, for showing the values elsewhere through `TrackedItem` |
| `TooltipFormatter` | `Func<ChartTrackingInfo, string>?` | `null` | | Text of each series row after its colored dot; `null` is the title and the formatted value; called once per point until the layout changes |
| `TrackedItem` | `object?` | `null` | ✓ | Item at the trackball (the first series wins at the same X), OneWayToSource; `null` when released |
| `TrackedIndex` | `int` | `-1` | ✓ | Position of that item in the data of its series, OneWayToSource |
| `TrackingChangedCommand` | `ICommand?` | `null` | ✓ | Executed when tracking starts, moves to another point and ends |
| `TrackingGroup` | `string?` | `null` | ✓ | Charts in the same window with the same group show the trackball line and markers of the chart being tracked |
| `TrackingGroupMode` | `ChartTrackingGroupMode` | `Crosshair` | ✓ | How this chart follows another chart of its group: `Crosshair` draws the line and the markers, `Tooltip` adds its tooltip, like the shared crosshair and the shared tooltip of a Grafana dashboard |

| `TrackballMode` | Start | Suits |
|---|---|---|
| `LongPress` | Pressing 500 ms within 8 DIP; the parent stops scrolling | A chart in a `ScrollView` or a `CollectionView` |
| `Touch` | At once; a first move of 8 DIP that is at least 1.25 times more vertical than horizontal goes to the parent scroll | A screen where the chart is the main content |

The trackball snaps to the X of the nearest point of all visible series, and each series shows its point at that X.  
It draws a hairline in `TrackballLineColor`, markers on line and area points and, when `TooltipVisible` is `true`, a tooltip 48 DIP above the finger (below it when there is no room) kept inside the view.  
The first line of the tooltip is the X label.  
On a `DateTimeAxis` with the default format it uses the finer of the tick precision and the precision of the point times, with the date added when the range spans days (`10/9 14:00`).  
The following rows show the title and the value of each series, formatted by the series or the value axis; 100% stacks show the original values.  
When every visible series is a `ScatterSeries`, the trackball snaps to the nearest point in both directions instead, draws a crosshair through it and shows only that point in the tooltip.  
With `TrackballSnapEnabled` set to `false` the lines stay at the finger, kept inside the plot, and a scatter crosshair does too.  
A data or layout change during tracking snaps again at the finger.  
The other charts of a `TrackingGroup` snap to their own point at the same X (the same category name on a category axis) and draw the line and the markers without `TrackedItem` or events; nothing is shown when that X is outside their range, and charts of only scatter series do not follow.  
A chart with `TrackingGroupMode` set to `Tooltip` also shows its tooltip, placed at the height of the finger on the tracked chart in proportion to its plot.  
An unsnapped chart shares the X of the finger, and a following chart with `TrackballSnapEnabled` set to `false` draws its line there, without the horizontal line.  
Tracking ends when the finger is released, a second finger touches, the finger leaves the view or `CancelInteraction()` is called, and is canceled when the view is resized, unloaded, disabled, hidden or loses focus, the handler changes, or `TouchMode` or `TrackballMode` changes.  

#### Thresholds and annotations

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `Thresholds` | `IList<ChartThreshold>` | empty `ObservableCollection<ChartThreshold>` | ✓ | Thresholds compared with the values on the primary value axis, not included in the automatic range; `null` throws `ArgumentNullException` |
| `ThresholdsMode` | `ChartThresholdsMode` | `None` | ✓ | How the thresholds are shown, as flags |
| `Annotations` | `IList<ChartAnnotation>` | empty `ObservableCollection<ChartAnnotation>` | ✓ | Reference lines and bands (see [Annotations](#-annotations)); an annotation belongs to one chart, and adding it to another throws `InvalidOperationException`; `null` throws `ArgumentNullException` |

| `ThresholdsMode` flag | Drawing |
|---|---|
| `Lines` | A dashed line (`10,10`) of 2 DIP in the threshold color at 70% at each threshold but the base, with its `Label` above the right end |
| `Bands` | Each step up to the next threshold filled with its color at 15%, under the series |
| `ColorSeries` | Lines and areas on the primary value axis are colored by the region of their values with hard edges; bars, markers, scatter points and tooltip dots take the color of the threshold at their value, and the legend keeps the series colors |

Changes of a notifying list of thresholds are followed while the chart is attached; after changing a threshold in place, call `Invalidate()`.  

### 📣 Events

| Name | EventArgs | Description |
|---|---|---|
| `LegendItemTapped` | `ChartLegendEventArgs` | A legend item was tapped; `Handled = true` skips the toggle |
| `SelectionChanged` | `ChartSelectionChangedEventArgs` | The selection changed by a tap or a property; not raised when only the position moved with the data |
| `TrackingStarted` | `ChartTrackingEventArgs` | The trackball started |
| `TrackingChanged` | `ChartTrackingEventArgs` | The trackball moved to another point |
| `TrackingEnded` | `ChartTrackingEventArgs` | The trackball ended; `Index` is `-1` and `Points` is empty |

### 🎬 Updates

| Change | Animation |
|---|---|
| Values with the same number of points | Each point and the axis ranges move from the old values to the new ones |
| Item added at the end | The new point grows out of the previous one and the chart flows |
| First item removed | The point flows out of the plot |
| Item added or removed in the middle | Lines and areas grow from or shrink into the neighbors, bars grow from or shrink to zero height |
| Series hidden or shown in the legend | Lines fade, bars and stacked areas shrink to or grow from their baseline while the others close up |
| `Minimum` / `Maximum` of an axis | The axis range moves; on the value axis it follows the points in view |

A change during an animation continues from the state on the screen.  
Points are matched by the collection changes since the last update; a move, more than 64 changes or a reset matches them by position.  
Adding or removing series and changing `Orientation` switch at once.  

### 🧱 Stacking

Series of the same kind (bar or area), the same value axis and the same `StackGroup` stack at the same X.  
Positive values stack up from 0 and negative values down from 0, and `Percent` scales each side to its own total.  
Hidden series and missing points are left out, and segments are separated by `ChartStyle.SegmentGap`.  
Each stack and each unstacked bar takes an equal slot of the group, which is the category width times `BarWidthRatio`, and bars narrowed by `MaxBarWidth` are centered in the column.  

## 📊 Series

### ChartSeries

Base of all series.  
A series is an `Element`, so it inherits the `BindingContext` of the chart.  

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `Title` | `string?` | `null` | ✓ | Name in the legend and the tooltip |
| `ItemsSource` | `IEnumerable?` | `null` | ✓ | Data (see [Data](#-data)) |
| `ValueAccessors` | `IChartAccessorProvider?` | `null` | ✓ | Accessors of this series, used before those of the chart |
| `XKey`, `YKey` | `string?` | `null` | ✓ | Keys of X and Y; a `null` `XKey` uses the key of the chart |
| `ValueSelector` | `Func<object, double?>?` | `null` | | Y of an item from code, used before the keys |
| `YAxis` | `ChartAxisTarget` | `Primary` | ✓ | `Secondary` draws the series on the second value axis |
| `Color` | `Color?` | `null` | ✓ | `null` takes the palette color at the position of the series |
| `IsVisible` | `bool` | `true` | ✓ | Shown or hidden, TwoWay so that legend toggles reach the view model; hiding a series keeps the colors of the others |
| `LegendVisible` | `bool` | `true` | ✓ | Lists the series in the legend |
| `ValueFormat` | `string?` | `null` | ✓ | Format of data labels and tooltip values: composite when it contains `{` (`{0:N0} kWh`), otherwise a format string (`N1`) |
| `ValueConverter` | `IValueConverter?` | `null` | ✓ | Converts the value (`double`) to its text; used instead of `ValueFormat` |
| `ValueFormatter` | `Func<double, string>?` | `null` | | Text from code; used before `ValueConverter` and `ValueFormat` |
| `EmptyPointMode` | `ChartEmptyPointMode` | `Gap` | ✓ | How missing values (`null` and `NaN`) are drawn |
| `TrackItemChanges` | `bool` | `true` | ✓ | Reads an item again when it raises `PropertyChanged` |

When the three value formats are unset, data labels and tooltips use the format of the value axis, and when that is the default too, 100% stacks show percentages (`35%`) and the other values use the compact number format.  
A series beyond the palette is drawn with `ChartStyle.OtherColor` and reported once through `Trace.TraceWarning`.  

### LineSeries

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `Thickness` | `double?` | `null` | ✓ | Line width; `null` uses `ChartStyle.LineThickness` |
| `Interpolation` | `ChartLineInterpolation` | `Linear` | ✓ | Shape between the points |
| `DashPattern` | `DoubleCollection?` | `null` | ✓ | Dashes in multiples of the line width (`4,3`), as `StrokeDashArray` |
| `MarkerMode` | `ChartMarkerMode` | `Auto` | ✓ | Points that get a marker |
| `MarkerSize` | `double?` | `null` | ✓ | Marker diameter; `null` uses `ChartStyle.MarkerSize` |
| `MarkerShape` | `ChartMarkerShape` | `Circle` | ✓ | Shape of the markers, the selected point, the trackball point and the live dot |
| `GlowRadius` | `double` | `0` | ✓ | Spread of a soft glow under the line, 0 or more; `0` draws none |
| `LiveIndicatorVisible` | `bool` | `false` | ✓ | Draws a dot on the last point and two rings that grow from it every 1.8 seconds |

A marker is cut out of the line by `ChartStyle.MarkerKnockout`, which is really transparent, and points clipped by the plot get no marker.  
The cut-out follows the shape, and a diamond is the square turned by 45 degrees; the legend and the tooltip keep dots.  
The glow is 1.8 times the line width in the line color at 55%, blurred with a standard deviation of half of `GlowRadius`. It follows dashes and threshold colors, stays in the plot, and is left out in `Minimal` density and for series of more than 500 points.  
The live dot has the size and the cut-out of a marker, even with `MarkerMode` `None`, and the rings grow in its color from the marker radius to three times it while fading from 45%. The plot keeps the ring radius from the edge of the view on the side where X ends (the right, or the bottom of a horizontal chart), so the rings are not cut there. With the animations off only the dot is drawn.  

### AreaSeries

Derives from `LineSeries`.  

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `FillOpacity` | `double?` | `null` | ✓ | Opacity of the fill from 0 to 1; `null` uses `ChartStyle.AreaFillOpacity` |
| `FillMode` | `ChartFillMode` | `Solid` | ✓ | `Gradient` fades the fill from `FillOpacity` at the line to transparent at the baseline |
| `BaselineValue` | `double?` | `null` | ✓ | Value the fill reaches; `null` is 0, or the edge of the plot when 0 is out of the range |
| `StackGroup` | `string?` | `null` | ✓ | Areas with the same name stack |

### BarSeries

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `CornerRadius` | `double?` | `null` | ✓ | Radius of the data end of the bar (the top segment of a stack); `null` uses `ChartStyle.BarCornerRadius`; the baseline end and ends cut by the plot stay square |
| `MaxBarWidth` | `double?` | `null` | ✓ | Widest bar, greater than 0; `null` uses `ChartStyle.MaxBarWidth` |
| `BarWidthRatio` | `double` | `0.6` | ✓ | Share of the category width taken by the bar or the group, greater than 0 and up to 1; a group uses the value of its first visible bar series |
| `StackGroup` | `string?` | `null` | ✓ | Bars with the same name stack |
| `NegativeColor` | `Color?` | `null` | ✓ | Color of negative values; `null` uses the series color |
| `TrackVisible` | `bool` | `false` | ✓ | Draws a faint track behind each bar across the whole value range, like a progress bar |
| `FillMode` | `ChartFillMode` | `Solid` | ✓ | `Gradient` fades each bar from the series color at its end to transparent at the baseline |

A value axis with bars includes 0 unless `IncludeZero` is `false`, and bars on a numeric or date and time X axis widen the range by half of the smallest gap so that the end bars are not cut.  
The track uses `ChartStyle.TrackColor` or a neutral gray (black at 7% in light, white at 7% in dark), takes the width and position of the bar with square corners, and is drawn once for a stack.  
A gradient is measured from the value farthest from the baseline in the series, separately for positive and negative values, so short bars are faint; it is solid while thresholds color the series.  

### ScatterSeries

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `MarkerSize` | `double?` | `null` | ✓ | Point diameter; `null` uses `ChartStyle.MarkerSize` |
| `MarkerShape` | `ChartMarkerShape` | `Circle` | ✓ | Shape of the points |
| `FillOpacity` | `double?` | `null` | ✓ | Opacity of the fill from 0 to 1; `null` is 0.5 |

Points are filled with the series color at `FillOpacity` and outlined with it at 1 DIP, and are not connected.  
X is a number or a date and time; like a line, the value axis does not include 0 by default, and a fixed X range takes only the points inside it.  
The legend shows a dot, and the `Auto` entrance fades the points in while they grow from 60% of their size.  

### RangeBarSeries

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `LowKey`, `HighKey` | `string?` | `null` | ✓ | Keys of the low and the high value; `YKey` and `ValueSelector` are not used |
| `LowSelector`, `HighSelector` | `Func<object, double?>?` | `null` | | Low and high values from code, used before the keys |
| `CornerRadius` | `double?` | `null` | ✓ | Radius of both ends; `null` uses `ChartStyle.BarCornerRadius`; ends cut by the plot stay square |
| `MaxBarWidth` | `double?` | `null` | ✓ | Widest bar, greater than 0; `null` uses `ChartStyle.MaxBarWidth` |
| `BarWidthRatio` | `double` | `0.6` | ✓ | Share of the category width taken by the bar or the group, greater than 0 and up to 1 |
| `TrackVisible` | `bool` | `false` | ✓ | Draws a faint track behind each bar across the whole value range |

A range bar spans from the low to the high value in a bar slot, next to other bars and never stacked; swapped values are drawn from the smaller one, and a point without both values is skipped.  
The value axis does not include 0, data labels sit at both ends, the tooltip shows `12 – 25`, and a tap reports the high value in `Y`.  
The entrance grows each bar from the middle of its range, and added, removed or hidden bars shrink to the middle.  

### BubbleSeries

Derives from `ScatterSeries`.  

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `SizeKey` | `string?` | `null` | ✓ | Key of the size value |
| `SizeSelector` | `Func<object, double?>?` | `null` | | Size value from code, used before the key |
| `MinBubbleSize`, `MaxBubbleSize` | `double` | `8`, `40` | ✓ | Smallest and largest diameter, 0 or more |
| `SizeIncludesZero` | `bool` | `true` | ✓ | `false` sizes the bubbles from the smallest to the largest value among all the bubble series, as the XY chart of Grafana does, instead of from 0 |

The area grows from 0 to the largest size value among all the bubble series of the chart: 0 and below get `MinBubbleSize` and the largest value gets `MaxBubbleSize`.  
With `SizeIncludesZero` set to `false` the smallest value gets `MinBubbleSize` instead, and when all the values are equal every bubble gets `MinBubbleSize`.  
A point without a size is skipped. The automatic axis ranges grow by the radius of the largest bubble, and fixed ranges clip the bubbles at the plot.  
Larger bubbles are drawn first, a tap reaches the radius of a bubble or 22 DIP, whichever is larger, and the tooltip adds the size after the value.  

## 📏 Axes

### ChartAxis

Base of all axes, an `Element` set to `XAxis`, `YAxis` or `SecondaryYAxis`.  

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `IsVisible` | `bool?` | `null` | ✓ | `null` follows the density, which hides the axes in `Minimal` |
| `Title` | `string?` | `null` | ✓ | Title shown in `Full` density, on a row above the plot for a vertical axis and outside the labels for a horizontal axis |
| `TitleOrientation` | `ChartOrientation` | `Horizontal` | ✓ | `Vertical` turns the title of a vertical axis by 90° as in Grafana, reading upward on the left and downward on the right, outside the labels and centered on the plot; horizontal axes ignore it |
| `LabelsVisible` | `bool` | `true` | ✓ | Draws the tick labels |
| `LabelFormat` | `string?` | `null` | ✓ | Format of the labels: composite when it contains `{`, otherwise a format string |
| `LabelConverter` | `IValueConverter?` | `null` | ✓ | Converts the tick value of the axis type to its text; used instead of `LabelFormat` |
| `MaxLabelCount` | `int?` | `null` | ✓ | Largest number of labels, 1 or more; `null` fits them to the length (about one per 64 DIP on X and 40 DIP on Y) |
| `GridLinesVisible` | `bool?` | `null` | ✓ | `null` draws grid lines for the value axis only |
| `Position` | `ChartAxisPosition` | `Auto` | ✓ | `Opposite` moves the axis to the other side; the secondary value axis is always opposite the primary |
| `IsInversed` | `bool` | `false` | ✓ | Reverses the direction |

Each axis adds a `LabelFormatter` of its own value type, used before `LabelConverter` and `LabelFormat`.  
Overlapping labels are thinned to every k-th label, keeping both ends.  
Labels of a vertical axis take up to a third of the width and longer ones end with `…`; labels of a horizontal axis are not shortened. A category axis can also cut its labels to `LabelMaxLength` characters.  

### NumericAxis

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `Minimum`, `Maximum` | `double?` | `null` | ✓ | Fixed ends; data outside is clipped at the plot edge, and either end can be set alone |
| `SoftMinimum`, `SoftMaximum` | `double?` | `null` | ✓ | Ends shown at least, widened only when the data goes beyond them |
| `Interval` | `double?` | `null` | ✓ | Tick interval, greater than 0; `null` chooses a multiple of 1, 2, 2.5 or 5 |
| `IncludeZero` | `bool?` | `null` | ✓ | Includes 0; `null` is `true` when bars or areas use the axis |
| `NiceRange` | `bool` | `true` | ✓ | Widens the automatic ends to the next tick |
| `TickAlignment` | `ChartTickAlignment` | `AlignToPrimary` | ✓ | How the ticks of the secondary axis are chosen; ignored on the primary axis |
| `LabelFormatter` | `Func<double, string>?` | `null` | | Label text from code |

The range is decided in this order.  

1. Ends set by `Minimum` / `Maximum` are fixed.
2. The other ends follow the data of the visible series, widened to `SoftMinimum` / `SoftMaximum`. When `Minimum` / `Maximum` of a numeric or date and time X axis narrow the view, only the points in view and the values of lines and areas at its edges count.
3. `IncludeZero` adds 0.
4. `NiceRange` widens the automatic ends to a multiple of the interval.

A `Maximum` that is not greater than `Minimum` falls back to the automatic range and is reported once through `Trace.TraceWarning`, so that a binding updating the ends one by one never blanks the chart.  
Without data the range is 0 to 1, and a single value is widened by 10%.  
The default label format is the compact number format, and an axis carrying only 100% stacks labels percentages (`0%` to `100%`).  

| `TickAlignment` | Secondary axis |
|---|---|
| `AlignToPrimary` | Ticks at the heights of the primary ticks; the automatic range is widened so that the interval is a multiple of 1, 2, 2.5 or 5, and `Interval` is not used |
| `Independent` | Its own ticks drawn as short marks at the edge; the grid lines follow the primary axis only |

With two value axes, the title of each axis carries the colored markers of its series so that the axis of every line stays clear.  

### DateTimeAxis

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `Minimum`, `Maximum` | `DateTime?` | `null` | ✓ | Fixed ends; bind them to switch between a day, a week and a month |
| `IntervalType` | `ChartDateTimeIntervalType` | `Auto` | ✓ | Tick unit; a unit alone chooses among that unit and the larger ones |
| `Interval` | `double?` | `null` | ✓ | Ticks every n units of `IntervalType`, greater than 0; not used with `Auto` (months and years are rounded to whole numbers, fractions of the other units become the smaller unit) |
| `WindowSize` | `TimeSpan?` | `null` | ✓ | Shows only this span ending at `Maximum`, or at the latest point of the visible series when `Maximum` is `null`; `Minimum` is not used; greater than zero |
| `LabelFormatter` | `Func<DateTime, string>?` | `null` | | Label text from code |

`Auto` chooses the smallest step whose labels fit: 1, 2, 5, 10, 15 or 30 seconds or minutes, 1, 3, 6 or 12 hours, 1 or 2 days, 1 or 2 weeks, 1, 2, 3 or 6 months, or 1, 2, 5, 10, 20, 50 and more years.  
Ticks follow the calendar: weeks start on `FirstDayOfWeek` of the culture and months on January.  
The default format follows the step: `HH:mm:ss` under a minute, `HH:mm` under a day, `M/d` under two months and `yyyy/M` above, with the separators of the culture.  
With times of day, the tick where the date changes shows the date instead: `M/d` at midnight, or `M/d HH:mm` when no tick falls on midnight.  
Times are local: a `DateTimeOffset` uses `LocalDateTime`, a UTC `DateTime` is converted and other `DateTime` values are used as they are; the same applies to `Minimum` and `Maximum`.  
With `WindowSize`, short data is drawn at the right with the left side empty, older points are clipped at the edge of the plot, and the automatic value range and the legend table use only the points in the window.  

### CategoryAxis

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `LabelPlacement` | `ChartLabelPlacement` | `BetweenTicks` | ✓ | Where the categories sit |
| `CategoryWidth` | `double?` | `null` | ✓ | Length of each category; the chart then requests the width (the height in a horizontal chart) of all categories plus the axes, for a horizontal `ScrollView`; greater than 0; an explicit `WidthRequest` or `HeightRequest` wins |
| `LabelMaxLength` | `int?` | `null` | ✓ | Cuts each label to this many characters and adds `…`, as `xTickLabelMaxLength` of the Grafana bar chart; 1 or more, and `null` keeps the whole label. Emoji and combined characters are not split, spaces at the cut are dropped, and the tooltip shows the whole name |
| `LabelFormatter` | `Func<string, string>?` | `null` | | Label text from code |

Categories merge the X values of all series in order of appearance, hidden series included, so the axis does not change when a series is toggled.  
`LabelFormat` applies only composite formats (`{0} st`).  

## 📌 Annotations

### ChartAnnotation

Base of `ChartReferenceLine` and `ChartReferenceBand`, an `Element` added to `ClamChartView.Annotations` that inherits the `BindingContext` of the chart.  

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `Axis` | `ChartAnnotationAxis` | `Y` | ✓ | Axis the annotation is placed on |
| `YAxis` | `ChartAxisTarget` | `Primary` | ✓ | Value axis of an annotation with `Axis = Y` |
| `Label` | `string?` | `null` | ✓ | Text drawn with the line or the band |
| `Color` | `Color?` | `null` | ✓ | Color of the line, the band and the label |

### ChartReferenceLine

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `Value` | `double` | `NaN` | ✓ | Position on a value axis, or on a numeric or category X axis |
| `Time` | `DateTime?` | `null` | ✓ | Position on a date and time X axis |
| `DashPattern` | `DoubleCollection?` | `null` | ✓ | Dashes in multiples of the line width; `null` is `4,4` and an empty collection draws a solid line |

### ChartReferenceBand

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `Start`, `End` | `double` | `NaN` | ✓ | Ends of the band on a value axis, or on a numeric or category X axis, in any order |
| `StartTime`, `EndTime` | `DateTime?` | `null` | ✓ | Ends of the band on a date and time X axis |

| Item | Rule |
|---|---|
| Values | Finite or `NaN`, and infinities throw `ArgumentOutOfRangeException`; `NaN` draws nothing. A date and time X axis uses `Time`, `StartTime` and `EndTime`, and the other X axes use the numbers (positions on a `CategoryAxis`) |
| Line | 1 DIP dashed in `Color`, or `SecondaryTextColor` when `null`, over the series |
| Band | `Color`, or `MutedTextColor` when `null`, at 15% under the series |
| Label | `DataLabelFontSize` in `Color` or `SecondaryTextColor`: above the right end of a horizontal line, right of the top of a vertical line, and inside the top left of a band, truncated to its width |
| Range | Annotations on a value axis are included in its automatic range; annotations on the X axis are not, so passed times do not stretch a flowing time axis |

## 🍩 ClamPieChartView

Shows the shares of a whole.  
The default is a donut with the total in the center; `InnerRadius = 0` draws a pie and `StartAngle = 270` with `SweepAngle = 180` the upper half.  

### 🧩 Properties

#### Data

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `ItemsSource` | `IEnumerable?` | `null` | ✓ | Data; `ChartEntry` items need no keys |
| `ValueAccessors` | `IChartAccessorProvider?` | `null` | ✓ | Accessors (see [Data](#-data)) |
| `ValueKey`, `LabelKey`, `ColorKey` | `string?` | `null` | ✓ | Keys of the value, the name and the color |
| `ValueSelector` | `Func<object, double?>?` | `null` | | Value of an item from code, used before the keys |
| `LabelSelector` | `Func<object, string>?` | `null` | | Name of an item from code, used before the keys |
| `TrackItemChanges` | `bool` | `true` | ✓ | Reads an item again when it raises `PropertyChanged` |

#### Shape

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `InnerRadius` | `double` | `0.6` | ✓ | Hole as a fraction of the radius, from 0 to 0.95 |
| `StartAngle` | `double` | `0` | ✓ | Start of the sweep |
| `SweepAngle` | `double` | `360` | ✓ | Sweep, greater than 0 and up to 360 |
| `SliceGap` | `double?` | `null` | ✓ | Transparent gap between slices; `null` uses `ChartStyle.SegmentGap` |
| `SortMode` | `ChartSortMode` | `None` | ✓ | Order of the slices; colors keep the data order |
| `MaxSlices` | `int` | `8` | ✓ | Largest number of slices including the merged slice, 1 or more |
| `OthersThreshold` | `double` | `0` | ✓ | Slices below this share of the total, from 0 to less than 1, are merged when two or more qualify |
| `OthersLabel` | `string` | `Other` | ✓ | Name of the merged slice |

#### Labels and legend

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `LabelsMode` | `ChartPieLabelsMode` | `None` | ✓ | `Inside` draws the labels that fit in their slices; `Outside` draws them in columns beside the pie with leader lines |
| `LabelContent` | `ChartPieLabelContent` | `Percent` | ✓ | Text of the labels |
| `LegendVisible` | `bool?` | `null` | ✓ | `null` shows the legend in `Full` density |
| `LegendPosition` | `ChartLegendPosition` | `Bottom` | ✓ | Rows at the top or the bottom, or a column on the left or the right centered vertically |
| `LegendValueMode` | `ChartLegendValueMode` | `None` | ✓ | Adds the value or the percentage, right-aligned in a column and after the name in rows |
| `LegendToggleEnabled` | `bool` | `false` | ✓ | A legend tap hides or shows the slice, as in the pie chart of Grafana, instead of selecting it; the last visible slice stays |
| `LegendItemTappedCommand` | `ICommand?` | `null` | ✓ | Executed after `LegendItemTapped` |

#### Center and formats

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `CenterValueMode` | `ChartCenterValueMode` | `Auto` | ✓ | Value in the hole |
| `CenterValue` | `double?` | `null` | ✓ | Value shown instead of the total; the selected slice still comes first |
| `CenterLabel` | `string?` | `null` | ✓ | Small text under the value; the name and the percentage of the selected slice replace it |
| `CenterValueFormat` | `string?` | `null` | ✓ | Format of the center value; when the three center formats are unset the value formats are used |
| `CenterValueConverter` | `IValueConverter?` | `null` | ✓ | Converts the center value to its text |
| `CenterValueFormatter` | `Func<double, string>?` | `null` | | Center text from code |
| `ValueFormat` | `string?` | `null` | ✓ | Format of the slice values in the labels and the legend; unset uses the compact number format |
| `ValueConverter` | `IValueConverter?` | `null` | ✓ | Converts the slice value to its text |
| `ValueFormatter` | `Func<double, string>?` | `null` | | Slice value text from code |

#### Selection

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `SelectionMode` | `ChartSelectionMode` | `None` | ✓ | `Item` selects one slice; `Category` is the same as `Item` |
| `SelectedItem` | `object?` | `null` | ✓ | Item of the selected slice, TwoWay |
| `SelectedIndex` | `int` | `-1` | ✓ | Position of the selected slice in the data, TwoWay |
| `SelectionChangedCommand` | `ICommand?` | `null` | ✓ | Executed after `SelectionChanged` |
| `ExplodeOffset` | `double` | `6` | ✓ | Distance the selected slice moves out; the pie leaves this space when selection is on |

### 📣 Events

| Name | EventArgs | Description |
|---|---|---|
| `LegendItemTapped` | `ChartLegendEventArgs` | A legend item was tapped; then, unless handled, the slice is hidden or shown when `LegendToggleEnabled` is `true`, or else selected when `SelectionMode` is set, or cleared when it is already selected |
| `SelectionChanged` | `ChartSelectionChangedEventArgs` | The selection changed by a tap or a property |

### 📏 Rules

| Item | Rule |
|---|---|
| Values | Finite values greater than 0 become slices; negative values are skipped and reported once through `Trace.TraceWarning` |
| Merged slice | Items over `MaxSlices` (the smallest first) and below `OthersThreshold` become one last slice named `OthersLabel` in `ChartStyle.OtherColor`; a single item is never merged |
| Order | `None` keeps the data order, and `Descending` and `Ascending` order by value, equal values in data order |
| Colors | An explicit color (`ColorKey`, `ChartEntry.Color`) first, otherwise the palette in the data order of the slices drawn when nothing is hidden, followed by the items that hiding brings out of the merged slice; slices beyond the palette use `OtherColor` with a warning |
| Placement | The swept sector, hole included, is scaled to the largest size that fits outside the legend; the upper half sits on the bottom edge |
| Gaps | Parallel gaps of `SliceGap` between neighboring slices, none at the ends of a partial sweep or around a single full slice |
| Labels | At the middle angle of the slice, centered in the ring of a donut or at 0.62 of the radius of a pie, drawn only when they fit with 2 DIP of margin, in white or `#0B0B0B` by WCAG contrast; percentages use `P0` |
| Outside labels | Slices whose middle is on the right get a label in a column on the right, the others on the left. A line in the slice color leaves the edge, bends 8 DIP out and runs to the column 16 DIP out, and the text follows 4 DIP later in `SecondaryTextColor`. The labels of each side are stacked 2 DIP apart near their slices, and those of the smallest slices are left out when they do not fit. The pie shrinks to make room but not below half its size, leaving out the widest labels instead. The line follows a selected slice that moves out |
| Center value | Drawn when `InnerRadius` is greater than 0, shrunk to fit the hole (not drawn under 6 DIP); a half donut puts it on the flat side; switching the selection crossfades it over 150 ms; the number counts up from 0 during the entrance and from the shown number on updates |
| Taps | The sector of each slice, gaps included, from the hole to the edge, at its moved position when selected; a tap on the merged slice raises only `ItemTapped` with `Item = null`, `Index = -1` and the merged total; in `Interactive` mode a tap in the center or outside the slices clears the selection |
| Hidden slices | Hidden by name, so they stay hidden through data changes; they keep their place in the legend with a faded marker, a muted name and `-` as the percentage; the percentages, the center total and the merged slice count only the shown slices; the merged slice cannot be hidden; hiding the selected slice clears the selection; turning `LegendToggleEnabled` off shows every slice; when no shown value is left, the hidden slices are shown |
| Density | `Full` shows the legend, `Compact` hides it unless `LegendVisible` is `true`, `Minimal` keeps the donut and the center value |
| Updates | Slices are matched by data position and the merged slice as one; added slices open from the border of the previous slice, removed and merged ones close at the new border, and colors that change with the order move between the two colors; hiding and showing a slice moves the same way even when `UpdateAnimationEnabled` is `false` |

## ✨ ClamSparklineView

A chart without axes or legend for list cells, KPI tiles and text.  
It is drawn without allocations of its own: bars and win-loss allocate nothing, and lines and areas only the path that SkiaSharp creates.  

### 🧩 Properties

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `Values` | `IEnumerable<double>?` | `null` | ✓ | Values, used before `ItemsSource`; an `IReadOnlyList<double>` or `double[]` without change notifications is read in place, and other sequences are copied to a pooled array |
| `ItemsSource` | `IEnumerable?` | `null` | ✓ | Items of any type read through `ValueAccessors` and `ValueKey` |
| `ValueAccessors` | `IChartAccessorProvider?` | `null` | ✓ | Accessors (see [Data](#-data)) |
| `ValueKey` | `string?` | `null` | ✓ | Key of the value |
| `ValueSelector` | `Func<object, double?>?` | `null` | | Value of an item from code, used before the key |
| `TrackItemChanges` | `bool` | `true` | ✓ | Reads an item of `ItemsSource` again when it raises `PropertyChanged` |
| `Kind` | `ChartSparklineKind` | `Line` | ✓ | Shape |
| `Color` | `Color?` | `null` | ✓ | `null` uses the first palette color |
| `NegativeColor` | `Color?` | `null` | ✓ | Negative bars and losses; `null` uses `ChartStyle.CriticalColor` |
| `Thickness` | `double?` | `null` | ✓ | Line width; `null` uses `ChartStyle.LineThickness`, or 1.5 when the view is lower than 24 DIP |
| `Smooth` | `bool` | `false` | ✓ | Monotone cubic interpolation |
| `Markers` | `ChartSparklineMarkers` | `Last` | ✓ | Points that get a marker on lines and areas: `First`, `Last`, `High` and `Low` combined |
| `Minimum`, `Maximum` | `double?` | `null` | ✓ | Fixed range; pass the same values to every row to compare rows on one scale |
| `ReferenceValue` | `double?` | `null` | ✓ | Dashed hairline in `AxisLineColor` for a target or an average, included in the automatic range; not drawn for win-loss |
| `NormalRangeMinimum`, `NormalRangeMaximum` | `double?` | `null` | ✓ | Normal range drawn as a band in `MutedTextColor` at 15% and included in the automatic range; one end alone reaches the edge; finite values; not drawn for win-loss |
| `EmphasisMode` | `ChartSparklineEmphasis` | `None` | ✓ | `Last` draws the last segment (the last bar) in the series color and the rest, the fill included, at `ChartStyle.EmphasisDimmedOpacity` |
| `Thresholds` | `IList<ChartThreshold>` | empty | ✓ | Thresholds, not included in the automatic range; `null` throws `ArgumentNullException` |
| `ThresholdsMode` | `ChartThresholdsMode` | `None` | ✓ | As in `ClamChartView`, with 1 DIP dashed lines (`4,4`); `ColorSeries` colors lines and areas by region and bars, win-loss and markers by value |
| `EdgeToEdge` | `bool` | `false` | ✓ | Draws lines and areas to the edges of the content without the inset for the line caps and the markers, for card backgrounds; markers are kept inside |
| `LiveIndicatorVisible` | `bool` | `false` | ✓ | Dot and growing rings on the last point of lines and areas, as `LineSeries.LiveIndicatorVisible`; the line is inset by the marker on the left, top and bottom and by the ring radius on the right, except with `EdgeToEdge` |

| Item | Rule |
|---|---|
| Range | Lines and areas span the values, bars include 0 and win-loss uses only the sign, together with `ReferenceValue` and the normal range; equal values are widened by 1 on each side, and an inverted `Minimum` / `Maximum` falls back to the automatic range with a warning |
| Markers | Diameter 2.5 times the line width with a cut-out of half the width, in the line color; the highest and the lowest are their first points, and points sharing a position get one marker |
| Bars | Width of the slot minus a gap, at most `MaxBarWidth`, from 0 (or the nearer edge) with square corners |
| Win-loss | Wins fill the upper half and losses the lower half with 1 DIP between them; zeros and missing values are not drawn |
| Missing values | `NaN` breaks the line; bars and win-loss skip it |
| Updates | Changes of a notifying list (add, remove, replace) and of item values animate; replacing `Values` or `ItemsSource` and resets do not, so reused rows never move |
| Entrance | `Auto` plays nothing; `Grow`, `Reveal`, `Fade` and `PlayEntranceAnimation()` play |
| Density | Always `Minimal`, with whole-chart taps only |

## 🔢 ClamStatView

A KPI tile: a title, a value with its unit, a difference, a trend line and a state, drawn on one canvas.  
The value counts up from the shown value when it changes, and counting allocates nothing of its own.  

### 🧩 Properties

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `Title` | `string?` | `null` | ✓ | Label at the top left |
| `Value` | `double` | `0` | ✓ | Value; `NaN` shows `EmptyText` in `MutedTextColor` without the unit, the difference and the state |
| `ValueFormat`, `ValueConverter` | `string?`, `IValueConverter?` | `null` | ✓ | Format of the value (see [Formats](#formats)); the default is the compact number |
| `ValueFormatter` | `Func<double, string>?` | `null` | | Format of the value from code, used first |
| `Unit` | `string?` | `null` | ✓ | Unit after the value, on the same baseline |
| `ValueFontSize`, `MinValueFontSize` | `double` | `32`, `16` | ✓ | Largest and smallest size of the value; positive finite values |
| `PreviousValue` | `double?` | `null` | ✓ | Base of the difference |
| `Delta` | `double?` | `null` | ✓ | Difference given directly, used before `PreviousValue`; a ratio in `Percent` mode (`0.082` is 8.2%) |
| `DeltaMode` | `ChartDeltaMode` | `Percent` | ✓ | Ratio to the size of the base, or the difference itself |
| `DeltaFormat` | `string?` | `null` | ✓ | Format of the difference; `null` writes the sign of the culture before `P1` or the compact number |
| `DeltaCaption` | `string?` | `null` | ✓ | Text after the difference, shown only with a difference |
| `IncreaseIsGood` | `bool` | `true` | ✓ | `false` colors increases with `NegativeTextColor`, for costs and errors |
| `TrendValues` | `IEnumerable<double>?` | `null` | ✓ | Trend line, read like `ClamSparklineView.Values` |
| `TrendKind` | `ChartSparklineKind` | `Area` | ✓ | Shape of the trend |
| `TrendPlacement` | `ChartTrendPlacement` | `Bottom` | ✓ | Place of the trend |
| `Thresholds` | `IList<ChartThreshold>` | empty | ✓ | State by value; `null` throws `ArgumentNullException` |
| `ColorMode` | `ChartStatColorMode` | `None` | ✓ | How the state and the tile color are shown |
| `Fill` | `Color?` | `null` | ✓ | Color of the tile used by `Value`, `BackgroundGradient` and the trend; `null` uses the state color, then the first palette color |
| `LayoutMode` | `ChartStatLayoutMode` | `Auto` | ✓ | Arrangement |
| `AlertAnimationEnabled` | `bool` | `false` | ✓ | While the value is at a `Critical` threshold, the band and the icon of `Indicator`, the background of `Background` and `BackgroundGradient`, or the value and the unit of `Value` pulse between 100% and 60% opacity every 1.2 seconds; nothing pulses with `None` or with the animations off |

| Item | Rule |
|---|---|
| Vertical | Title, value and difference stacked from the top left, and a bottom trend in the rest |
| Horizontal | Title at the top left, the value and the unit at the top right, and the difference under the value; `Auto` uses it when the width is at least 2.5 times the height |
| Fitting | The value takes the largest size up to `ValueFontSize` in 0.5 DIP steps. A short height drops the bottom trend and then the difference before the value goes below `MinValueFontSize`, and a short width drops the unit. After that the value shrinks below the minimum down to 6 DIP and is not drawn under it, so numbers are never truncated |
| Difference | Nothing is shown when `Percent` mode has a base of 0. With the default format, a difference that rounds to zero is neutral (`SecondaryTextColor`, no arrow); others get a vector arrow in `PositiveTextColor` or `NegativeTextColor` |
| Trend | A line of 1 DIP in the tile color (`Fill`, then the threshold color even with `None`, then the first palette color) over a fill of the same color at 20%; on a filled background, the line is the tile color brightened by 40% over a white fill at 40%, as in the Grafana stat panel. `Bottom` keeps at least 16 DIP under the text, `Right` takes the right 40% of the content, and `Background` fills the bottom 60% of the tile edge to edge and fades toward the top |
| Colors | `None` draws the text in ink colors. `Value` writes the value, the unit and the trend in the tile color. `Indicator` adds a 4 DIP band in the state color at the left edge, `Background` tints the tile with the state color at 15%, and `BackgroundGradient` fills the tile at 120° from the tile color shifted by +8° in hue and -15 in lightness to -8° and -5 (+10.5 and +3.5 when `ChartStyle.IsDark` is `false`). Text on the gradient is `#202226` when the brightness of the tile color is above 180 and `#F7F8FA` otherwise |
| State icon | With a color mode, the level of the threshold puts a vector icon before the title: `Good` ✓, `Warning` ▲, `Serious` ◆ and `Critical` ● |
| Padding | `ContentPadding` `null` is 12 DIP with a color mode and the density default with `None` |
| Animation | The entrance counts from 0, reveals the trend and fades in the difference, the caption and the icon over its last 30%. A new value counts from the shown value; the trend animates like a sparkline. Numbers in the default format count with the decimals of the two ends |
| Taps | The whole tile raises `ChartTapped` |

### ChartThreshold

| Name | Type | Default | Description |
|---|---|---|---|
| `Value` | `double` | `Double.NegativeInfinity` | The state applies from this value; leave the first one as the base |
| `Color` | `Color?` | `null` | Color of the state; `null` uses the color of `Level` |
| `Level` | `ChartThresholdLevel` | `None` | Level of the state, which chooses the status color and the icon |
| `Label` | `string?` | `null` | Text above the right end of the threshold line of `ClamChartView` in `Lines` mode |

The threshold with the largest `Value` at or below the value applies, and the later one wins a tie.  
Changes of a notifying list are followed; after changing a threshold in place, call `Invalidate()`.  

## 📶 ClamBarGaugeView

Bars of a value against a maximum for several items, such as storage use, the progress of stores toward their goals or a ranking.  
The bars are drawn like the Grafana bar gauge: `Basic`, `Gradient` and the LCD-like `Segmented`.  
With `AutoHeight`, a horizontal gauge measures its height from the rows, so it grows with the items in a `StackLayout`.  

### 🧩 Properties

#### Data

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `ItemsSource` | `IEnumerable?` | `null` | ✓ | Data; `ChartEntry` items need no keys |
| `ValueAccessors` | `IChartAccessorProvider?` | `null` | ✓ | Accessors (see [Data](#-data)) |
| `ValueKey`, `LabelKey`, `MaximumKey`, `TargetKey`, `ColorKey` | `string?` | `null` | ✓ | Keys of the value, the name, the maximum of the item, the target and the color |
| `ValueSelector` | `Func<object, double?>?` | `null` | | Value of an item from code, used before the keys |
| `LabelSelector` | `Func<object, string?>?` | `null` | | Name of an item from code, used before the keys |
| `TrackItemChanges` | `bool` | `true` | ✓ | Reads an item again when it raises `PropertyChanged` |

#### Shape

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `Minimum`, `Maximum` | `double` | `0`, `100` | ✓ | Range of every item, finite values; the maximum of an item comes first |
| `Orientation` | `ChartOrientation` | `Horizontal` | ✓ | `Horizontal` stacks rows and `Vertical` places columns |
| `DisplayMode` | `ChartBarGaugeMode` | `Basic` | ✓ | Drawing of the bars |
| `SegmentWidth` | `double` | `12` | ✓ | Width of a cell in `Segmented`, greater than 0; the count of cells is the bar length divided by it, rounded down |
| `BarThickness` | `double` | `8` | ✓ | Thickness of the bars, greater than 0 |
| `ItemSpacing` | `double` | `12` | ✓ | Space between rows or columns, zero or more |
| `LabelPosition` | `ChartBarGaugeLabelPosition` | `Top` | ✓ | Place of the names |
| `ValueVisible` | `bool` | `true` | ✓ | Shows the value at the end of each row or above each column |
| `ValueMode` | `ChartBarGaugeValueMode` | `Percent` | ✓ | Text of the value |
| `ValueColorMode` | `ChartBarGaugeValueColorMode` | `Bar` | ✓ | Color of the value: the bar color, or `PrimaryTextColor` as the text value mode of the Grafana bar gauge |
| `ValueFormat`, `ValueConverter` | `string?`, `IValueConverter?` | `null` | ✓ | Format of the numbers in `Value` and `ValueOfMaximum` (see [Formats](#formats)); the default is the compact number |
| `ValueFormatter` | `Func<double, string>?` | `null` | | Format of the numbers from code, used first |
| `TrackVisible` | `bool` | `true` | ✓ | Draws the part that is not reached in `ChartStyle.TrackColor` |
| `TargetVisible` | `bool` | `true` | ✓ | Draws a line at the target of the items that have one |
| `Thresholds` | `IList<ChartThreshold>` | empty | ✓ | Colors by the percentage of the maximum; `null` throws `ArgumentNullException` |
| `Color` | `Color?` | `null` | ✓ | Color of every bar; `null` uses the first palette color |
| `SortMode` | `ChartSortMode` | `None` | ✓ | Order of the rows; items without a value come last |
| `MaxItems` | `int?` | `null` | ✓ | Shows only the first items after sorting, zero or more |
| `AutoHeight` | `bool` | `true` | ✓ | Measures the height from the rows when horizontal and `HeightRequest` is not set |

| Item | Rule |
|---|---|
| Rows | Rows follow the item order, sorted by `SortMode` and cut at `MaxItems`. `Top` puts the name 4 DIP above the bar, and `Left` puts the names right-aligned in a column of up to 40% of the width, 8 DIP before the bars; names take room only when some item has one. The bar line is as high as `BarThickness` or the value text, whichever is higher, with the bar in its middle, and the values are right-aligned after the bars |
| Columns | `Vertical` divides the width evenly, with the value at the top, the name at the bottom and the bar growing upward between them, at most `BarThickness` wide |
| Ratio | (value − `Minimum`) / (maximum − `Minimum`), where the maximum is the item's, otherwise `Maximum`. Bars are clamped from 0 to 1 and drawn at least 1 DIP long, and `Percent` writes the ratio in `P0` without clamping (`120%`). An item without a value or with an empty range draws neither the bar nor the value |
| Thresholds | Compared with the percentage of the maximum (`Value = 70` means 70%), so items with different maximums share them; the later one wins a tie |
| `Basic` | The bar color at 35% with a 2 DIP line at the end |
| `Gradient` | The threshold colors change halfway between each threshold and the previous one; thresholds above the value are not used and the last color sits at the end of the bar. Without thresholds the bar is one color |
| `Segmented` | Each cell takes the threshold color at its start. Cells up to the value are lit with a radial gradient from 95% to 55%, and the others are drawn at 18%. Cells are separated by `ChartStyle.SegmentGap` and fill the bar length |
| Track | `Basic` and `Gradient` draw the track only beyond the bar; in `Segmented` the unlit cells take its place |
| Target | A 2 DIP line in `PrimaryTextColor`, 3 DIP longer than the bar on each side |
| Colors | The bar color is the item color (`ColorKey`, `ChartEntry.Color`), then `Color`, then the first palette color, and the threshold color at the value when thresholds are set. The value uses the same color, or `PrimaryTextColor` with `ValueColorMode` set to `Text`, and the name `SecondaryTextColor` |
| Height | `AutoHeight` measures the rows that are shown plus the padding. The density then depends on the width only, so a gauge with one row keeps its item taps |
| Taps | A row (name, bar and value) raises `ItemTapped` with the data position in `Index`, the name in `X` and the value in `Y`; the space between rows raises `ChartTapped` |
| Animation | The entrance grows the bars from 0 with 40 ms between items (at most 30% of the duration in total) and fades in the values over its last 30%. Updates move the lengths, the positions after sorting and the opacity from the shown state; added rows grow from 0 and removed rows shrink and fade where they were. Frames allocate nothing |

## ⭕ ClamRadialBarView

Concentric rings that compare several progress values, such as activity goals or the load of servers.  
The rings are drawn like the Grafana gauge: flat ends over a neutral track, in one color, a gradient or segments.  
The first item is the outermost ring.  

### 🧩 Properties

#### Data

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `ItemsSource` | `IEnumerable?` | `null` | ✓ | Data; `ChartEntry` items need no keys |
| `ValueAccessors` | `IChartAccessorProvider?` | `null` | ✓ | Accessors (see [Data](#-data)) |
| `ValueKey`, `LabelKey`, `MaximumKey`, `ColorKey` | `string?` | `null` | ✓ | Keys of the value, the name, the maximum of the item and the color |
| `ValueSelector` | `Func<object, double?>?` | `null` | | Value of an item from code, used before the keys |
| `LabelSelector` | `Func<object, string?>?` | `null` | | Name of an item from code, used before the keys |
| `TrackItemChanges` | `bool` | `true` | ✓ | Reads an item again when it raises `PropertyChanged` |

#### Shape

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `Minimum`, `Maximum` | `double` | `0`, `100` | ✓ | Range of every ring, finite values; the maximum of an item comes first |
| `StartAngle`, `SweepAngle` | `double` | `0`, `360` | ✓ | Start and length of the rings; `250` and `220` give the Grafana gauge open at the bottom |
| `InnerRadius` | `double` | `0.4` | ✓ | Inner edge of the area the automatic thickness fills, as a fraction of the radius, from 0 to 0.95 |
| `RingThickness` | `double?` | `null` | ✓ | Thickness of each ring, greater than 0; `null` divides the area outside `InnerRadius`, up to 1/6 of the radius |
| `RingSpacing` | `double` | `4` | ✓ | Space between the rings, zero or more |
| `CapStyle` | `ChartCapStyle` | `Flat` | ✓ | Ends of the rings and the track |
| `DisplayMode` | `ChartRadialBarMode` | `Basic` | ✓ | Drawing of the rings |
| `SegmentCount` | `int` | `20` | ✓ | Cells of a ring in `Segmented`, 1 or more; fewer on short rings |
| `TrackVisible` | `bool` | `true` | ✓ | Draws the rest of each ring in `ChartStyle.TrackColor` |
| `BarGlow` | `bool` | `false` | ✓ | Makes the value of each ring glow, like the bar glow of the Grafana gauge |
| `ThresholdMarkersVisible` | `bool` | `false` | ✓ | Draws a thin ring of the threshold ranges outside the outermost ring, like the threshold markers of the Grafana gauge |
| `OverflowMode` | `ChartOverflowMode` | `Clamp` | ✓ | Values over the maximum |
| `Thresholds` | `IList<ChartThreshold>` | empty | ✓ | Colors by the percentage of the maximum; `null` throws `ArgumentNullException` |

#### Center and legend

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `CenterValueMode` | `ChartCenterValueMode` | `Auto` | ✓ | Value in the middle |
| `CenterText` | `string?` | `null` | ✓ | Text shown in the middle while no ring is selected, instead of the value |
| `CenterLabel` | `string?` | `null` | ✓ | Small text under the value while no ring is selected, instead of the name |
| `ValueFormat`, `ValueConverter` | `string?`, `IValueConverter?` | `null` | ✓ | Format of the `Total` value and the legend values (see [Formats](#formats)); the default is the compact number |
| `ValueFormatter` | `Func<double, string>?` | `null` | | Format of the values from code, used first |
| `LegendVisible` | `bool?` | `null` | ✓ | `null` shows the legend in `Full` density |
| `LegendPosition` | `ChartLegendPosition` | `Right` | ✓ | Rows at the top or the bottom, or a column on the left or the right |
| `LegendValueMode` | `ChartLegendValueMode` | `None` | ✓ | Adds the value or the percentage after the names |
| `LegendItemTappedCommand` | `ICommand?` | `null` | ✓ | Executed after `LegendItemTapped` |

#### Selection

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `SelectionMode` | `ChartSelectionMode` | `None` | ✓ | `Item` selects one ring; `Category` is the same as `Item` |
| `SelectedItem` | `object?` | `null` | ✓ | Item of the selected ring, TwoWay |
| `SelectedIndex` | `int` | `-1` | ✓ | Position of the selected ring in the data, TwoWay |
| `SelectionChangedCommand` | `ICommand?` | `null` | ✓ | Executed after `SelectionChanged` |

### 📣 Events

| Name | EventArgs | Description |
|---|---|---|
| `LegendItemTapped` | `ChartLegendEventArgs` | A legend item was tapped; then, unless handled, the ring is selected when `SelectionMode` is set, or cleared when it is already selected |
| `SelectionChanged` | `ChartSelectionChangedEventArgs` | The selection changed by a tap or a property |

### 📏 Rules

| Item | Rule |
|---|---|
| Size | The rings take the largest radius whose arc fits outside the legend. The automatic thickness divides the area between `InnerRadius` and the edge among the rings, up to 1/6 of the radius. Round ends on a partial sweep leave room for the ends, and a glow leaves 2% of the shorter side around the rings, as in Grafana |
| Ratio | (value − `Minimum`) / (maximum − `Minimum`), where the maximum is the item's, otherwise `Maximum`. `Clamp` keeps it from 0 to 1. `Wrap` draws the part over the maximum on top of the first lap in the ring color with 15 less lightness, showing only what is left of the last lap. An item without a value or with an empty range draws only the track |
| Colors | The item color (`ColorKey`, `ChartEntry.Color`), then the palette in ring order, and the threshold color at the value when thresholds are set; rings beyond the palette use `OtherColor` with a warning |
| Track | From the end of the value to the end of the sweep, with the same ends, in `TrackColor` or a neutral gray (black at 7% in light, white at 7% in dark) |
| `Gradient` | As in the Grafana gauge. With thresholds, each threshold color sits at its position around the ring and the ring shows it up to the value. Without thresholds, a darker and a lighter shade of the ring color are turned so that the strongest contrast falls at the value. Round ends get the colors at both ends |
| `Segmented` | Each cell takes the threshold color at its start and is lit when its start is before the value. Unlit cells use `TrackColor`, otherwise 30% black in light and 20% white in dark. Cells are separated by `ChartStyle.SegmentGap`, at least 3 DIP long, with flat ends; `Wrap` is not used |
| Glow | As the bar glow of Grafana: a copy of the value arc with the same width and colors, blurred with a standard deviation of 0.75 + 0.08 × the thickness, drawn under all the rings. `Basic` follows `CapStyle`, `Gradient` keeps its colors and blurs round ends too, and `Segmented` glows every cell, unlit ones included unless `TrackVisible` is `false`. The track of `Basic` and `Gradient` and the wrapped lap do not glow. `Minimal` density draws no glow |
| Threshold markers | As in Grafana, one thin ring outside the outermost ring: 0.2 × thickness^0.92 wide, from 4 to 12 DIP, with half of that (from 2 to 12 DIP) of space, and the rings shrink to make room. Each threshold range takes its color, the range below the first threshold the color of the outermost ring; `Gradient` uses the gradient of the outermost ring and `Segmented` does not split it. The ends are flat and `BarGlow` makes it glow too. Nothing is drawn or reserved without thresholds or in `Minimal` density |
| Center | Fitted inside the innermost ring, up to 32 DIP. `Auto` shows the selected ring, otherwise the outermost one, as a percentage with its name; `Total` shows the sum of the values with `CenterLabel`. The number counts up from 0 during the entrance and from the shown number on updates |
| Selection | The other rings fade to `DimmedOpacity` over 150 ms; a second tap on the selected ring clears it |
| Taps | The band of each ring, up to half the spacing, raises `ItemTapped` with the data position in `Index`, the name in `X` and the value in `Y`; elsewhere `ChartTapped` is raised and the selection is cleared |
| Animation | The entrance grows the rings from 0 with 40 ms between them (at most 30% of the duration in total). Updates move the lengths, and the radii when rings come and go, from the shown state. Frames allocate nothing |
| Density | `Full` shows the legend, `Compact` hides it unless `LegendVisible` is `true`, and `Minimal` keeps the rings and the center value with whole-chart taps |

## 🕸 ClamRadarChartView

Compares several series on axes that spread from the center, one axis per item, such as scores of products.  
Grafana has no radar, so the look follows its line and area tone: a hairline grid, 2 DIP lines over faint fills, and markers only where a vertex is selected or tracked.  
`Series` is the content property, so `RadarSeries` are written as child elements in XAML.  

### 🧩 Properties

#### Series and data

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `Series` | `IList<RadarSeries>` | empty `ObservableCollection<RadarSeries>` | ✓ | Series drawn in order; a series belongs to one chart, and adding it to another throws `InvalidOperationException` |
| `ValueAccessors` | `IChartAccessorProvider?` | `null` | ✓ | Accessors for series without their own |
| `XKey` | `string?` | `null` | ✓ | Key of the item name for series without their own |

#### Shape

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `Minimum`, `Maximum` | `double?` | `null` | ✓ | Value at the center and at the outer ring, finite values; `null` is chosen from the visible series with 0 included and nice levels, and a `Maximum` not greater than `Minimum` falls back to the automatic range with a warning |
| `GridShape` | `ChartRadarGridShape` | `Polygon` | ✓ | Shape of the rings; fewer than 3 items always use circles |
| `LevelCount` | `int` | `5` | ✓ | Most rings, 1 or more; nice level steps can give fewer |
| `ValueFormat`, `ValueConverter` | `string?`, `IValueConverter?` | `null` | ✓ | Format of the level values and of the tooltip values of series without their own format (see [Formats](#formats)); the default is the compact number |
| `ValueFormatter` | `Func<double, string>?` | `null` | | Format of those values from code, used first |

#### Legend

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `LegendVisible` | `bool?` | `null` | ✓ | `null` shows the legend with 2 or more series in `Full` density |
| `LegendPosition` | `ChartLegendPosition` | `Bottom` | ✓ | Rows at the top or the bottom, or a column on the left or the right centered vertically |
| `LegendToggleEnabled` | `bool` | `false` | ✓ | A legend tap shows or hides the series, keeping the last visible one |
| `LegendItemTappedCommand` | `ICommand?` | `null` | ✓ | Executed after `LegendItemTapped` |

#### Selection and trackball

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `SelectionMode` | `ChartSelectionMode` | `None` | ✓ | `Item` selects one vertex of a series, `Category` one item on every series; `None` clears the selection |
| `SelectedItem` | `object?` | `null` | ✓ | Item of the selected vertex, TwoWay; for `Category`, the item of the first visible series with a value |
| `SelectedIndex` | `int` | `-1` | ✓ | Position of that item in the data of its series, TwoWay |
| `SelectedSeries` | `RadarSeries?` | `null` | ✓ | Series of an `Item` selection (`null` for `Category`), TwoWay |
| `SelectionChangedCommand` | `ICommand?` | `null` | ✓ | Executed after `SelectionChanged` |
| `TrackballMode` | `ChartTrackballMode` | `None` | ✓ | How the trackball starts, as in `ClamChartView` |
| `TooltipVisible` | `bool` | `true` | ✓ | Draws the tooltip |
| `TooltipFormatter` | `Func<ChartTrackingInfo, string>?` | `null` | | Text of each series row after its colored dot; `null` is the title and the formatted value |
| `TrackedItem` | `object?` | `null` | ✓ | Item of the first series at the tracked axis, OneWayToSource; `null` when released |
| `TrackedIndex` | `int` | `-1` | ✓ | Position of that item in the data of its series, OneWayToSource |
| `TrackingChangedCommand` | `ICommand?` | `null` | ✓ | Executed when tracking starts, moves to another axis and ends |

### RadarSeries

Derives from `ChartSeries`, so `Title`, `ItemsSource`, `ValueAccessors`, `Color`, `IsVisible`, `LegendVisible`, the formats, `EmptyPointMode` and `TrackItemChanges` work as described there.  
`XKey` is the item name and `YKey` (or `ValueSelector`) the value; `ChartEntry` items need no keys. `YAxis` is not used, and adding a radar series to `ClamChartView` throws `InvalidOperationException`.  

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `Thickness` | `double?` | `null` | ✓ | Line thickness, 0 or more; `null` uses `ChartStyle.LineThickness` |
| `FillOpacity` | `double` | `0.15` | ✓ | Opacity of the fill, from 0 to 1 |

### 📣 Events

| Name | EventArgs | Description |
|---|---|---|
| `LegendItemTapped` | `ChartLegendEventArgs` | A legend item was tapped; `Handled = true` skips the toggle |
| `SelectionChanged` | `ChartSelectionChangedEventArgs` | The selection changed by a tap or a property |
| `TrackingStarted` | `ChartTrackingEventArgs` | The trackball started |
| `TrackingChanged` | `ChartTrackingEventArgs` | The trackball moved to another axis |
| `TrackingEnded` | `ChartTrackingEventArgs` | The trackball ended; `Index` is `-1` and `Points` is empty |

### 📏 Rules

| Item | Rule |
|---|---|
| Axes | Each item name becomes one axis in the order it first appears across all the series, hidden ones included, so showing and hiding a series keeps the axes. The axes start at 12 o'clock and go clockwise at equal angles. Series without names (numeric X or plain values) place their values by position on unnamed axes, numbered in the tooltip. A name repeated in one series keeps its first value |
| Missing values | An item without a value in a series is skipped, and its neighbors are joined |
| Levels | Chosen from the visible series with 0 included and nice steps; the center is the minimum and the outer ring the maximum, and values outside the range stop at the center or the outer ring |
| Size | The radar takes the largest radius at which it fits outside the legend with its item names, and the whole is centered in the direction left over, so wider names on one side move the center. Names are cut at 1/4 of the width, and names and level values are left out when they would make the radius smaller than 40% of the radius without them. The outer ring keeps room for the selection markers |
| Grid | A polygon (or a circle) for each level and a line from the center to each axis, as hairlines in `GridLineColor` |
| Item names | 8 DIP outside the outer ring in `SecondaryTextColor`: centered at the top and the bottom, left-aligned on the right and right-aligned on the left, and shifted up or down with the direction. The item selected by `Category` uses `PrimaryTextColor` |
| Level values | Small text in `MutedTextColor` centered on the 12 o'clock axis at each level, except the center; crowded values are thinned. The grid behind them is cut out, and they are drawn over the series |
| Series | Fills at `FillOpacity` first, then 2 DIP lines with round joins |
| Markers | Only at the selected vertex (`Item`), at the selected item of every series (`Category`) and at the tracked axis of every series, with the same size and knockout as in `ClamChartView`; a selected marker grows from 60% over 150 ms |
| Selection | For `Item`, the other series fade to `DimmedOpacity` over 150 ms. Tapping the selected vertex (the same item for `Category`) again clears it, and so does a tap outside the vertices in `Interactive` mode unless `ChartTapped` is handled |
| Trackball | Snaps to the axis nearest to the direction of the finger from the center, draws that axis in `TrackballLineColor` with the markers of every series, and shows a tooltip with the item name and a row for each series, placed as in `ClamChartView` |
| Taps | The nearest vertex within 22 DIP, the upper series first when they overlap, raises `ItemTapped` with the item name in `X` and the value in `Y`. In `Category` selection, a tap inside the radar selects the item in the nearest direction |
| Animation | The entrance grows the series from the center (`Grow`, also for `Auto`); `Fade` raises the opacity and `Reveal` sweeps clockwise from 12 o'clock. Updates move the values and the levels when the axes and the number of series stay the same, and showing or hiding a series fades it while the levels move. The paths are built again only when the layout or an animation changes them, so still, selected and tracked frames allocate nothing |
| Density | `Full` shows the legend with 2 or more series, `Compact` hides it unless `LegendVisible` is `true`, and `Minimal` keeps only the shapes, without names, level values, the legend or interaction, with whole-chart taps |

## 🟩 ClamHeatmapView

Shows values as shades of color, either as a matrix such as days by hours, or as a calendar of daily values with one column per week like the contribution graph of GitHub.  
The colors follow the Grafana heatmap: the Oranges scheme in 64 steps, reversed in the dark theme so that large values are light.  

### 🧩 Properties

#### Data

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `ItemsSource` | `IEnumerable?` | `null` | ✓ | Data; `ChartEntry` items make one row of a matrix and `ChartTimePoint` items make a calendar without keys |
| `ValueAccessors` | `IChartAccessorProvider?` | `null` | ✓ | Accessors (see [Data](#-data)) |
| `RowKey`, `ColumnKey`, `ValueKey` | `string?` | `null` | ✓ | Keys of the row name, the column and the value of a matrix; without `RowKey` the matrix has one row |
| `DateKey` | `string?` | `null` | ✓ | Key of the date of a calendar (`DateTime` or `DateTimeOffset`) |
| `RowSelector` | `Func<object, string?>?` | `null` | | Row name of an item from code, used before the key |
| `ValueSelector` | `Func<object, double?>?` | `null` | | Value of an item from code, used before the key |
| `TrackItemChanges` | `bool` | `true` | ✓ | Reads an item again when it raises `PropertyChanged` |

#### Shape and colors

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `LayoutMode` | `ChartHeatmapLayoutMode` | `Matrix` | ✓ | A matrix or a calendar |
| `Minimum`, `Maximum` | `double?` | `null` | ✓ | Values at the two ends of the colors, finite values; `null` takes the smallest and the largest value, and a `Maximum` not greater than `Minimum` falls back to them with a warning |
| `ColorScale` | `IReadOnlyList<Color>?` | `null` | ✓ | Colors from low to high values, two or more; `null` uses `ChartStyle.HeatmapColorScale` |
| `CellGap` | `double` | `1` | ✓ | Space after each cell, 0 or more |
| `ValueFormat`, `ValueConverter` | `string?`, `IValueConverter?` | `null` | ✓ | Format of the values of the legend and the tooltip (see [Formats](#formats)); the default is the compact number |
| `ValueFormatter` | `Func<double, string>?` | `null` | | Format of the values from code, used first |
| `LegendVisible` | `bool?` | `null` | ✓ | Shows the color scale; `null` shows it in `Full` density |
| `LegendPosition` | `ChartLegendPosition` | `Bottom` | ✓ | A horizontal scale under or above the cells, or a vertical one on the left or the right |

#### Selection and trackball

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `SelectionMode` | `ChartSelectionMode` | `None` | ✓ | `Item` selects one cell; `Category` is the same as `Item` |
| `SelectedItem` | `object?` | `null` | ✓ | Item of the selected cell, TwoWay |
| `SelectedIndex` | `int` | `-1` | ✓ | Position of the selected item in the data, TwoWay |
| `SelectionChangedCommand` | `ICommand?` | `null` | ✓ | Executed after `SelectionChanged` |
| `TrackballMode` | `ChartTrackballMode` | `None` | ✓ | How the trackball starts, as in `ClamChartView` |
| `TooltipVisible` | `bool` | `true` | ✓ | Draws the tooltip |
| `TrackedItem` | `object?` | `null` | ✓ | Item of the cell under the finger, OneWayToSource; `null` for an empty cell and when released |
| `TrackedIndex` | `int` | `-1` | ✓ | Position of that item in the data, OneWayToSource |
| `TrackingChangedCommand` | `ICommand?` | `null` | ✓ | Executed when tracking starts, moves to another cell and ends |

### 📣 Events

| Name | EventArgs | Description |
|---|---|---|
| `SelectionChanged` | `ChartSelectionChangedEventArgs` | The selection changed by a tap or a property |
| `TrackingStarted`, `TrackingChanged`, `TrackingEnded` | `ChartTrackingEventArgs` | The trackball started, moved to another cell or ended; `X` is the column or the date, `Index` is `-1` for an empty cell, and `Points` is empty |

### 📏 Rules

| Item | Rule |
|---|---|
| Matrix | Rows follow the order in which their names appear. Columns follow the order of appearance for text and ascend for numbers and dates. When a cell gets two or more values, the first one is used. A cell without a value is drawn in `ChartStyle.TrackColor` or a neutral gray (black at 7% in light, white at 7% in dark). The cells fill the area left by the names and the legend, with their own width and height |
| Calendar | One column per week and one row per day of the week, starting on the `FirstDayOfWeek` of the culture. The weeks run from the first to the last date of the data, up to the latest 1,000 weeks; days outside that range are not drawn, and days in it without a value are neutral gray. The cells are the largest squares that fit, and the whole calendar with its names and legend is centered. When a day gets two or more values, the first one is used |
| Colors | The range from `Minimum` to `Maximum` is split into 64 steps, and a value takes step `floor(64 × (value − minimum) / range)` (a range of 0 counts as 1), as in Grafana. The colors are spread over the 64 steps with the B-spline of d3 (`interpolateRgbBasis`), opacity included, which gives exactly the colors of Grafana for the Oranges scheme. Values of zero and below are drawn too |
| Gaps | Each cell leaves `CellGap` on its right and bottom, and the edges are rounded to whole pixels |
| Names | A matrix writes the row names on the left (up to a third of the width) and the column names under the cells, centered and thinned when they collide. A calendar writes the month above the week where it starts, when it fits before the next month, and Mon, Wed and Fri on the left. All use `AxisLabelFontSize` and `MutedTextColor` |
| Legend | A 9 DIP bar of the colors, as long as the cells, with values under it (or beside it on the left or the right): the minimum and the maximum at the ends and evenly spaced values between them as far as they fit, rounded to integers when both ends are integers. The steps are drawn side by side without overlapping, so translucent colors keep their opacity |
| Selection and trackball | The cell gets a 2 DIP frame in `PrimaryTextColor` and the other cells keep their colors; the selection moves over 150 ms. The tooltip shows the row and the column (or the date and its day of the week) and then the value with a dot in the cell color; an empty cell shows only the first line |
| Taps | A cell, gap included, raises `ItemTapped` when it has a value, with `null` in `Series`, `-1` in `SeriesIndex`, the column or the date in `X` and the value in `Y`; empty cells and the rest raise `ChartTapped` and clear the selection |
| Animation | The entrance fades the columns in from the left (`Auto`); `Fade` fades all cells together, `Grow` grows the cells from their centers column by column and `Reveal` uncovers them from the left. Updates move the values and the color range when the rows and columns stay the same |
| Allocation | Cells are plain rectangles, so still, selected, tracked and animated frames allocate nothing |
| Density | `Full` shows the legend, `Compact` hides it unless `LegendVisible` is `true`, and `Minimal` keeps only the cells, without names, legend or interaction, with whole-chart taps |

## 🚥 ClamStateTimelineView

Shows how states change over time, one row per entity, such as the status of servers or the history of alerts.  
It follows the Grafana state timeline: value mappings and thresholds choose the text and the color of each state, equal states next to each other are merged, and the options of the Grafana panel are properties with the same defaults.  
Each item is the start of a state, which lasts until the next start in its row.  

### 🧩 Properties

#### Data

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `ItemsSource` | `IEnumerable?` | `null` | ✓ | Data; each item starts a state |
| `ValueAccessors` | `IChartAccessorProvider?` | `null` | ✓ | Accessors (see [Data](#-data)) |
| `RowKey` | `string?` | `null` | ✓ | Key of the row name (text); without it everything is one row |
| `StartKey`, `EndKey` | `string?` | `null` | ✓ | Keys of the start and the optional end of a state (dates) |
| `StateKey` | `string?` | `null` | ✓ | Key of the state (text or a number) |
| `TrackItemChanges` | `bool` | `true` | ✓ | Reads an item again when it raises `PropertyChanged` |

Without `StartKey` and `StateKey` nothing is drawn and a warning is written once.  

#### Shape and colors

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `Minimum`, `Maximum` | `DateTime?` | `null` | ✓ | Time range, like the time range of a Grafana dashboard; `null` takes the first start and the last start or end of the data |
| `ValueMappings` | `IList<ChartValueMapping>` | empty | ✓ | Text and color of state values; `null` throws `ArgumentNullException` |
| `Thresholds` | `IList<ChartThreshold>` | empty | ✓ | Steps of numeric states; `null` throws `ArgumentNullException` |
| `MergeValues` | `bool` | `true` | ✓ | Merges equal states next to each other, and numbers in the same threshold step |
| `ShowValue` | `ChartVisibilityMode` | `Auto` | ✓ | Text of the state on each band; `Auto` writes it where its first two characters fit |
| `AlignValue` | `ChartValueAlignment` | `Left` | ✓ | Alignment of that text |
| `RowHeight` | `double` | `0.9` | ✓ | Share of the height taken by the bands, from 0 to 1 |
| `FillOpacity` | `double` | `0.7` | ✓ | Opacity of the band fill, from 0 to 1 |
| `LineWidth` | `double` | `0` | ✓ | Outline of the bands in their solid color, 0 or more |
| `ValueFormat`, `ValueConverter` | `string?`, `IValueConverter?` | `null` | ✓ | Format of numeric states and of the threshold step names (see [Formats](#formats)); the default is the compact number |
| `LegendVisible` | `bool?` | `null` | ✓ | Shows the states or the threshold steps; `null` shows them in `Full` density |
| `LegendPosition` | `ChartLegendPosition` | `Bottom` | ✓ | Rows at the top or the bottom, or a column on the left or the right |

#### Trackball

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `TrackballMode` | `ChartTrackballMode` | `None` | ✓ | How the trackball starts, as in `ClamChartView` |
| `TooltipVisible` | `bool` | `true` | ✓ | Draws the tooltip |
| `TrackedItem` | `object?` | `null` | ✓ | First item of the state under the finger, OneWayToSource; `null` when released |
| `TrackedIndex` | `int` | `-1` | ✓ | Position of that item in the data, OneWayToSource |
| `TrackingChangedCommand` | `ICommand?` | `null` | ✓ | Executed when tracking starts, moves to another state and ends |

There is no selection, as in Grafana.  

### ChartValueMapping

A value mapping of Grafana for `ValueMappings`.  

| Name | Type | Default | Description |
|---|---|---|---|
| `Value` | `object?` | `null` | State value to match; text is compared as it is and numbers by their invariant text, so `Value="1"` in XAML matches the number 1 |
| `Text` | `string?` | `null` | Text shown for the state; `null` keeps the value |
| `Color` | `Color?` | `null` | Color of the state; `null` uses the color of `Level` |
| `Level` | `ChartThresholdLevel` | `None` | Status color from the style, used when `Color` is `null` |

### 📣 Events

| Name | EventArgs | Description |
|---|---|---|
| `TrackingStarted`, `TrackingChanged`, `TrackingEnded` | `ChartTrackingEventArgs` | The trackball started, moved to another state or ended; `X` is the time at the finger and `Points` is empty |

### 📏 Rules

| Item | Rule |
|---|---|
| States | Items are sorted by start within each row, and each state lasts until the next start, or until its own end when that comes first. An item without a state value ends the previous state. The last state lasts until the end of the range, so pass the current time or the like as `Maximum`, or give it an end; without them the range ends at the last start |
| Merging | With `MergeValues`, states next to each other with the same value, or numbers in the same threshold step, become one band; a gap left by an end keeps them apart |
| Rows | Rows follow the order in which their names appear. As in Grafana, the bands take `RowHeight` of the height split by the rows and the rest is spread between them; a single row sits at the top. Row names are on the left, up to a third of the width, and the time axis at the bottom works like the date and time axis of `ClamChartView` |
| Colors | A mapped value takes its `Color`, otherwise the color of its `Level`. A number takes the color of its threshold step, or without thresholds a green to yellow to red scale between the smallest and the largest number of its row, the default of Grafana. Text without a mapping is green, or the color of the lowest step when thresholds are set. The status colors come from `GoodColor`, `WarningColor` and `CriticalColor` |
| Bands | Filled at `FillOpacity` unless the color has an opacity of its own, outlined inside by `LineWidth` in the solid color, and aligned to pixels |
| Text | The mapped `Text`, otherwise the value with `ValueFormat`; a merged number shows its step name (`ChartThreshold.Label`, otherwise `< 60` or `60+` as in Grafana). It is written in `DataLabelFontSize` in black or white by the band color, 2 DIP inside the band and its outline, cut with an ellipsis when it is longer than the band, and aligned within the visible part of a band that runs out of the range |
| Legend | The threshold steps when thresholds are set and some states are numbers, otherwise the states with text in the order they first appear; numbers on the continuous scale have no legend, as in Grafana |
| Trackball | The state under the finger, or in the nearest row between rows, gets a 20% white overlay as in Grafana, with a hairline at the finger in `TrackballLineColor` and a tooltip of the start and the end, the row name and the state with a dot in its color, and the duration written like Grafana (`2h 30m`) |
| Taps | A band raises `ItemTapped` with `null` in `Series`, `-1` in `SeriesIndex`, the first item of the band, its start in `X` and the number of the state in `Y` (`NaN` for text); elsewhere `ChartTapped` is raised |
| Animation | The entrance reveals the bands from the left (`Auto`); `Fade` fades them in and `Grow` grows them from the middle of their rows. A change of the range moves the shown range, while the bands switch at once |
| Allocation | Still frames and the trackball within one state allocate nothing |
| Density | `Full` shows the legend, `Compact` hides it unless `LegendVisible` is `true`, and `Minimal` keeps only the bands, without names, the axis or interaction, with whole-chart taps |

## 🎨 ChartStyle

`ChartStyle` holds the look of everything except the data, together with the defaults of series widths, opacities and sizes.  
One style can be shared by every chart.  
Treat an assigned style as immutable and create a new one with a `with` expression, or call `Invalidate()` after changing it in place.  
The style is validated when assigned: font sizes, `LegendMarkerSize`, `LineThickness`, `MaxBarWidth` and `MarkerSize` must be positive, the other widths, radii and gaps zero or more, the opacities from 0 to 1, and `MinimalSize` must not exceed `CompactSize`; otherwise `ArgumentException` is thrown.  

| Name | Type | Light | Dark | Description |
|---|---|---|---|---|
| `IsDark` | `bool` | `false` | `true` | Whether the style is for a dark theme; chooses the direction of KPI gradients and the text on them |
| `FontFamily` | `string` | `sans-serif` | `sans-serif` | Primary font of all text; missing characters are resolved through `FallbackFonts` of SkiaExtension |
| `PrimaryTextColor` | `Color` | `#0B0B0B` | `#F0EFEC` | Center value of a pie and the value of a KPI tile |
| `SecondaryTextColor` | `Color` | `#52514E` | `#C3C2B7` | Legend, axis titles, data labels outside the elements, the caption under the center value, and the title, unit and caption of a KPI tile |
| `MutedTextColor` | `Color` | `#898781` | `#898781` | Tick labels, hidden legend items, `+N` of the legend and `EmptyText` |
| `TextHaloColor`, `TextHaloWidth` | `Color`, `double` | `Transparent`, `0` | `Transparent`, `0` | Outline around text |
| `AxisLabelFontSize`, `AxisTitleFontSize` | `double` | `11`, `11` | `11`, `11` | Axis text |
| `AxisLineColor` | `Color` | `#C3C2B7` | `#383835` | Baseline and axis lines |
| `GridLineColor` | `Color` | `#E1E0D9` | `#2C2C2A` | Grid lines |
| `GridLineThickness` | `double` | `0` | `0` | Width of the grid lines; `0` is one physical pixel |
| `LegendFontSize`, `LegendMarkerSize` | `double` | `12`, `8` | `12`, `8` | Legend |
| `DataLabelFontSize` | `double` | `11` | `11` | Data labels and the text on state timeline bands |
| `TooltipBackground` | `Color` | `#F0FCFCFB` | `#F01F1F1D` | Tooltip fill |
| `TooltipTextColor` | `Color` | `#0B0B0B` | `#F0EFEC` | Tooltip text |
| `TooltipBorderColor` | `Color` | `#1A0B0B0B` | `#1AF0EFEC` | Tooltip hairline border |
| `TooltipFontSize`, `TooltipCornerRadius` | `double` | `12`, `0` | `12`, `0` | Tooltip |
| `TrackballLineColor` | `Color` | `#898781` | `#898781` | Trackball line |
| `TrackColor` | `Color?` | `null` | `null` | Part of a bar gauge or a radial bar that is not reached, unlit radial segments and heatmap cells without a value; `null` is a neutral gray (black at 7% in light, white at 7% in dark) |
| `DimmedOpacity` | `double` | `0.35` | `0.35` | Opacity of the elements that are not selected |
| `EmphasisDimmedOpacity` | `double` | `0.35` | `0.5` | Opacity of a sparkline except its emphasized last segment |
| `PressedOverlayColor` | `Color` | `#0F000000` | `#14FFFFFF` | Cover of a pressed chart |
| `SkeletonColor`, `SkeletonHighlightColor` | `Color` | `#ECEBE6`, `#F6F5F2` | `#232321`, `#2C2C2A` | Loading placeholder and the band that sweeps over it |
| `Palette` | `IReadOnlyList<Color>` | `ChartPalettes.DefaultLight` | `ChartPalettes.DefaultDark` | Series colors in a fixed order |
| `HeatmapColorScale` | `IReadOnlyList<Color>` | `ChartPalettes.OrangesLight` | `ChartPalettes.OrangesDark` | Heatmap colors from low to high values, two or more |
| `OtherColor` | `Color` | `#B5B3AA` | `#5C5B56` | Merged slices and series beyond the palette |
| `GoodColor`, `WarningColor`, `SeriousColor`, `CriticalColor` | `Color` | `#56A64B`, `#F2CC0C`, `#FF780A`, `#E02F44` | `#73BF69`, `#FADE2A`, `#FF9830`, `#F2495C` | Status colors used by thresholds: the Grafana named colors green, yellow, orange and red of each theme; `CriticalColor` is the default color of negative sparkline values |
| `PositiveTextColor`, `NegativeTextColor` | `Color` | `#56A64B`, `#E02F44` | `#73BF69`, `#F2495C` | Good and bad differences in KPI tiles, the Grafana green and red |
| `LineThickness` | `double` | `2` | `2` | Default line width |
| `AreaFillOpacity` | `double` | `0.1` | `0.1` | Default fill opacity |
| `BarCornerRadius`, `MaxBarWidth` | `double` | `0`, `24` | `0`, `24` | Default bar corner and width |
| `MarkerSize`, `MarkerKnockout` | `double` | `8`, `2` | `8`, `2` | Default marker diameter and the transparent ring around it |
| `SegmentGap` | `double` | `2` | `2` | Gap between stack segments, grouped bars, slices and the cells of segmented gauges |
| `CompactSize`, `MinimalSize` | `Size` | `320, 200`, `160, 96` | same | Density boundaries: a width or a height at or below a size switches to that density |

### ChartStyles

| Name | Description |
|---|---|
| `ChartStyles.Light` | Style for light screens and the default of `ChartViewBase.ChartStyle` |
| `ChartStyles.Dark` | Style for dark screens |
| `ChartStyles.OverlayLight` | `Light` for charts over light photos and cards: no grid or axis lines, white outline of 1.5 DIP at 80% around text, fill opacity 0.06, placeholder in black at 8% with a white band |
| `ChartStyles.OverlayDark` | `Dark` for charts over maps, dark photos and video: no grid or axis lines, black outline of 1.5 DIP at 80% around text, fill opacity 0.08, placeholder in white at 12% |

The library does not follow the theme of the system by itself.  
An implicit style on the base class with `AppThemeBinding` themes every chart at once.  

```xml
<Style TargetType="clam:ChartViewBase" ApplyToDerivedTypes="True">
    <Setter Property="ChartStyle" Value="{AppThemeBinding Light={x:Static clam:ChartStyles.Light}, Dark={x:Static clam:ChartStyles.Dark}}" />
</Style>
```

### ChartPalettes

The default palettes are eight colors in a fixed order chosen so that neighbors stay distinct for color vision deficiencies.  
Colors belong to series and slices by position, so hiding a series does not change the others, and colors are never reused in a cycle.  

| Name | Colors |
|---|---|
| `ChartPalettes.DefaultLight` | `#2A78D6`, `#EB6834`, `#1BAF7A`, `#EDA100`, `#E87BA4`, `#008300`, `#6250D6`, `#E34948` |
| `ChartPalettes.DefaultDark` | `#3987E5`, `#D95926`, `#199E70`, `#C98500`, `#D55181`, `#008300`, `#9085E9`, `#E66767` |
| `ChartPalettes.GrafanaLight` | `#56A64B`, `#E0B400`, `#3274D9`, `#FF780A`, `#E02F44`, `#A352CC` |
| `ChartPalettes.GrafanaDark` | `#73BF69`, `#F2CC0C`, `#5794F2`, `#FF9830`, `#F2495C`, `#B877D9` |
| `ChartPalettes.RedShadesLight` | `#E02F44`, `#EF3E4E`, `#D21F39`, `#FE4D58`, `#B81B37`, `#FF5C62`, `#9E1833`, `#FF6A6B`, `#84142E`, `#FF7D79`, `#6A1027` |
| `ChartPalettes.RedShadesDark` | `#F2495C`, `#FF5866`, `#F02D49`, `#FF6771`, `#EE1136`, `#FF767B`, `#D30F35`, `#FF8484`, `#B70D33`, `#FF9793`, `#9B0B2F` |
| `ChartPalettes.OrangeShadesLight` | `#FF780A`, `#FF8E19`, `#EB6300`, `#FFA428`, `#CD5100`, `#FFB937`, `#B04000`, `#FFCE45`, `#923100`, `#FFE454`, `#742400` |
| `ChartPalettes.OrangeShadesDark` | `#FF9830`, `#FFAD3F`, `#FF8312`, `#FFC14E`, `#F36D00`, `#FFD65D`, `#D65A00`, `#FFEA6B`, `#B84800`, `#FFFF7A`, `#9A3800` |
| `ChartPalettes.YellowShadesLight` | `#F2CC0C`, `#FFE11B`, `#D6AE0B`, `#FFF72A`, `#B99209`, `#FFFF39`, `#9D7808`, `#FFFF47`, `#815F06`, `#FFFF56`, `#644705` |
| `ChartPalettes.YellowShadesDark` | `#FADE2A`, `#FFF339`, `#F9D30D`, `#FFFF48`, `#E3B905`, `#FFFF57`, `#C69C05`, `#FFFF65`, `#A98004`, `#FFFF74`, `#8C6703` |
| `ChartPalettes.GreenShadesLight` | `#56A64B`, `#62B55A`, `#4E9142`, `#6FC469`, `#457D38`, `#7BD378`, `#3B692F`, `#87E186`, `#315426`, `#95F097`, `#26401D` |
| `ChartPalettes.GreenShadesDark` | `#73BF69`, `#80CE78`, `#62B654`, `#8CDD87`, `#57A647`, `#99EC96`, `#4E913E`, `#A4FAA4`, `#457C35`, `#B3FFB5`, `#3B672C` |
| `ChartPalettes.BlueShadesLight` | `#3274D9`, `#417EE8`, `#256AC8`, `#5089F7`, `#2061AF`, `#5F93FF`, `#1C5696`, `#6D9CFF`, `#174B7D`, `#7CA7FF`, `#123E64` |
| `ChartPalettes.BlueShadesDark` | `#5794F2`, `#669FFF`, `#3B87F0`, `#75A9FF`, `#207CEE`, `#84B4FF`, `#1173DE`, `#92BEFF`, `#0F6AC3`, `#A1C8FF`, `#0D5FA7` |
| `ChartPalettes.PurpleShadesLight` | `#A352CC`, `#B561DB`, `#933BC5`, `#C870EA`, `#7F34AF`, `#DA7FF9`, `#6B2D98`, `#EC8DFF`, `#582681`, `#FE9CFF`, `#461F6A` |
| `ChartPalettes.PurpleShadesDark` | `#B877D9`, `#CA86E8`, `#A960D2`, `#DB95F7`, `#9848CC`, `#EDA4FF`, `#8636C1`, `#FEB2FF`, `#732FAA`, `#FFC1FF`, `#602992` |
| `ChartPalettes.OrangesLight` | `#FFF5EB`, `#FEE6CE`, `#FDD0A2`, `#FDAE6B`, `#FD8D3C`, `#F16913`, `#D94801`, `#A63603`, `#7F2704` |
| `ChartPalettes.OrangesDark` | The same colors in reverse order |

`GrafanaLight` and `GrafanaDark` are the first six colors of the classic Grafana palette (green, semi-dark yellow, blue, orange, red and purple) in each theme.  
The shades are the shades of Grafana: the named color of each hue in each theme followed by five pairs of a lighter and a darker shade (hue turned by up to 10° and brightness changed by up to 35%), eleven colors in all; the twelfth series uses `OtherColor`.  
In XAML, choose the shades by the theme with `{AppThemeBinding Light={x:Static clam:ChartPalettes.BlueShadesLight}, Dark={x:Static clam:ChartPalettes.BlueShadesDark}}`.  
They are not checked for color vision deficiencies, so pair them with labels or a legend.  
`OrangesLight` and `OrangesDark` are the Oranges scheme of d3 that the Grafana heatmap uses by default, from low to high values; the dark theme reverses it so that large values are light. They are meant for `ColorScale` of a heatmap, not for series.  
Any `IReadOnlyList<Color>` works as a palette, on a view through `Palette` or for every chart through `ChartStyle.Palette`; in XAML, list the colors in `x:Array Type="{x:Type Color}"`. The `AppPalettes.Material` of the Example (the 400 tones of five Material Design colors for both themes) is such an application palette.  

## 🖌 ChartDrawing

Draws on any `SKCanvas` without a view, for custom canvases, list cells, widgets and images made in the background; it touches no MAUI object and can run on any thread.  

| Method | Description |
|---|---|
| `DrawSparkline(SKCanvas canvas, SKRect bounds, ReadOnlySpan<double> values, in SparklineDrawOptions options)` | Draws a sparkline in `bounds` (canvas units) as `ClamSparklineView` does, without animations or the transparent ring around markers |
| `DrawRadialBar(SKCanvas canvas, SKRect bounds, ReadOnlySpan<double> values, in RadialBarDrawOptions options)` | Draws one ring per value from the outside in, fitted in `bounds` as `ClamRadialBarView` does, without text; returns the square inside the innermost ring for your own center text, or an empty rectangle when nothing is drawn |
| `DrawBarGauge(SKCanvas canvas, SKRect bounds, ReadOnlySpan<double> values, in BarGaugeDrawOptions options)` | Draws one bar per value as `ClamBarGaugeView` does, without text; the bars share `bounds` evenly with `Spacing` between them, as rows when horizontal and as columns when vertical |
| `DrawBarGauge(SKCanvas canvas, SKRect bounds, ReadOnlySpan<double> values, ReadOnlySpan<double> targets, in BarGaugeDrawOptions options)` | Also draws a target line at `targets[i]` on each bar; a missing or `NaN` target draws none, and the line stays inside `bounds` |

Rings and bars look the same as the views in `Full` density, glow included. Values outside the range are clamped, and options that make no shape, such as a zero thickness, draw nothing instead of throwing.  

| `SparklineDrawOptions` | Type | Default | Description |
|---|---|---|---|
| `Kind` | `ChartSparklineKind` | `Line` | Shape |
| `Color` | `SKColor` | `#2A78D6` | Line, fill and bars |
| `NegativeColor` | `SKColor` | `#E02F44` | Negative bars and losses |
| `Thickness` | `float` | `2` | Line width |
| `AreaOpacity` | `float` | `0.1` | Fill opacity of `Area` |
| `Smooth` | `bool` | `false` | Monotone cubic interpolation |
| `Markers` | `ChartSparklineMarkers` | `Last` | Points that get a marker on lines and areas |
| `Minimum`, `Maximum` | `double?` | `null` | Fixed range |

| `RadialBarDrawOptions` | Type | Default | Description |
|---|---|---|---|
| `Minimum`, `Maximum` | `double` | `0`, `100` | Range of the values |
| `StartAngle`, `SweepAngle` | `float` | `0`, `360` | Clock angle where the rings start, and their sweep up to 360 |
| `InnerRadius` | `float` | `0.4` | Inner edge of the area the automatic thickness fills, as a fraction of the radius, from 0 to 0.95 |
| `Thickness` | `float?` | `null` | Thickness of each ring; `null` divides the area outside `InnerRadius`, up to 1/6 of the radius |
| `Spacing` | `float` | `4` | Space between the rings |
| `CapStyle` | `ChartCapStyle` | `Flat` | Ends of the rings and the track |
| `DisplayMode` | `ChartRadialBarMode` | `Basic` | `Basic`, `Gradient` or `Segmented` |
| `SegmentCount`, `SegmentGap` | `int`, `float` | `20`, `2` | Cells of a ring in `Segmented` and the gap between them; fewer cells on short rings |
| `TrackVisible`, `TrackColor` | `bool`, `SKColor?` | `true`, `null` | Draws the rest of each ring; `null` uses the track color of the theme |
| `BarGlow` | `bool` | `false` | Makes the value of each ring glow as `ClamRadialBarView.BarGlow` does |
| `ThresholdMarkersVisible` | `bool` | `false` | Draws the threshold ring as `ClamRadialBarView.ThresholdMarkersVisible` does when `Thresholds` has entries |
| `OverflowMode` | `ChartOverflowMode` | `Clamp` | Values over the maximum; `Wrap` draws the next lap darker |
| `Colors` | `IReadOnlyList<SKColor>?` | `null` | Color of each ring from the outside; `null` uses the default palette, and rings past the list are gray |
| `Thresholds` | `IReadOnlyList<ChartDrawingThreshold>?` | `null` | Colors by the percentage of the range |
| `IsDark` | `bool` | `false` | Uses the dark defaults for the palette, the track, the unlit cells and the gradient |

| `BarGaugeDrawOptions` | Type | Default | Description |
|---|---|---|---|
| `Minimum`, `Maximum` | `double` | `0`, `100` | Range of the values and the targets |
| `Orientation` | `ChartOrientation` | `Horizontal` | `Horizontal` stacks rows and `Vertical` places columns |
| `DisplayMode` | `ChartBarGaugeMode` | `Basic` | `Basic`, `Gradient` or `Segmented` |
| `SegmentWidth`, `SegmentGap` | `float` | `12`, `2` | Width of a cell in `Segmented` and the gap between cells |
| `Spacing` | `float` | `12` | Space between the bars |
| `TrackVisible`, `TrackColor` | `bool`, `SKColor?` | `true`, `null` | Draws the part that is not reached; `null` uses the track color of the theme |
| `Color` | `SKColor?` | `null` | Color of the bars; `null` uses the first color of the default palette |
| `Thresholds` | `IReadOnlyList<ChartDrawingThreshold>?` | `null` | Colors by the percentage of the range |
| `IsDark` | `bool` | `false` | Uses the dark defaults for the palette, the track and the target line |

`ChartDrawingThreshold(double Value, SKColor Color)` is a step of `Thresholds` with its color already chosen: `Value` is a percentage of the range as in `ChartThreshold`, and `Double.NegativeInfinity` makes the base step.  

## 🔧 ClamChartSettings

Settings for every chart in the application, read when an animation starts.  

| Name | Type | Default | Description |
|---|---|---|---|
| `AnimationEnabled` | `bool` | `true` | `false` turns every chart animation off, for kiosks, slow devices and automated screenshots |
| `AnimationScale` | `double` | `1` | Multiplies every duration |
| `RespectSystemAnimationScale` | `bool` | `true` | Follows the animator duration scale of Android: `0` turns the animations off and other values multiply the durations; animations are also off while Android disables animators, as in battery saver |

## 🧩 Data

### Passing data

| Way | Data | Suits |
|---|---|---|
| Values | `IEnumerable<double>` (other numeric types too); X is the position from 0 | Sparklines and simple trends |
| Models | `ChartPoint`, `ChartTimePoint` and `ChartEntry` | Data built in the view model, pies |
| Any type | Items in `ItemsSource`, a `ChartAccessorCollection<T>` in `ValueAccessors` and keys such as `XKey` and `YKey` | Existing models and DTOs as they are |

A value is read from the first that applies: the selector delegate (`ValueSelector`, `LabelSelector`), the accessor of the key, the model types, and a number.  
An item that matches none of them throws `InvalidOperationException` when the data is processed, never while drawing.  
The data is processed with the other changes of the same UI loop, so the order in which XAML or bindings set the properties does not matter, and a frame drawn before that keeps the previous data.  
Missing Y values (`null` and `NaN`) follow `EmptyPointMode`.  
No reflection is used, so the library is trimmable and AOT compatible.  

### ChartAccessorCollection&lt;T&gt;

Typed getters registered by key with a collection initializer.  
It implements `IChartAccessorProvider`, whose members are internal, and reads each value type without boxing.  

```csharp
public sealed class MonthlySales
{
    public static ChartAccessorCollection<MonthlySales> Accessors { get; } = new()
    {
        { "month", static x => x.Month },
        { "sales", static x => x.Sales },
        { "target", static x => x.Target }
    };

    public required string Month { get; init; }

    public double Sales { get; set; }

    public double? Target { get; set; }
}
```

| Name | Returns | Description |
|---|---|---|
| `Add(string key, Func<T, double> getter)` | `void` | Number |
| `Add(string key, Func<T, double?> getter)` | `void` | Number where `null` is a missing value |
| `Add(string key, Func<T, DateTime> getter)` | `void` | Date and time |
| `Add(string key, Func<T, DateTimeOffset> getter)` | `void` | Date and time with an offset |
| `Add(string key, Func<T, string> getter)` | `void` | Text, such as a category or a name |
| `Add(string key, Func<T, Color> getter)` | `void` | Color of a slice |
| `ContainsKey(string key)` | `bool` | Whether the key is registered |
| `Count` | `int` | Number of keys |

A key whose type does not fit its use, such as a text getter for `YKey`, throws `InvalidOperationException` when the data is processed.  
The type of X chooses the automatic axis: text a `CategoryAxis`, dates a `DateTimeAxis` and numbers a `NumericAxis`.  

### Models

| Type | Members | Description |
|---|---|---|
| `ChartPoint` | `X` (`double`), `Y` (`double`) | Point on a numeric X axis |
| `ChartTimePoint` | `X` (`DateTime`), `Y` (`double`) | Point on a date and time axis |
| `ChartEntry` | `Label` (`string?`), `Value` (`double`), `Color` (`Color?`), `Maximum`, `Target` (`double?`), the last three optional | Named value, such as a slice, a category or a bar gauge item |

They are sealed records, so `TrackedItem` and `SelectedItem` can be bound to typed view model properties.  

### Change tracking

| Change | Behavior |
|---|---|
| `ItemsSource` replaced | The data is read again; the same number of points morphs and a different number plays the entrance again |
| `INotifyCollectionChanged` add and remove | Applied by position: an item added at the end grows from the previous point, the first item flows out |
| `INotifyCollectionChanged` replace and move | The values at those positions move |
| `INotifyCollectionChanged` reset | Same as a replaced `ItemsSource` |
| `INotifyPropertyChanged` of an item | With `TrackItemChanges`, only that item is read again |

Calling `Add` and `RemoveAt(0)` on an `ObservableCollection` one after the other makes a streaming chart: both changes are handled in one frame and the line flows to the left.  
Collections are observed only while the view has a handler, so a long-lived view model does not keep a detached view alive.  

### Formats

Every number and date shown as text has three levels of format, as in the other Clam libraries.  

| Priority | Property | Type | Bindable |
|:-:|---|---|:-:|
| 1 | `XxxFormatter` | `Func<double, string>?` (the axis value type for axes) | |
| 2 | `XxxConverter` | `IValueConverter?` | ✓ |
| 3 | `XxxFormat` | `string?`: composite when it contains `{` (`{0:N0} kWh`), otherwise a format string (`N0`) | ✓ |

The default number format is compact: under 10,000 the value is shown with group separators and up to two decimals (`1,284`, `0.25`), and from 10,000 it is rounded to three significant digits with `K`, `M`, `B` or `T` (`12.9K`, `4.2M`, `1.25B`).  
Separators follow `Culture` and the suffixes stay in English.  

## 📣 Event arguments

### ChartItemEventArgs

| Name | Type | Description |
|---|---|---|
| `Series` | `ChartSeries?` | Series of the element; `null` for a pie, a heatmap and a state timeline |
| `SeriesIndex` | `int` | Position of the series; `-1` for a pie, a heatmap and a state timeline |
| `Index` | `int` | Position of the item in the data; `-1` for the merged slice |
| `Item` | `object?` | Item of the element; `null` for the merged slice |
| `X` | `object?` | X of the point: `string?` for categories, `DateTime` for dates and `double` for numbers and positions; the name of a slice |
| `Y` | `double` | Original value, or the merged total; `NaN` for a state given as text |
| `Position` | `Point` | Tap position in the view |
| `Handled` | `bool` | Skips the default action and the command |

### ChartTappedEventArgs

| Name | Type | Description |
|---|---|---|
| `Position` | `Point` | Tap position in the view |
| `Handled` | `bool` | Skips the default action (clearing the selection) and the command |

### ChartLegendEventArgs

| Name | Type | Description |
|---|---|---|
| `Series` | `ChartSeries?` | Series of the legend item in `ClamChartView` and `ClamRadarChartView` |
| `Item` | `object?` | Item of the legend entry in `ClamPieChartView` |
| `Index` | `int` | Position of the series or the item |
| `Handled` | `bool` | Skips the toggle or the selection and the command |

### ChartSelectionChangedEventArgs

| Name | Type | Description |
|---|---|---|
| `OldItem`, `NewItem` | `object?` | Items before and after the change |
| `OldIndex`, `NewIndex` | `int` | Positions before and after the change; `-1` when nothing is selected |

### ChartTrackingEventArgs

| Name | Type | Description |
|---|---|---|
| `Index` | `int` | Position of the tracked item; `-1` when tracking ends |
| `Item` | `object?` | Tracked item |
| `X` | `object?` | X of the trackball, typed as in `ChartItemEventArgs` |
| `Points` | `IReadOnlyList<ChartTrackingInfo>` | Point of each series at that X, or only the tracked point with the crosshair of scatter series; empty when tracking ends |

### ChartTrackingInfo

A struct with the point of one series under the trackball, also passed to `TooltipFormatter`.  

| Name | Type | Description |
|---|---|---|
| `Series` | `ChartSeries` | Series |
| `SeriesIndex` | `int` | Position of the series |
| `Index` | `int` | Position of the item in the data of the series |
| `Item` | `object?` | Item |
| `X` | `object?` | X, typed as in `ChartItemEventArgs` |
| `Y` | `double` | Original value |

### ChartFrameEventArgs

| Name | Type | Description |
|---|---|---|
| `ElapsedMilliseconds` | `double` | Time spent drawing the frame |
| `PointCount` | `int` | Number of points drawn |
| `Animating` | `bool` | Whether an animation is running |

## 🧭 Enumerations

| Type | Value | Description |
|---|---|---|
| `ChartDensity` | `Auto` | Chosen from the size of the view |
| | `Full`, `Compact`, `Minimal` | Fixed level of detail |
| `ChartTouchMode` | `Auto` | Chosen from the settings |
| | `Interactive` | Takes every touch |
| | `HitOnly` | Takes touches on data elements and legend items in use |
| | `PassThrough` | Takes no touches |
| `ChartEntranceAnimation` | `Auto` | Chosen by the kind of chart |
| | `None` | No entrance |
| | `Fade` | Opacity rises; scatter points also grow from 60% of their size |
| | `Grow` | Values grow from the baseline; a pie widens from `StartAngle`, a radar from the center, heatmap cells from their centers and state timeline bands from the middle of their rows |
| | `Reveal` | Drawn from the start of the X axis; a pie is revealed from `StartAngle`, a radar clockwise from 12 o'clock and a heatmap and a state timeline from the left |
| `ChartOrientation` | `Vertical` | X along the bottom; for `TitleOrientation`, a title turned by 90° |
| | `Horizontal` | X along the left, first at the top; for `TitleOrientation`, a horizontal title |
| `ChartStackMode` | `Normal` | Stacks the values |
| | `Percent` | Stacks the shares to 100% |
| `ChartLegendPosition` | `Top`, `Bottom`, `Left`, `Right` | Place of the legend |
| `ChartLegendMode` | `List` | Markers and names in rows or a column |
| | `Table` | A table of names and values |
| `ChartLegendSortKey` | `Name` | Sorts a legend table by the names |
| | `Last`, `Min`, `Max`, `Mean` | Sorts a legend table by that value column |
| `ChartLegendValues` | `None`, `Last`, `Min`, `Max`, `Mean` | Flags for the value columns of a legend table |
| `ChartDownsampling` | `None` | Draws every point |
| | `Auto` | Thins dense lines and areas for drawing |
| `ChartDataLabelsMode` | `None` | No data labels |
| | `Last` | The last point of each series |
| | `MinMax` | The largest and the smallest point of each series |
| | `All` | Every point; not recommended for dense data |
| `ChartSelectionMode` | `None` | No selection |
| | `Item` | One point, bar, slice, radar vertex or heatmap cell |
| | `Category` | The column of one X (one slice in a pie, one item on every series of a radar, one cell in a heatmap) |
| `ChartTrackballMode` | `None` | No trackball |
| | `LongPress` | Starts after a long press |
| | `Touch` | Starts at once |
| `ChartTrackingGroupMode` | `Crosshair` | A following chart draws the line and the markers |
| | `Tooltip` | A following chart also draws its tooltip |
| `ChartAxisTarget` | `Primary`, `Secondary` | Value axis of a series |
| `ChartAxisPosition` | `Auto` | Y on the left and X at the bottom |
| | `Opposite` | The other side |
| `ChartTickAlignment` | `AlignToPrimary` | Secondary ticks at the primary ticks |
| | `Independent` | Secondary ticks of its own |
| `ChartDateTimeIntervalType` | `Auto` | Chosen from the width |
| | `Seconds`, `Minutes`, `Hours`, `Days`, `Weeks`, `Months`, `Years` | Unit of the ticks |
| `ChartLabelPlacement` | `BetweenTicks` | Each category centered in its band |
| | `OnTicks` | Categories on the ticks from edge to edge |
| `ChartLineInterpolation` | `Linear` | Straight lines |
| | `Smooth` | Monotone cubic curve that does not overshoot the data |
| | `Step` | Keeps the value until the next point |
| | `StepBefore` | Moves to the next value first and keeps it until the next point |
| `ChartMarkerMode` | `Auto` | The last point and the selected point |
| | `None` | No markers |
| | `All` | Every point |
| `ChartFillMode` | `Solid` | Even fill |
| | `Gradient` | Fades out toward the baseline |
| `ChartEmptyPointMode` | `Gap` | Breaks the line and skips the point |
| | `Zero` | Treats the point as 0 |
| | `Interpolate` | Interpolates between the neighbors along X |
| `ChartSortMode` | `None` | Data order |
| | `Descending` | Largest first |
| | `Ascending` | Smallest first |
| `ChartPieLabelsMode` | `None` | No labels |
| | `Inside` | Labels inside the slices that fit |
| | `Outside` | Labels in columns beside the pie with leader lines |
| `ChartPieLabelContent` | `Percent`, `Value`, `Label`, `LabelAndPercent` | Text of a slice label |
| `ChartLegendValueMode` | `None`, `Value`, `Percent` | Value added to the legend entries of a pie |
| `ChartCenterValueMode` | `Auto` | The selected slice or ring, otherwise the total or `CenterValue` of a pie and the outermost ring of a radial bar |
| | `Total` | Always the total or `CenterValue` |
| | `Selected` | Only the selected slice or ring, nothing while none is selected |
| | `None` | Nothing in the center |
| `ChartSparklineKind` | `Line`, `Area`, `Bar`, `WinLoss` | Shape of a sparkline |
| `ChartSparklineMarkers` | `None`, `First`, `Last`, `High`, `Low` | Flags of the points with a marker |
| `ChartThresholdLevel` | `None`, `Good`, `Warning`, `Serious`, `Critical` | Level of a threshold |
| `ChartThresholdsMode` | `None` | Thresholds are not drawn |
| | `Lines` | Dashed lines at the thresholds |
| | `Bands` | Tinted steps between the thresholds |
| | `ColorSeries` | Series colored by the thresholds |
| `ChartAnnotationAxis` | `Y` | On a value axis |
| | `X` | On the X axis |
| `ChartMarkerShape` | `Circle`, `Square`, `Diamond` | Shape of line markers and scatter points |
| `ChartSparklineEmphasis` | `None` | Every segment in the series color |
| | `Last` | Only the last segment in full color |
| `ChartDeltaMode` | `Percent` | Ratio to the size of the base |
| | `Absolute` | Difference of the values |
| `ChartTrendPlacement` | `Bottom` | Under the text |
| | `Background` | Behind the text in the bottom of the tile, edge to edge |
| | `Right` | Right of the text |
| `ChartStatColorMode` | `None` | Ink colors without a state |
| | `Value` | Value, unit and trend in the tile color |
| | `Indicator` | State icon and a band at the left edge |
| | `Background` | Tile tinted with the state color |
| | `BackgroundGradient` | Gradient of the tile color with text in light or dark ink by its brightness |
| `ChartStatLayoutMode` | `Auto` | Horizontal when the width is at least 2.5 times the height |
| | `Vertical`, `Horizontal` | Fixed arrangement |
| `ChartBarGaugeMode` | `Basic` | Bar color at 35% with a line at the end |
| | `Gradient` | Gradient of the threshold colors up to the value |
| | `Segmented` | Cells lit up to the value, like an LCD |
| `ChartBarGaugeLabelPosition` | `Top` | Name above the bar |
| | `Left` | Names in a column on the left |
| | `None` | No names |
| `ChartBarGaugeValueMode` | `Value` | The value |
| | `Percent` | Percentage of the maximum |
| | `ValueOfMaximum` | The value and the maximum (`72 / 100`) |
| `ChartBarGaugeValueColorMode` | `Bar` | The bar color, or the threshold color at the value |
| | `Text` | `PrimaryTextColor` |
| `ChartRadialBarMode` | `Basic` | One color |
| | `Gradient` | Gradient of the threshold colors or of shades of the ring color |
| | `Segmented` | Cells lit before the value |
| `ChartCapStyle` | `Flat` | Square ends |
| | `Round` | Round ends |
| `ChartOverflowMode` | `Clamp` | Stops at the maximum |
| | `Wrap` | Draws the rest of the value over the first lap in a darker color |
| `ChartRadarGridShape` | `Polygon` | Rings as polygons through the axes |
| | `Circle` | Rings as circles |
| `ChartHeatmapLayoutMode` | `Matrix` | Rows and columns from the data |
| | `Calendar` | One column per week and one row per day of the week |
| `ChartVisibilityMode` | `Auto` | Shown where it fits |
| | `Always`, `Never` | Always or never shown |
| `ChartValueAlignment` | `Left`, `Center`, `Right` | Alignment of a text |

## 🔤 Fonts

Per character font fallback comes from [SkiaExtension](https://github.com/usausa/skia-extension).  
Configure it with `FallbackFonts` in the `SkiaExtension` namespace at startup, before the first chart is shown.  
`Languages` defaults to the UI language of the device.  

```csharp
FallbackFonts.Languages = ["ja", "en"];
FallbackFonts.Typefaces = [SKTypeface.FromStream(await FileSystem.OpenAppPackageFileAsync("NotoSansJP-Regular.ttf"))];
```
