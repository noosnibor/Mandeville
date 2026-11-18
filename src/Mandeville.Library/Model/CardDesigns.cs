namespace Mandeville.Library.Model;

/// <summary>One picture the admin can choose for a celebration card (file lives in Mandeville/wwwroot/cards).</summary>
public record CardDesign(string File, string Label);

/// <summary>
/// The list of card pictures. To add a design, drop the picture into wwwroot/cards and add one line here.
/// The first design in each list is the usual choice; automatic emails spread members across the list.
/// </summary>
public static class CardDesigns
{
    #region Design lists

    public static readonly IReadOnlyList<CardDesign> Anniversary =
    [
        new("anniversary-1.jpg", "Champagne & roses"),
        new("anniversary-2.jpg", "Red roses")
    ];

    public static readonly IReadOnlyList<CardDesign> BirthdayMale =
    [
        new("male-1.jpg", "Chocolate slice"),
        new("male-2.jpg", "Chocolate cake & verse"),
        new("male-3.jpg", "Blue roses & Bible"),
        new("male-4.jpg", "Blue balloons")
    ];

    public static readonly IReadOnlyList<CardDesign> BirthdayFemale =
    [
        new("female-1.jpg", "Gold & blush flowers"),
        new("female-2.jpg", "Rose gold floral"),
        new("female-3.jpg", "Pink gift & bow"),
        new("female-4.jpg", "Rose & silver balloons")
    ];

    #endregion

    #region Lookups

    /// <summary>The designs offered for a card: couples get the anniversary list, men and women their own birthday list, and a birthday with no gender recorded gets the women's list first, then the men's.</summary>
    public static IReadOnlyList<CardDesign> For(CelebrationModel card)
    {
        if (!card.IsBirthday) return Anniversary;
        return card.Gender switch
        {
            "Male" => BirthdayMale,
            "Female" => BirthdayFemale,
            _ => [.. BirthdayFemale, .. BirthdayMale]
        };
    }

    /// <summary>The design used when nobody picks one (automatic emails, first preview). It varies by member so everyone is not sent the same picture, but a member always gets the same one.</summary>
    public static CardDesign Default(CelebrationModel card)
    {
        var list = For(card);
        var spread = card.MemberKey.ToByteArray().Sum(b => (int)b);
        return list[spread % list.Count];
    }

    /// <summary>The picture for a file name, or the default when the name is unknown or not offered for this card.</summary>
    public static CardDesign Resolve(CelebrationModel card, string? file) =>
        For(card).FirstOrDefault(d => d.File == file) ?? Default(card);

    #endregion
}
