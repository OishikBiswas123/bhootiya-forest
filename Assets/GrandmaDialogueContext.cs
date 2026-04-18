public enum GrandmaDialogueSource
{
    None,
    FromOutside,
    FromUpstairs
}

public static class GrandmaDialogueContext
{
    public static GrandmaDialogueSource LastSource = GrandmaDialogueSource.None;
    public static bool PendingUpstairsCheckBeforeLeaving = false;

    public static void SetFromOutside()
    {
        LastSource = GrandmaDialogueSource.FromOutside;
        PendingUpstairsCheckBeforeLeaving = false;
    }

    public static void SetFromUpstairs()
    {
        LastSource = GrandmaDialogueSource.FromUpstairs;
        PendingUpstairsCheckBeforeLeaving = true;
    }

    public static GrandmaDialogueSource Consume()
    {
        GrandmaDialogueSource src = LastSource;
        LastSource = GrandmaDialogueSource.None;
        return src;
    }

    public static void MarkUpstairsDialogueHandled()
    {
        PendingUpstairsCheckBeforeLeaving = false;
    }
}
