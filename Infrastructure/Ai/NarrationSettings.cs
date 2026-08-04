namespace Kidamooz.Infrastructure.Ai;

public class NarrationSettings
{
    public const string SectionName = "Narration";

    public string Voice { get; set; } = "fa-IR-DilaraNeural";
    public string OutputFolder { get; set; } = "wwwroot/audio";
    public string UserOutputFolder { get; set; } = "wwwroot/audio/user";
    public long MaxUploadSize { get; set; } = 52_428_800;
    public string Executable { get; set; } = "edge-tts";
}
