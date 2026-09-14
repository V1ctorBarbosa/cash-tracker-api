namespace CashTrack.Domain.Users;

public sealed class User
{
    public UserId Id { get; }

    public string Name { get; }

    public string Email { get; }

    private User(UserId id, string name, string email)
    {
        Id = id;
        Name = name;
        Email = email;
    }

    public static User Create(UserId id, string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }

        return new User(id, name.Trim(), email.Trim());
    }
}
