namespace CivGame.Core;

public enum MoveValidationFailureReason
{
    None,
    TargetOutOfBounds,
    TargetNotAdjacent,
    NoMovementRemaining,
    TargetTileMissing,
    TargetTileImpassable
}

public readonly struct MoveValidationResult
{
    public bool IsValid { get; }
    public MoveValidationFailureReason FailureReason { get; }

    private MoveValidationResult(bool isValid, MoveValidationFailureReason failureReason)
    {
        IsValid = isValid;
        FailureReason = failureReason;
    }

    public static MoveValidationResult Valid() => new(true, MoveValidationFailureReason.None);

    public static MoveValidationResult Invalid(MoveValidationFailureReason failureReason) => new(false, failureReason);

    public string GetMessage()
    {
        return FailureReason switch
        {
            MoveValidationFailureReason.None => "Move is valid.",
            MoveValidationFailureReason.TargetOutOfBounds => "Target is outside the map bounds.",
            MoveValidationFailureReason.TargetNotAdjacent => "Target must be an adjacent tile.",
            MoveValidationFailureReason.NoMovementRemaining => "Unit has no movement points remaining.",
            MoveValidationFailureReason.TargetTileMissing => "Target tile does not exist.",
            MoveValidationFailureReason.TargetTileImpassable => "Target tile is impassable for this unit.",
            _ => "Move is invalid."
        };
    }
}
