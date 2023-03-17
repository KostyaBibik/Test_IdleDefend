namespace Components
{
    public interface IHealable
    {
        void AddHealth(int amount);
        void ReduceHealth(int amount);
        int GetHealth();
        
    }
}