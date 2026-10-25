using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EduHorsLigne.ViewModels;

/// <summary>États possibles d'une page de données.</summary>
public enum PageState { Content, Loading, Empty, Error }

/// <summary>
/// État de démonstration global (Paramètres &gt; Mode démo) permettant de voir chaque page
/// dans les états Contenu / Chargement / Vide / Erreur.
/// </summary>
public static class DemoState
{
    public static PageState Current { get; private set; } = PageState.Content;

    public static void Set(PageState state)
    {
        Current = state;
        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PageState>(state));
    }
}

/// <summary>Base de tous les ViewModels : expose l'état de la page.</summary>
public abstract partial class BaseViewModel : ObservableObject
{
    protected BaseViewModel()
    {
        _state = DemoState.Current;
        WeakReferenceMessenger.Default.Register<BaseViewModel, ValueChangedMessage<PageState>>(
            this, static (recipient, message) => recipient.State = message.Value);
    }

    [ObservableProperty]
    private PageState _state;
}
