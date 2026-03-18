using System.Diagnostics;

#if DEBUG

#endif

if (args.Contains("--on-demand"))
{
    // start on-demand worker flow
}

// keep process alive if this is meant to be a background worker
Thread.Sleep(Timeout.Infinite);
return 0;