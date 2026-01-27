public enum TileType
{
    Walkable,
    Void,
    Wall,       // Blocks line of sight, surrounds walkable areas
    HeroSpawn,
    BlipSpawn,
    ArtifactSpawn, // Walkable tile where artifact object spawns
    Target
}
