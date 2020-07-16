using Dym.Logging;
using System;

namespace Dym.Libs.WebSocketLib.Server
{
    public class WebSocketServiceHost<TBehavior> : WebSocketServiceHost where TBehavior : WebSocketBehavior
    {
        private readonly Func<TBehavior> _creator;

        public WebSocketServiceHost(string path, Func<TBehavior> creator, Logger log) : this(path, creator, null, log) { }

        public WebSocketServiceHost(string path, Func<TBehavior> creator, Action<TBehavior> initializer, Logger log) : base(path, log)
        {
            _creator = createCreator(creator, initializer);
        }

        public override Type BehaviorType => typeof(TBehavior);

        private Func<TBehavior> createCreator(Func<TBehavior> creator, Action<TBehavior> initializer)
        {
            if (initializer == null)
                return creator;

            return () =>
            {
                var ret = creator();
                initializer(ret);

                return ret;
            };
        }

        protected override WebSocketBehavior CreateSession()
        {
            return _creator();
        }
    }
}
