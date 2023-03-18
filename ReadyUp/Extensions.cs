namespace ReadyUp
{
    public static class Extensions
    {

        // Code from Mirror (Unity #1 Networking Solution) by vis2k and friends :D
        // https://github.com/MirrorNetworking/Mirror
        public static int GetStableHashCode(this string text)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in text)
                    hash = hash * 31 + c;

                return hash;
            }
        }
    }
}
