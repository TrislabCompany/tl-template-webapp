namespace Api.Features.Items;

public sealed class Item
{
    public const int NameMaxLength = 200;

    public int Id { get; set; }

    public required string Name { get; set; }

    public bool Done { get; set; }
}
