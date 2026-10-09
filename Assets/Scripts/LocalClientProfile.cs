using System;
using System.Threading;

namespace SecretsReborn
{
    // PlayerPrefs is shared by all game processes on this OS account. A named
    // lease assigns concurrent clients separate persistent credential keys.
    public static class LocalClientProfile
    {
        private static Mutex lease;
        private static int index;
        public static string KeySuffix
        {
            get
            {
                if (lease == null)
                    for (int candidate = 1; candidate <= 4; candidate++)
                    {
                        var attempt = new Mutex(false,"SecretsReborn.LocalClientProfile."+candidate);
                        bool acquired;
                        try { acquired = attempt.WaitOne(0); }
                        catch (AbandonedMutexException) { acquired = true; }
                        if (acquired) { lease = attempt; index = candidate; break; }
                        attempt.Dispose();
                    }
                if (lease == null) throw new InvalidOperationException("Alle vier lokalen Client-Profile sind bereits in Verwendung.");
                return index == 1 ? "" : ".LocalPlayer"+index;
            }
        }
        public static void Release()
        {
            if (lease == null) return;
            try { lease.ReleaseMutex(); }
            finally { lease.Dispose(); lease = null; index = 0; }
        }
    }
}
