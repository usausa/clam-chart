# ClamChart API reference

## 📖 ChartViewBase

`ClamChartView`, `ClamPieChartView` and `ClamSparklineView` derive from `ChartViewBase`, which derives from `SKCanvasView`.  
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

### 🛠 Methods

| Name | Returns | Description |
|---|---|---|
| `Invalidate()` | `void` | Validates `ChartStyle` again, recreates the renderer and redraws; call it after changing the assigned style in place |
| `PlayEntranceAnimation()` | `void` | Plays the entrance animation again, for example when a tab is shown again |
| `CompleteAnimations()` | `void` | Moves the running animations to their final state |
| `CancelInteraction()` | `void` | Cancels a press, a long press or the trackball in progress |
| `Dispose()` | `void` | Stops the animations and the input and releases the subscriptions and native resources |

### 📣 Events

| Name | Arguments | Description |
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

## 📈 ClamChartView

Draws line, area and bar series on X and Y axes, mixed in one chart, stacked, horizontal or on a date and time axis.  
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
| `LegendToggleEnabled` | `bool` | `false` | ✓ | Tapping a legend item shows or hides its series; the last visible series cannot be hidden |
| `LegendItemTappedCommand` | `ICommand?` | `null` | ✓ | Executed after `LegendItemTapped` |
| `DataLabelsMode` | `ChartDataLabelsMode` | `None` | ✓ | Points that get a value label |

The legend lists the series with a `Title`, hidden ones included, each with a dot or a short line for line series.  
Rows wrap and more than two rows end with `+N`.  
A column on the left or the right is as wide as the longest item up to 180 DIP and is hidden when the plot would be narrower than twice its width.  

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
| Line and area point | Halfway to the neighboring points along X and 22 DIP from the point along Y, the fill included; the nearer point wins |
| Bar | The whole column of the category, split evenly between grouped bars |
| `Category` selection | The whole column of the category |

#### Trackball

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `TrackballMode` | `ChartTrackballMode` | `None` | ✓ | How the trackball starts |
| `TooltipVisible` | `bool` | `true` | ✓ | Draws the tooltip; `false` keeps only the line and the markers, for showing the values elsewhere through `TrackedItem` |
| `TooltipFormatter` | `Func<ChartTrackingInfo, string>?` | `null` | | Text of each series row after its colored dot; `null` is the title and the formatted value; called once per point until the layout changes |
| `TrackedItem` | `object?` | `null` | ✓ | Item at the trackball (the first series wins at the same X), OneWayToSource; `null` when released |
| `TrackedIndex` | `int` | `-1` | ✓ | Position of that item in the data of its series, OneWayToSource |
| `TrackingChangedCommand` | `ICommand?` | `null` | ✓ | Executed when tracking starts, moves to another point and ends |

| `TrackballMode` | Start | Suits |
|---|---|---|
| `LongPress` | Pressing 500 ms within 8 DIP; the parent stops scrolling | A chart in a `ScrollView` or a `CollectionView` |
| `Touch` | At once; a first move of 8 DIP that is at least 1.25 times more vertical than horizontal goes to the parent scroll | A screen where the chart is the main content |

The trackball snaps to the X of the nearest point of all visible series, and each series shows its point at that X.  
It draws a hairline in `TrackballLineColor`, markers on line and area points and, when `TooltipVisible` is `true`, a tooltip 48 DIP above the finger (below it when there is no room) kept inside the view.  
The first line of the tooltip is the X label.  
On a `DateTimeAxis` with the default format it uses the finer of the tick precision and the precision of the point times, with the date added when the range spans days (`10/9 14:00`).  
The following rows show the title and the value of each series, formatted by the series or the value axis; 100% stacks show the original values.  
A data or layout change during tracking snaps again at the finger.  
Tracking ends when the finger is released, a second finger touches, the finger leaves the view or `CancelInteraction()` is called, and is canceled when the view is resized, unloaded, disabled, hidden or loses focus, the handler changes, or `TouchMode` or `TrackballMode` changes.  

### 📣 Events

| Name | Arguments | Description |
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

A marker is cut out of the line by `ChartStyle.MarkerKnockout`, which is really transparent, and points clipped by the plot get no marker.  

### AreaSeries

Derives from `LineSeries`.  

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `FillOpacity` | `double?` | `null` | ✓ | Opacity of the fill from 0 to 1; `null` uses `ChartStyle.AreaFillOpacity` |
| `FillMode` | `ChartFillMode` | `Solid` | ✓ | `Gradient` fades the fill out toward the baseline |
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

A value axis with bars includes 0 unless `IncludeZero` is `false`, and bars on a numeric or date and time X axis widen the range by half of the smallest gap so that the end bars are not cut.  

## 📏 Axes

### ChartAxis

Base of all axes, an `Element` set to `XAxis`, `YAxis` or `SecondaryYAxis`.  

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `IsVisible` | `bool?` | `null` | ✓ | `null` follows the density, which hides the axes in `Minimal` |
| `Title` | `string?` | `null` | ✓ | Title shown in `Full` density, on a row above the plot for a vertical axis and outside the labels for a horizontal axis |
| `LabelsVisible` | `bool` | `true` | ✓ | Draws the tick labels |
| `LabelFormat` | `string?` | `null` | ✓ | Format of the labels: composite when it contains `{`, otherwise a format string |
| `LabelConverter` | `IValueConverter?` | `null` | ✓ | Converts the tick value of the axis type to its text; used instead of `LabelFormat` |
| `MaxLabelCount` | `int?` | `null` | ✓ | Largest number of labels, 1 or more; `null` fits them to the length (about one per 64 DIP on X and 40 DIP on Y) |
| `GridLinesVisible` | `bool?` | `null` | ✓ | `null` draws grid lines for the value axis only |
| `Position` | `ChartAxisPosition` | `Auto` | ✓ | `Opposite` moves the axis to the other side; the secondary value axis is always opposite the primary |
| `IsInversed` | `bool` | `false` | ✓ | Reverses the direction |

Each axis adds a `LabelFormatter` of its own value type, used before `LabelConverter` and `LabelFormat`.  
Overlapping labels are thinned to every k-th label, keeping both ends.  
Labels of a vertical axis take up to a third of the width and longer ones end with `…`; labels of a horizontal axis are not shortened.  

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
| `LabelFormatter` | `Func<DateTime, string>?` | `null` | | Label text from code |

`Auto` chooses the smallest step whose labels fit: 1, 2, 5, 10, 15 or 30 seconds or minutes, 1, 3, 6 or 12 hours, 1 or 2 days, 1 or 2 weeks, 1, 2, 3 or 6 months, or 1, 2, 5, 10, 20, 50 and more years.  
Ticks follow the calendar: weeks start on `FirstDayOfWeek` of the culture and months on January.  
The default format follows the step: `HH:mm:ss` under a minute, `HH:mm` under a day, `M/d` under two months and `yyyy/M` above, with the separators of the culture.  
Times are local: a `DateTimeOffset` uses `LocalDateTime`, a UTC `DateTime` is converted and other `DateTime` values are used as they are; the same applies to `Minimum` and `Maximum`.  

### CategoryAxis

| Name | Type | Default | Bindable | Description |
|---|---|---|:-:|---|
| `LabelPlacement` | `ChartLabelPlacement` | `BetweenTicks` | ✓ | Where the categories sit |
| `LabelFormatter` | `Func<string, string>?` | `null` | | Label text from code |

Categories merge the X values of all series in order of appearance, hidden series included, so the axis does not change when a series is toggled.  
`LabelFormat` applies only composite formats (`{0} st`).  

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
| `LabelsMode` | `ChartPieLabelsMode` | `None` | ✓ | `Inside` draws the labels that fit in their slices |
| `LabelContent` | `ChartPieLabelContent` | `Percent` | ✓ | Text of the labels |
| `LegendVisible` | `bool?` | `null` | ✓ | `null` shows the legend in `Full` density |
| `LegendPosition` | `ChartLegendPosition` | `Bottom` | ✓ | Rows at the top or the bottom, or a column on the left or the right centered vertically |
| `LegendValueMode` | `ChartLegendValueMode` | `None` | ✓ | Adds the value or the percentage, right-aligned in a column and after the name in rows |
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

| Name | Arguments | Description |
|---|---|---|
| `LegendItemTapped` | `ChartLegendEventArgs` | A legend item was tapped; then, unless handled, the slice is selected when `SelectionMode` is set, or cleared when it is already selected |
| `SelectionChanged` | `ChartSelectionChangedEventArgs` | The selection changed by a tap or a property |

### 📏 Rules

| Item | Rule |
|---|---|
| Values | Finite values greater than 0 become slices; negative values are skipped and reported once through `Trace.TraceWarning` |
| Merged slice | Items over `MaxSlices` (the smallest first) and below `OthersThreshold` become one last slice named `OthersLabel` in `ChartStyle.OtherColor`; a single item is never merged |
| Order | `None` keeps the data order and `Descending` orders by value, equal values in data order |
| Colors | An explicit color (`ColorKey`, `ChartEntry.Color`) first, otherwise the palette in the order of the drawn slices; slices beyond the palette use `OtherColor` with a warning |
| Placement | The swept sector, hole included, is scaled to the largest size that fits outside the legend; the upper half sits on the bottom edge |
| Gaps | Parallel gaps of `SliceGap` between neighboring slices, none at the ends of a partial sweep or around a single full slice |
| Labels | At the middle angle of the slice, centered in the ring of a donut or at 0.62 of the radius of a pie, drawn only when they fit with 2 DIP of margin, in white or `#0B0B0B` by WCAG contrast; percentages use `P0` |
| Center value | Drawn when `InnerRadius` is greater than 0, shrunk to fit the hole (not drawn under 6 DIP); a half donut puts it on the flat side; switching the selection crossfades it over 150 ms |
| Taps | The sector of each slice, gaps included, from the hole to the edge, at its moved position when selected; a tap on the merged slice raises only `ItemTapped` with `Item = null`, `Index = -1` and the merged total; in `Interactive` mode a tap in the center or outside the slices clears the selection |
| Density | `Full` shows the legend, `Compact` hides it unless `LegendVisible` is `true`, `Minimal` keeps the donut and the center value |
| Updates | Slices are matched by data position and the merged slice as one; added slices open from the border of the previous slice, removed and merged ones close at the new border, and colors that change with the order move between the two colors |

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

| Item | Rule |
|---|---|
| Range | Lines and areas span the values, bars include 0 and win-loss uses only the sign; equal values are widened by 1 on each side, and an inverted `Minimum` / `Maximum` falls back to the automatic range with a warning |
| Markers | Diameter 2.5 times the line width with a cut-out of half the width, in the line color; the highest and the lowest are their first points, and points sharing a position get one marker |
| Bars | Width of the slot minus a gap, at most `MaxBarWidth`, from 0 (or the nearer edge) with square corners |
| Win-loss | Wins fill the upper half and losses the lower half with 1 DIP between them; zeros and missing values are not drawn |
| Missing values | `NaN` breaks the line; bars and win-loss skip it |
| Updates | Changes of a notifying list (add, remove, replace) and of item values animate; replacing `Values` or `ItemsSource` and resets do not, so reused rows never move |
| Entrance | `Auto` plays nothing; `Grow`, `Reveal`, `Fade` and `PlayEntranceAnimation()` play |
| Density | Always `Minimal`, with whole-chart taps only |

## 🎨 ChartStyle

`ChartStyle` holds the look of everything except the data, together with the defaults of series widths, opacities and sizes.  
One style can be shared by every chart.  
Treat an assigned style as immutable and create a new one with a `with` expression, or call `Invalidate()` after changing it in place.  
The style is validated when assigned: font sizes, `LegendMarkerSize`, `LineThickness`, `MaxBarWidth` and `MarkerSize` must be positive, the other widths, radii and gaps zero or more, the opacities from 0 to 1, and `MinimalSize` must not exceed `CompactSize`; otherwise `ArgumentException` is thrown.  

| Name | Type | Light | Dark | Description |
|---|---|---|---|---|
| `FontFamily` | `string` | `sans-serif` | `sans-serif` | Primary font of all text; missing characters are resolved through `FallbackFonts` of SkiaExtension |
| `PrimaryTextColor` | `Color` | `#0B0B0B` | `#F0EFEC` | Center value of a pie |
| `SecondaryTextColor` | `Color` | `#52514E` | `#C3C2B7` | Legend, axis titles, data labels outside the elements and the caption under the center value |
| `MutedTextColor` | `Color` | `#898781` | `#898781` | Tick labels, hidden legend items, `+N` of the legend and `EmptyText` |
| `TextHaloColor`, `TextHaloWidth` | `Color`, `double` | `Transparent`, `0` | `Transparent`, `0` | Outline around text |
| `AxisLabelFontSize`, `AxisTitleFontSize` | `double` | `11`, `11` | `11`, `11` | Axis text |
| `AxisLineColor` | `Color` | `#C3C2B7` | `#383835` | Baseline and axis lines |
| `GridLineColor` | `Color` | `#E1E0D9` | `#2C2C2A` | Grid lines |
| `GridLineThickness` | `double` | `0` | `0` | Width of the grid lines; `0` is one physical pixel |
| `LegendFontSize`, `LegendMarkerSize` | `double` | `12`, `8` | `12`, `8` | Legend |
| `DataLabelFontSize` | `double` | `11` | `11` | Data labels |
| `TooltipBackground` | `Color` | `#F0FCFCFB` | `#F01F1F1D` | Tooltip fill |
| `TooltipTextColor` | `Color` | `#0B0B0B` | `#F0EFEC` | Tooltip text |
| `TooltipBorderColor` | `Color` | `#1A0B0B0B` | `#1AF0EFEC` | Tooltip hairline border |
| `TooltipFontSize`, `TooltipCornerRadius` | `double` | `12`, `6` | `12`, `6` | Tooltip |
| `TrackballLineColor` | `Color` | `#898781` | `#898781` | Trackball line |
| `TrackColor` | `Color?` | `null` | `null` | Reserved for gauge-like charts |
| `DimmedOpacity` | `double` | `0.35` | `0.35` | Opacity of the elements that are not selected |
| `PressedOverlayColor` | `Color` | `#0F000000` | `#14FFFFFF` | Cover of a pressed chart |
| `SkeletonColor`, `SkeletonHighlightColor` | `Color` | `#ECEBE6`, `#F6F5F2` | `#232321`, `#2C2C2A` | Reserved for the loading placeholder |
| `Palette` | `IReadOnlyList<Color>` | `ChartPalettes.DefaultLight` | `ChartPalettes.DefaultDark` | Series colors in a fixed order |
| `OtherColor` | `Color` | `#B5B3AA` | `#5C5B56` | Merged slices and series beyond the palette |
| `GoodColor`, `WarningColor`, `SeriousColor`, `CriticalColor` | `Color` | `#0CA30C`, `#FAB219`, `#EC835A`, `#D03B3B` | same | Status colors; `CriticalColor` is the default color of negative sparkline values |
| `PositiveTextColor`, `NegativeTextColor` | `Color` | `#006300`, `#D03B3B` | `#0CA30C`, `#E66767` | Reserved for differences in KPI tiles |
| `LineThickness` | `double` | `2` | `2` | Default line width |
| `AreaFillOpacity` | `double` | `0.1` | `0.1` | Default fill opacity |
| `BarCornerRadius`, `MaxBarWidth` | `double` | `4`, `24` | `4`, `24` | Default bar corner and width |
| `MarkerSize`, `MarkerKnockout` | `double` | `8`, `2` | `8`, `2` | Default marker diameter and the transparent ring around it |
| `SegmentGap` | `double` | `2` | `2` | Gap between stack segments, grouped bars and slices |
| `CompactSize`, `MinimalSize` | `Size` | `320, 200`, `160, 96` | same | Density boundaries: a width or a height at or below a size switches to that density |

### ChartStyles

| Name | Description |
|---|---|
| `ChartStyles.Light` | Style for light screens and the default of `ChartViewBase.ChartStyle` |
| `ChartStyles.Dark` | Style for dark screens |

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

## 🔧 ClamChartSettings

Settings for every chart in the application, read when an animation starts.  

| Name | Type | Default | Description |
|---|---|---|---|
| `AnimationEnabled` | `bool` | `true` | `false` turns every chart animation off, for kiosks, slow devices and automated screenshots |
| `AnimationScale` | `double` | `1` | Multiplies every duration |
| `RespectSystemAnimationScale` | `bool` | `true` | Follows the animator duration scale of Android: `0` turns the animations off and other values multiply the durations |

## 🔤 Fonts

Per character font fallback comes from [SkiaExtension](https://github.com/usausa/skia-extension).  
Configure it with `FallbackFonts` in the `SkiaExtension` namespace at startup, before the first chart is shown.  
`Languages` defaults to the UI language of the device.  

```csharp
FallbackFonts.Languages = ["ja", "en"];
FallbackFonts.Typefaces = [SKTypeface.FromStream(await FileSystem.OpenAppPackageFileAsync("NotoSansJP-Regular.ttf"))];
```

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
| `ChartEntry` | `Label` (`string?`), `Value` (`double`), `Color` (`Color?`, optional) | Named value, such as a slice or a category |

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
| `Series` | `ChartSeries?` | Series of the element; `null` for a pie |
| `SeriesIndex` | `int` | Position of the series; `-1` for a pie |
| `Index` | `int` | Position of the item in the data; `-1` for the merged slice |
| `Item` | `object?` | Item of the element; `null` for the merged slice |
| `X` | `object?` | X of the point: `string?` for categories, `DateTime` for dates and `double` for numbers and positions; the name of a slice |
| `Y` | `double` | Original value, or the merged total |
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
| `Series` | `ChartSeries?` | Series of the legend item in `ClamChartView` |
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
| `Points` | `IReadOnlyList<ChartTrackingInfo>` | Point of each series at that X; empty when tracking ends |

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
| | `Fade` | Opacity rises |
| | `Grow` | Values grow from the baseline; a pie widens from `StartAngle` |
| | `Reveal` | Drawn from the start of the X axis; a pie is revealed from `StartAngle` |
| `ChartOrientation` | `Vertical` | X along the bottom |
| | `Horizontal` | X along the left, first at the top |
| `ChartStackMode` | `Normal` | Stacks the values |
| | `Percent` | Stacks the shares to 100% |
| `ChartLegendPosition` | `Top`, `Bottom`, `Left`, `Right` | Place of the legend |
| `ChartDataLabelsMode` | `None` | No data labels |
| | `Last` | The last point of each series |
| | `MinMax` | The largest and the smallest point of each series |
| | `All` | Every point; not recommended for dense data |
| `ChartSelectionMode` | `None` | No selection |
| | `Item` | One point, bar or slice |
| | `Category` | The column of one X (one slice in a pie) |
| `ChartTrackballMode` | `None` | No trackball |
| | `LongPress` | Starts after a long press |
| | `Touch` | Starts at once |
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
| `ChartPieLabelsMode` | `None` | No labels |
| | `Inside` | Labels inside the slices that fit |
| `ChartPieLabelContent` | `Percent`, `Value`, `Label`, `LabelAndPercent` | Text of a slice label |
| `ChartLegendValueMode` | `None`, `Value`, `Percent` | Value added to the legend entries of a pie |
| `ChartCenterValueMode` | `Auto` | The selected slice, otherwise the total or `CenterValue` |
| | `Total` | Always the total or `CenterValue` |
| | `Selected` | Only the selected slice, nothing while none is selected |
| | `None` | Nothing in the center |
| `ChartSparklineKind` | `Line`, `Area`, `Bar`, `WinLoss` | Shape of a sparkline |
| `ChartSparklineMarkers` | `None`, `First`, `Last`, `High`, `Low` | Flags of the points with a marker |
