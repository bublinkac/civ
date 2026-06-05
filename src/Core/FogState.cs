namespace CivGame.Core;

public enum FogState
{
    Unexplored, // Completely black, never visited
    Shrouded,   // Explored previously, but no unit currently provides active vision
    Visible     // Under active vision from a unit or city
}
