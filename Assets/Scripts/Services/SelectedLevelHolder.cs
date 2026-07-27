namespace Services
{
    /// <summary>
    /// Индекс уровня, выбранного в меню, для загрузки в GameScene.
    /// Статикой по тому же принципу, что и SurvivalRecord: DiContainerRef.UnbindAll()
    /// рвёт DI между сценами Menu -> GameScene, а выбор уровня должен пережить переход.
    /// </summary>
    public static class SelectedLevelHolder
    {
        public static int SelectedLevelIndex { get; set; }
    }
}
