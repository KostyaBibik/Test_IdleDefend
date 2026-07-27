namespace Services
{
    public readonly struct ShopPurchaseResult
    {
        public ShopPurchaseResult(bool success, string reason)
        {
            Success = success;
            Reason = reason;
        }

        public bool Success { get; }
        public string Reason { get; }

        public static ShopPurchaseResult Ok()
        {
            return new ShopPurchaseResult(true, string.Empty);
        }

        public static ShopPurchaseResult Failed(string reason)
        {
            return new ShopPurchaseResult(false, reason);
        }
    }
}
