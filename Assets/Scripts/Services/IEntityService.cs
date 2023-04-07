using Signals;
using Views;

namespace Services
{
    public interface IEntityService
    {
        void AddEntityOnService(IEntityView entityView);
        void RemoveEntityFromService(DestroyEntitySignal signal);
    }
}