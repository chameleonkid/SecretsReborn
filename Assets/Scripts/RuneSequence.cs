namespace SecretsReborn
{
    public sealed class RuneSequence
    {
        public int Progress { get; private set; }
        public bool IsComplete => Progress == 4;

        public bool Enter(int index)
        {
            if (IsComplete) return false;
            if (index != Progress)
            {
                Progress = 0;
                return false;
            }
            Progress++;
            return true;
        }
    }
}
