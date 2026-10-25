using System.Windows.Input;

namespace EduHorsLigne.Views.Controls;

/// <summary>État vide / erreur : icône, titre, message court et bouton d'action facultatif.</summary>
public partial class EmptyState : ContentView
{
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(string), typeof(EmptyState), string.Empty,
        propertyChanged: (b, _, n) => ((EmptyState)b).IconLabel.Text = n as string);
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(EmptyState), string.Empty,
        propertyChanged: (b, _, n) => ((EmptyState)b).TitleLabel.Text = n as string);
    public static readonly BindableProperty MessageProperty = BindableProperty.Create(nameof(Message), typeof(string), typeof(EmptyState), string.Empty,
        propertyChanged: (b, _, n) =>
        {
            var s = (EmptyState)b;
            s.MessageLabel.Text = n as string;
            s.MessageLabel.IsVisible = !string.IsNullOrEmpty(n as string);
        });
    public static readonly BindableProperty ButtonTextProperty = BindableProperty.Create(nameof(ButtonText), typeof(string), typeof(EmptyState), string.Empty,
        propertyChanged: (b, _, n) =>
        {
            var s = (EmptyState)b;
            s.ActionButton.Text = n as string;
            s.ActionButton.IsVisible = !string.IsNullOrEmpty(n as string);
        });
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(EmptyState), null,
        propertyChanged: (b, _, n) => ((EmptyState)b).ActionButton.Command = n as ICommand);

    public EmptyState() => InitializeComponent();

    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public string ButtonText { get => (string)GetValue(ButtonTextProperty); set => SetValue(ButtonTextProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
}
