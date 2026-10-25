using EduHorsLigne.Helpers;

namespace EduHorsLigne.Views.Controls;

/// <summary>
/// Champ de formulaire : titre (+ étiquette d'état à droite), icône, saisie, validation (✔) ou erreur (✖),
/// bouton afficher/masquer et message d'erreur sous le champ. L'état n'est jamais porté par la couleur seule.
/// </summary>
public partial class FormField : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(FormField), string.Empty,
        propertyChanged: (b, _, _) => ((FormField)b).ApplyTitle());
    public static readonly BindableProperty UpperTitleProperty = BindableProperty.Create(nameof(UpperTitle), typeof(bool), typeof(FormField), false,
        propertyChanged: (b, _, _) => ((FormField)b).ApplyTitle());
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(FormField), string.Empty,
        BindingMode.TwoWay,
        propertyChanged: (b, _, n) =>
        {
            var f = (FormField)b;
            if (f.Input.Text != (n as string)) f.Input.Text = n as string;
        });
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(FormField), string.Empty,
        propertyChanged: (b, _, n) => ((FormField)b).Input.Placeholder = n as string);
    public static readonly BindableProperty ErrorTextProperty = BindableProperty.Create(nameof(ErrorText), typeof(string), typeof(FormField), null,
        propertyChanged: (b, _, n) => ((FormField)b).ApplyError(n as string));
    public static readonly BindableProperty TagTextProperty = BindableProperty.Create(nameof(TagText), typeof(string), typeof(FormField), null,
        propertyChanged: (b, _, n) =>
        {
            var f = (FormField)b;
            f.TagLabel.Text = n as string;
            f.TagLabel.IsVisible = !string.IsNullOrEmpty(n as string);
        });
    public static readonly BindableProperty LeadingIconProperty = BindableProperty.Create(nameof(LeadingIcon), typeof(string), typeof(FormField), null,
        propertyChanged: (b, _, n) =>
        {
            var f = (FormField)b;
            f.LeadingLabel.Text = n as string;
            f.LeadingLabel.IsVisible = !string.IsNullOrEmpty(n as string);
        });
    public static readonly BindableProperty IsValidProperty = BindableProperty.Create(nameof(IsValid), typeof(bool), typeof(FormField), false,
        propertyChanged: (b, _, _) => ((FormField)b).ApplyMark());
    public static readonly BindableProperty IsPasswordProperty = BindableProperty.Create(nameof(IsPassword), typeof(bool), typeof(FormField), false,
        propertyChanged: (b, _, n) =>
        {
            var f = (FormField)b;
            f.Input.IsPassword = (bool)n;
            f.ToggleButton.IsVisible = (bool)n;
        });
    public static readonly BindableProperty InputKeyboardProperty = BindableProperty.Create(nameof(InputKeyboard), typeof(Keyboard), typeof(FormField), Keyboard.Default,
        propertyChanged: (b, _, n) => ((FormField)b).Input.Keyboard = (Keyboard)n);
    public static readonly BindableProperty MaxLengthProperty = BindableProperty.Create(nameof(MaxLength), typeof(int), typeof(FormField), int.MaxValue,
        propertyChanged: (b, _, n) => ((FormField)b).Input.MaxLength = (int)n);

    private bool _focused;
    private bool _hasError;

    public FormField()
    {
        InitializeComponent();
        Input.TextChanged += (_, e) => Text = e.NewTextValue ?? string.Empty;
    }

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public bool UpperTitle { get => (bool)GetValue(UpperTitleProperty); set => SetValue(UpperTitleProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public string? ErrorText { get => (string?)GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }
    public string? TagText { get => (string?)GetValue(TagTextProperty); set => SetValue(TagTextProperty, value); }
    public string? LeadingIcon { get => (string?)GetValue(LeadingIconProperty); set => SetValue(LeadingIconProperty, value); }
    public bool IsValid { get => (bool)GetValue(IsValidProperty); set => SetValue(IsValidProperty, value); }
    public bool IsPassword { get => (bool)GetValue(IsPasswordProperty); set => SetValue(IsPasswordProperty, value); }
    public Keyboard InputKeyboard { get => (Keyboard)GetValue(InputKeyboardProperty); set => SetValue(InputKeyboardProperty, value); }
    public int MaxLength { get => (int)GetValue(MaxLengthProperty); set => SetValue(MaxLengthProperty, value); }

    private void ApplyTitle()
    {
        TitleLabel.Text = UpperTitle ? Title?.ToUpperInvariant() : Title;
        SemanticProperties.SetDescription(Input, Title);
    }

    private void ApplyError(string? message)
    {
        _hasError = !string.IsNullOrEmpty(message);
        ErrorLabel.IsVisible = _hasError;
        ErrorLabel.Text = _hasError ? "⚠ " + message : string.Empty;
        ApplyMark();
        UpdateFrame();
    }

    private void ApplyMark()
    {
        if (_hasError)
        {
            StateMark.Text = "⊗";
            StateMark.TextColor = Res.Themed("ErrorColor", "ErrorOnDarkColor");
            StateMark.IsVisible = true;
        }
        else if (IsValid)
        {
            StateMark.Text = "✔";
            StateMark.TextColor = Res.GetColor("SuccessColor");
            StateMark.IsVisible = true;
        }
        else StateMark.IsVisible = false;
    }

    private void OnFocused(object? sender, FocusEventArgs e) { _focused = true; UpdateFrame(); }
    private void OnUnfocused(object? sender, FocusEventArgs e) { _focused = false; UpdateFrame(); }

    private void OnToggle(object? sender, EventArgs e)
    {
        Input.IsPassword = !Input.IsPassword;
        ToggleButton.Text = Input.IsPassword ? "👁" : "🙈";
    }

    private void UpdateFrame()
    {
        if (_hasError)
        {
            FieldFrame.Stroke = Res.GetBrush("ErrorColor");
            FieldFrame.StrokeThickness = 2;
            if (Application.Current?.RequestedTheme != AppTheme.Dark) FieldFrame.BackgroundColor = Res.GetColor("ErrorSoftLight");
        }
        else
        {
            FieldFrame.ClearValue(VisualElement.BackgroundColorProperty);
            if (_focused)
            {
                FieldFrame.Stroke = Res.GetBrush("PrimaryColor");
                FieldFrame.StrokeThickness = 2;
            }
            else
            {
                FieldFrame.ClearValue(Border.StrokeProperty);
                FieldFrame.ClearValue(Border.StrokeThicknessProperty);
            }
        }
    }
}
