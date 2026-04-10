namespace EchoSpace.Save;

public sealed class EnemySaveData
{
    public string SaveId { get; set; } = string.Empty;
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public int Health { get; set; }
    public float Posture { get; set; }
    public bool IsBroken { get; set; }
    public bool IsDead { get; set; }
}
