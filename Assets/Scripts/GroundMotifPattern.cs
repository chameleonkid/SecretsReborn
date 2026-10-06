namespace SecretsReborn
{
    public static class GroundMotifPattern
    {
        public static int TileIndex(int x, int y, int seed)
        {
            int bx = x >> 1, by = y >> 1;
            uint hash = unchecked((uint)(bx * 73856093 ^ by * 19349663 ^ seed));
            hash ^= hash >> 13;
            int motif = (int)(hash % 3);
            int quadrant = (x - bx * 2) + (y - by * 2) * 2;
            return motif * 4 + quadrant;
        }
    }
}
