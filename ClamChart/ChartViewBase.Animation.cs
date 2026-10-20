namespace ClamChart;

using ClamChart.Animation;

public abstract partial class ChartViewBase
{
    public static readonly BindableProperty AnimationEnabledProperty = BindableProperty.Create(nameof(AnimationEnabled), typeof(bool), typeof(ChartViewBase), true, propertyChanged: OnAnimationSwitchChanged);
    public static readonly BindableProperty AnimationDurationProperty = BindableProperty.Create(nameof(AnimationDuration), typeof(int), typeof(ChartViewBase), 450, validateValue: static (_, value) => ValidateDuration(value, nameof(AnimationDuration)), propertyChanged: OnAnimationSwitchChanged);
    public static readonly BindableProperty AnimationEasingProperty = BindableProperty.Create(nameof(AnimationEasing), typeof(Easing), typeof(ChartViewBase), Easing.CubicOut, validateValue: static (_, value) => ValidateEasing(value, nameof(AnimationEasing)));
    public static readonly BindableProperty EntranceAnimationProperty = BindableProperty.Create(nameof(EntranceAnimation), typeof(ChartEntranceAnimation), typeof(ChartViewBase), ChartEntranceAnimation.Auto);
    public static readonly BindableProperty UpdateAnimationEnabledProperty = BindableProperty.Create(nameof(UpdateAnimationEnabled), typeof(bool), typeof(ChartViewBase), true);

    public bool AnimationEnabled
    {
        get => (bool)GetValue(AnimationEnabledProperty);
        set => SetValue(AnimationEnabledProperty, value);
    }

    public int AnimationDuration
    {
        get => (int)GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }

    public Easing AnimationEasing
    {
        get => (Easing)GetValue(AnimationEasingProperty);
        set => SetValue(AnimationEasingProperty, value);
    }

    public ChartEntranceAnimation EntranceAnimation
    {
        get => (ChartEntranceAnimation)GetValue(EntranceAnimationProperty);
        set => SetValue(EntranceAnimationProperty, value);
    }

    public bool UpdateAnimationEnabled
    {
        get => (bool)GetValue(UpdateAnimationEnabledProperty);
        set => SetValue(UpdateAnimationEnabledProperty, value);
    }

    internal IChartAnimationDriver AnimationDriver { get; set; }

    internal virtual bool IsAnimating => false;

    public void PlayEntranceAnimation()
    {
        ThrowIfUnavailable();
        StartEntranceAnimation();
    }

    public void CompleteAnimations()
    {
        ThrowIfUnavailable();
        FinishAnimations();
    }

    internal virtual void StartEntranceAnimation()
    {
    }

    internal virtual void FinishAnimations()
    {
    }

    internal double ResolveAnimationLength(double milliseconds) =>
        AnimationEnabled && (AnimationDuration > 0) ? milliseconds * ClamChartSettings.ResolveAnimationScale() : 0;

    private static bool ValidateDuration(object? value, string name) =>
        (value is int duration) && (duration >= 0) ? true : throw new ArgumentOutOfRangeException(name, value, "The value must be zero or more.");

    private static bool ValidateEasing(object? value, string name) =>
        value is not null ? true : throw new ArgumentNullException(name);

    private static void OnAnimationSwitchChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (ChartViewBase)bindable;
        if (!view.AnimationEnabled || (view.AnimationDuration <= 0))
        {
            view.FinishAnimations();
        }
    }
}
