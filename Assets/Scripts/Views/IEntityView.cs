namespace Views
{
    public interface IEntityView
    {
        public bool isDestroyed { get; set; }

        /// <summary>
        /// Счётчик "жизней" объекта, растёт на каждый Rent из пула (см. EntityPoolService).
        /// Любая ссылка на этот view, которая живёт дольше одного кадра, должна сохранить
        /// значение poolVersion на момент захвата и сверяться с ним перед использованием -
        /// иначе после переиспользования GameObject-а под другую сущность старая ссылка
        /// будет считать чужого нового врага/цель своей.
        /// </summary>
        public int poolVersion { get; set; }
    }
}