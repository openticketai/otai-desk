using OtaiDesk.Domain;

namespace OtaiDesk.Shared;

public sealed record TicketProvider(string Name)
{
    public void RequireDesk()
    {
        if (Name != "desk")
        {
            throw new ConnectorUnwiredException(Name);
        }
    }
}
