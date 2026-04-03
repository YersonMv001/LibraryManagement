namespace LibraryManagement.Application.Settings;

public class OllamaSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434/api/chat";
    public string Model { get; set; } = "llama3";
}
