# Mono Loader Lock Deadlock

This project demonstrates a deadlock due to Mono's global loader lock.

Two threads are involved:

1. The main thread attempts to load a class `LoaderDeadlockPlugin`
   that has a parent `LoaderDeadlockBase` that is supposed to be in
   the `LoaderDeadlockBase` assembly.  The application uses an
   `AssemblyResolve` event handler to locate the assembly containing
   the class definition.  At the same time, the event handler also
   kicks off some work on a background thread and waits for that work
   to finish before returning.
2. The background thread tries to initialize a class from the Linq libraries (which had not been loaded yet).
3. Because the `AssemblyResolve` event handler ran on the main thread
   from a context where the main thread is holding the global loader
   lock, the background thread cannot proceed with class
   initialization (which also needs to take the global loader lock).
   

This app uses net8.0, but the underlying problem exists in all
versions of Mono including the legacy (.NET Framework compatible)
versions of Mono.
