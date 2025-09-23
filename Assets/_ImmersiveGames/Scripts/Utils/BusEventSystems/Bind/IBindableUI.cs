namespace _ImmersiveGames.Scripts.Utils.BusEventSystems
{
    public interface IBindableUI<in TData>
    {
        void Bind(string actorId, TData data);
        void Unbind();
        void UpdateValue(TData newValue);
    }
}