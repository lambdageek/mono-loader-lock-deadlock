using System;

public sealed class LoaderDeadlockPlugin : LoaderDeadlockBase {
        public LoaderDeadlockPlugin () : base() {  }
        public override string Hello () => "hello from plugin";
}
