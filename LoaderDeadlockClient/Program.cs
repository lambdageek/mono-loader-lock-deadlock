//
// Triggering a mono global loader lock deadlock
//
// We need the following:
// - 2 threads
// - first thread tries to create a type; the initialization of that type depends on another assembly
//   - that assembly needs to be loaded using the AssemblyResolve event
//   - the AssemblyResolve event doesn't actually load anything - it forces another thread to wake up and do work
// - the second thread wakes up and tries to instantiate some class that hasn't been loaded before (from any assembly)
// - at this point the app will deadlock because the first thread is still trying to create its type.
//
// So I think we need three projects:
//
// Project 0:
//   public class BaseClass {}
// Project 1:
//   public class DerivedClass : BaseClass {}
//   Project 0 should not be a private asset of Project 1, so it doesn't get copied over
// Main Project:
//   - also defines BaseClass
//   - has a AssemblyResolve event handler that returns itself if someone asks for Project 0
//   - depends on Project 1 but does not include Project 0
//   - tries to instantiate DerivedClass

using System;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Reflection;

public static class Program {
    private static object s_SignalWorker = new();

    public static void Main () {
        Console.WriteLine ("Starting");
        lock (s_SignalWorker) {
            var t = new Thread (() => WorkerThreadMain());
            t.IsBackground = true;
            t.Start();
            Monitor.Wait (s_SignalWorker); // wait for worker to start
        }
        AppDomain.CurrentDomain.AssemblyResolve += (sender, evt) => ResolveHandler(evt.Name);
        Run();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run() {
        var x = new LoaderDeadlockPlugin();
        Console.WriteLine (x.Hello());

    }

    private static Assembly? ResolveHandler(string name)
    {
        if (name.StartsWith ("LoaderDeadlockBase, ")) {
            LoadLinqOtherThread();
            return typeof(Program).Assembly;
        }
        return null;
    }

    private static void LoadLinqOtherThread()
    {
        lock (s_SignalWorker) {
            Monitor.Pulse (s_SignalWorker); // wake up the other thread
            Monitor.Wait (s_SignalWorker); // wait for the other thread to load linq
        }
    }

    public static void WorkerThreadMain()
    {
        lock (s_SignalWorker) {
            Monitor.Pulse(s_SignalWorker); // wake up main thread
            Monitor.Wait(s_SignalWorker); // wait for the main thread to wake us
            LoadLinq(); // trigger assebly loading
            Monitor.Pulse(s_SignalWorker); // wake up main thread
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LoadLinq() {
        Console.WriteLine ("foo");
        Console.WriteLine (typeof (System.Linq.Queryable).Assembly.FullName);
    }
}
