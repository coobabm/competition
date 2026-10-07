namespace LingGuangV05.XingGuang
{
    public sealed partial class XgSim
    {
        public const string EraDeliveryTask = "life.era.delivery";
        // The existing life-hook owns only the on/off switch. StoryState owns the commission and player approval.
        void AddEraDeliveryTask(XgWiringView view)
        {
            if (!lifeHooks.TryGetValue(EraDeliveryTask, out var hook)) return;
            var node = Life(EraDeliveryTask, T("亲友委托 · 商品文案交付", "Personal commission · product copy"), hook.available(), hook.on(), T("收到亲友委托，并具备翻译检查点后开放", "Receive the commission and train a translation checkpoint"));
            node.shown = node.available; node.unit = T("份", "draft"); node.realItems = true;
            Add(view, node);
        }
        public void NotifyEraDeliveryItem() { WireItem?.Invoke(EraDeliveryTask, true); }
    }
}
