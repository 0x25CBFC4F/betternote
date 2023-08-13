namespace Void.BetterNote.DTO;

public class CreateResponse
{
    public string Id { get; set; }
    public string Key { get; set; }
    public string IV { get; set; }
    public int NoteExpirationInMinutes { get; set; }
}
