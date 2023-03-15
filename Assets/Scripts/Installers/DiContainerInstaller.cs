using Zenject;

namespace Installers
{
    public class DiContainerInstaller : MonoInstaller
    {
        [Inject] private DiContainer _diContainer;

        public override void InstallBindings()
        {
            DiContainerRef.Container = _diContainer;
        }
    }
}